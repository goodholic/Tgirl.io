using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using System.Collections;
using Thinksquirrel.CShake;
using Unity.VisualScripting;

/// <summary>
/// 플레이어의 이동, 공격, 회전, 애니메이션 및 IK를 처리하는 컨트롤러
/// </summary>
public class PlayerController : DamageableController
{
    #region Inspector Fields
    [SerializeField] private bool _isInit;
    [SerializeField] private Joystick _joystick;          // 이동 조이스틱
    [SerializeField] private float _moveSpeed = 5f;         // 이동 속도 (NavMeshAgent 속도)

    [SerializeField] private GameObject _muzzleFlashPrefab; // 머즐 플래시 효과 프리팹
    [SerializeField] private GameObject _projectilePrefab;  // 발사체 프리팹
    [SerializeField] private Transform _gunSpawnPoint;      // 발사체 생성 위치
    [SerializeField] private float _attackInterval;         // 공격 실행 간격 (초)
    [SerializeField] private AudioSource _attackAudioSource;  // 공격 사운드 재생용 AudioSource
    [SerializeField] private AudioClip _attackSoundClip;      // 공격 사운드 클립

    [SerializeField] private Animator _playerAnimator;      // 플레이어 애니메이터
    [SerializeField] private Transform _lookAtTarget;         // IK LookAt 대상
    [SerializeField] private Vector3 _lookAtOffset;           // LookAt 대상에 적용할 오프셋
    [SerializeField] private float _minLookAtY = 0.5f;          // LookAt 대상 최소 Y값
    [SerializeField] private float _maxLookAtY = 2.0f;          // LookAt 대상 최대 Y값
    [SerializeField] private Transform _leftHandTarget;       // 왼손 IK 타겟
    [SerializeField] private float _handIKWeight = 1.0f;        // 왼손 IK 가중치

    [SerializeField] private float _dragSensitivityX = 0.1f;  // 드래그 회전 X 민감도
    [SerializeField] private float _dragSensitivityY = 0.1f;  // 드래그 회전 Y 민감도
    
    [Header("Dash Settings")]
    [SerializeField] private float dashDistance = 3f;  // 뒤로 이동할 거리
    [SerializeField] private float dashDuration = 0.2f;// 이동에 걸리는 시간
    private bool isDashing = false;
    #endregion

    #region Private Fields
    private NavMeshAgent _agent;             // NavMeshAgent 컴포넌트
    private int _rotationTouchId = -1;       // 터치 회전 제어용 터치 ID (-1: 미사용)
    private Vector3 _lastMousePosition = Vector3.zero; // 에디터에서 마우스 드래그용 이전 마우스 위치
    #endregion
    
    public Transform LookAtTarget { get { return _lookAtTarget; } set { _lookAtTarget = value; } }

    #region Unity Methods
    protected override void Awake()
    {
        base.Awake();
        _agent = GetComponent<NavMeshAgent>();
        
        // (1) 업그레이드 데이터 먼저 로드 (게임 진입 시 1회만 로드해도 되지만, 혹시 몰라 안전차)
        UpgradeManager.LoadData();

        // (2) 기본값 + 업그레이드 보너스
        float moveSpeedBonus = UpgradeManager.GetMoveSpeedBonus();
        // 예: _moveSpeed = 기본값(Inspector) + 보너스
        _moveSpeed += moveSpeedBonus;

        // 공격 속도 = 공격 간격 감소
        //float intervalReduction = UpgradeManager.geta();
        //_attackInterval = Mathf.Max(0.1f, _attackInterval - intervalReduction); 
        // (최소 0.1초라고 제한 예시)

        // 최대 체력 보너스
        float healthBonus = UpgradeManager.GetMaxHealthBonus();
        _maxHealth += healthBonus;

        if (!_isInit)
        {
            _health = _maxHealth; // 부활 시 풀피로 시작
            _isInit = true;
        }

        // NavMeshAgent에 적용
        if (_agent != null)
        {
            _agent.speed = _moveSpeed;
            _agent.updateRotation = false;
        }
        
        if (_agent != null)
        {
            _agent.speed = _moveSpeed;
            _agent.updateRotation = false; // 직접 회전 제어
        }
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
    }

    private void OnEnable()
    {
        if (_attackInterval > 0)
        {
            InvokeRepeating(nameof(Attack), 0.3f, _attackInterval);
        }
    }

    private void OnDisable()
    {
        if (_attackInterval > 0)
        {
            CancelInvoke(nameof(Attack));
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        HandleRotationByMouse();
#else
        HandleRotationByDrag();
#endif
        if (!isDead)
        {
            HandleMovement();
        }
        UpdateAnimation();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (_playerAnimator != null)
        {
            // LookAt 처리
            if (_lookAtTarget != null)
            {
                _playerAnimator.SetLookAtWeight(1f, 0f, 1f, 0f, 0f);
                Vector3 targetPos = _lookAtTarget.position + _lookAtOffset;
                _playerAnimator.SetLookAtPosition(targetPos);
            }
            // 왼손 IK 처리
            if (_leftHandTarget != null)
            {
                _playerAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, _handIKWeight);
                _playerAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, _handIKWeight);
                _playerAnimator.SetIKPosition(AvatarIKGoal.LeftHand, _leftHandTarget.position);
                _playerAnimator.SetIKRotation(AvatarIKGoal.LeftHand, _leftHandTarget.rotation);
            }
        }
    }
    #endregion

    #region Movement & Rotation
    /// <summary>
    /// 조이스틱 입력을 기반으로 NavMeshAgent를 이용해 플레이어를 이동시킵니다.
    /// </summary>
    private void HandleMovement()
    {
        Vector2 input = _joystick.InputDirection;
        if (input.sqrMagnitude > 0.01f)
        {
            // 카메라 기준 전후 좌우 방향 계산 (수평 성분만 사용)
            Vector3 camForward = Camera.main.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = Camera.main.transform.right;
            camRight.y = 0f;
            camRight.Normalize();

            Vector3 moveDir = camRight * input.x + camForward * input.y;
            Vector3 destination = transform.position + moveDir;
            _agent.SetDestination(destination);
        }
        else
        {
            _agent.SetDestination(transform.position);
        }
    }

    /// <summary>
    /// 모바일 환경에서 오른쪽 화면 터치 드래그로 플레이어 회전을 처리합니다.
    /// </summary>
    private void HandleRotationByDrag()
    {
        if (_rotationTouchId == -1)
        {
            // 오른쪽 화면에서 터치 시작한 터치를 탐색
            foreach (Touch touch in Input.touches)
            {
                if (touch.phase == TouchPhase.Began && touch.position.x > Screen.width / 2)
                {
                    if (!EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    {
                        _rotationTouchId = touch.fingerId;
                        break;
                    }
                }
            }
        }
        else
        {
            bool foundTouch = false;
            foreach (Touch touch in Input.touches)
            {
                if (touch.fingerId == _rotationTouchId)
                {
                    foundTouch = true;
                    if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                    {
                        float deltaX = touch.deltaPosition.x * _dragSensitivityX;
                        transform.Rotate(0f, deltaX, 0f);

                        float deltaY = touch.deltaPosition.y * _dragSensitivityY;
                        if (_lookAtTarget != null)
                        {
                            Vector3 pos = _lookAtTarget.position;
                            pos.y = Mathf.Clamp(pos.y + deltaY, _minLookAtY, _maxLookAtY);
                            _lookAtTarget.position = pos;
                        }
                    }
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        _rotationTouchId = -1;
                    }
                    break;
                }
            }
            if (!foundTouch)
            {
                _rotationTouchId = -1;
            }
        }
    }

    /// <summary>
    /// 에디터 환경에서 마우스 드래그로 플레이어 회전을 처리합니다.
    /// </summary>
    private void HandleRotationByMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Input.mousePosition.x > Screen.width / 2 && !EventSystem.current.IsPointerOverGameObject())
            {
                _lastMousePosition = Input.mousePosition;
            }
            else
            {
                _lastMousePosition = Vector3.zero;
            }
        }
        if (Input.GetMouseButton(0))
        {
            if (_lastMousePosition != Vector3.zero)
            {
                Vector3 delta = Input.mousePosition - _lastMousePosition;
                float deltaX = delta.x * _dragSensitivityX;
                transform.Rotate(0f, deltaX, 0f);

                float deltaY = delta.y * _dragSensitivityY;
                if (_lookAtTarget != null)
                {
                    Vector3 pos = _lookAtTarget.position;
                    pos.y = Mathf.Clamp(pos.y + deltaY, _minLookAtY, _maxLookAtY);
                    _lookAtTarget.position = pos;
                }
                _lastMousePosition = Input.mousePosition;
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            _lastMousePosition = Vector3.zero;
        }
    }
    #endregion

    #region Animation & Attack
    /// <summary>
    /// 조이스틱 사용 여부에 따라 애니메이터 파라미터를 업데이트합니다.
    /// </summary>
    private void UpdateAnimation()
    {
        if (_joystick.IsActive)
        {
            Vector2 input = _joystick.InputDirection;
            _playerAnimator.SetFloat("X", input.x);
            _playerAnimator.SetFloat("Y", input.y);
        }
        else
        {
            _playerAnimator.SetFloat("X", 0f);
            _playerAnimator.SetFloat("Y", 0f);
        }
    }

    /// <summary>
    /// 공격 실행: 머즐 플래시 효과 재생, 발사체 생성 및 사운드/카메라 쉐이크 적용
    /// </summary>
    public void Attack()
    {
        if (isDead)
            return;

        if (_muzzleFlashPrefab != null && _gunSpawnPoint != null)
        {
            Instantiate(_muzzleFlashPrefab, _gunSpawnPoint.position, _gunSpawnPoint.rotation);
        }
        if (_projectilePrefab != null && _gunSpawnPoint != null)
        {
            _attackAudioSource.clip = _attackSoundClip;
            _attackAudioSource.Play();

            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;
            Vector3 targetPoint = Physics.Raycast(ray, out hit) ? hit.point : ray.GetPoint(1000f);

            GameObject bullet = Instantiate(_projectilePrefab, _gunSpawnPoint.position, Quaternion.identity);
            bullet.transform.LookAt(targetPoint);

            Projectile proj = bullet.GetComponent<Projectile>();
            int currentDamage = proj.Damage + (int)UpgradeManager.GetDamageBonus();
            if (proj != null)
            {
                proj.userType = UserType.Player;
                proj.Damage = currentDamage;
            }

            CameraShake.ShakeAll();
        }
    }
    #endregion
    
    // 백 대쉬 버튼이 눌렸을 때 호출
    public void OnBackDashButton()
    {
        // 이미 대쉬 중이거나 죽은 상태면 실행 안 함
        if (isDashing || isDead) return;

        StartCoroutine(BackDashRoutine());
    }

    private IEnumerator BackDashRoutine()
    {
        isDashing = true;
        // 대쉬 애니메이션 트리거 발동 (애니메이터에 "BackDash" 트리거 세팅)
        _playerAnimator.Play("Dash");

        float startTime = Time.time;
        Vector3 startPos = transform.position;
        Vector3 endPos = transform.position - transform.forward * dashDistance;

        // 일정 시간 동안 부드럽게 뒤로 이동
        while (Time.time < startTime + dashDuration)
        {
            float t = (Time.time - startTime) / dashDuration;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        transform.position = endPos;

        isDashing = false;
    }

    #region Death Handling
    /// <summary>
    /// 플레이어 사망 처리: 공격 중단, 사망 애니메이션 재생 후 사망 효과 실행
    /// </summary>
    protected override void Die()
    {
        if (_attackInterval > 0)
        {
            CancelInvoke(nameof(Attack));
        }

        isDead = true;
        animator.SetBool("isDie", true);
        StartCoroutine(PlayDeathEffect());
    }

    /// <summary>
    /// 사망 효과를 재생한 후 게임 종료(또는 게임오버) 처리를 호출합니다.
    /// </summary>
    private IEnumerator PlayDeathEffect()
    {
        yield return new WaitForSeconds(1f);
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        float elapsed = 0f;
        float effectDuration = 2f;
        while (elapsed < effectDuration)
        {
            float cutoff = Mathf.Lerp(0f, 1f, elapsed / effectDuration);
            foreach (Renderer rend in renderers)
            {
                foreach (Material mat in rend.materials)
                {
                    mat.SetFloat("_Cutout", cutoff);
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        foreach (Renderer rend in renderers)
        {
            foreach (Material mat in rend.materials)
            {
                mat.SetFloat("_Cutout", 1f);
            }
        }
        PlayerManager.Instance.Die();
    }
    #endregion
}
