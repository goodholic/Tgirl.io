using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Thinksquirrel.CShake;
using Unity.VisualScripting;

// 캐릭터 타입 열거형 추가
public enum CharacterType
{
    Shotgun,    // 샷건: 높은 데미지, 짧은 사거리, 보통 공격속도
    Bazooka,    // 바주카: 높은 데미지, 긴 사거리, 느린 공격속도
    Sword       // 검: 일반 데미지, 빠른 공격속도, 근접
}

/// <summary>
/// 플레이어의 이동, 공격, 회전, 애니메이션 및 IK를 처리하는 컨트롤러
/// </summary>
public class PlayerController : DamageableController
{
    #region Inspector Fields
    [Header("Character Type")]
    [SerializeField] private CharacterType _characterType = CharacterType.Shotgun;
    
    [SerializeField] private bool _isInit;
    [SerializeField] private Joystick _joystick;          // 이동 조이스틱
    [SerializeField] private float _moveSpeed = 5f;         // 이동 속도 (NavMeshAgent 속도)

    [SerializeField] private GameObject _muzzleFlashPrefab; // 머즐 플래시 효과 프리팹
    [SerializeField] private GameObject _projectilePrefab;  // 발사체 프리팹
    [SerializeField] private Transform _gunSpawnPoint;      // 발사체 생성 위치
    [SerializeField] private float _attackInterval = 0.3f;         // 공격 실행 간격 (초)
    [SerializeField] private AudioSource _attackAudioSource;  // 공격 사운드 재생용 AudioSource
    [SerializeField] private AudioClip _attackSoundClip;      // 공격 사운드 클립
    
    [Header("Character Type Stats")]
    [SerializeField] private float _shotgunDamageMultiplier = 1.5f;
    [SerializeField] private float _shotgunRange = 10f;
    [SerializeField] private float _shotgunAttackInterval = 0.5f;
    [SerializeField] private int _shotgunPelletCount = 5; // 산탄 개수
    [SerializeField] private float _shotgunSpreadAngle = 15f; // 산탄 퍼짐 각도
    
    [SerializeField] private float _bazookaDamageMultiplier = 2f;
    [SerializeField] private float _bazookaRange = 30f;
    [SerializeField] private float _bazookaAttackInterval = 1.5f;
    [SerializeField] private GameObject _bazookaExplosionPrefab; // 폭발 이펙트
    [SerializeField] private float _bazookaExplosionRadius = 5f;
    
    [SerializeField] private float _swordDamageMultiplier = 1f;
    [SerializeField] private float _swordAttackInterval = 0.2f;
    [SerializeField] private float _swordAttackRange = 3f;
    [SerializeField] private GameObject _swordSlashEffectPrefab; // 검 휘두르기 이펙트

    [SerializeField] private Animator _playerAnimator;      // 플레이어 애니메이터
    [SerializeField] private Transform _lookAtTarget;         // IK LookAt 대상
    [SerializeField] private Vector3 _lookAtOffset;           // LookAt 대상에 적용할 오프셋
    [SerializeField] private float _minLookAtY = 0.5f;          // LookAt 대상 최소 Y값
    [SerializeField] private float _maxLookAtY = 2.0f;          // LookAt 대상 최대 Y값
    [SerializeField] private Transform _leftHandTarget;       // 왼손 IK 타겟
    [SerializeField] private float _handIKWeight = 1.0f;        // 왼손 IK 가중치

    [SerializeField] private float _dragSensitivityX = 0.1f;  // 드래그 회전 X 민감도
    [SerializeField] private float _dragSensitivityY = 0.1f;  // 드래그 회전 Y 민감도
    
    [Header("Auto Aim Settings")]
    [SerializeField] private float _autoAimRadius = 20f;        // 자동 조준 검색 반경
    [SerializeField] private float _autoAimAngle = 45f;         // 자동 조준 각도 (정면 기준)
    [SerializeField] private float _autoAimStrength = 0.8f;     // 자동 조준 강도 (0~1)
    [SerializeField] private float _autoAimRotationSpeed = 5f;  // 자동 조준 회전 속도
    [SerializeField] private LayerMask _enemyLayerMask = -1;    // 적 레이어 마스크
    [SerializeField] private bool _enableAutoAim = true;        // 자동 조준 활성화 여부
    
    [Header("Dash Settings")]
    [SerializeField] private float dashDistance = 3f;  // 뒤로 이동할 거리
    [SerializeField] private float dashDuration = 0.2f;// 이동에 걸리는 시간
    private bool isDashing = false;
    
    [Header("Skill Settings")]
    // 스킬 1: 이동하면서 사용하는 스킬 (쿨타임 4초)
    [SerializeField] private float skill1Cooldown = 4f;
    [SerializeField] private float skill1Duration = 1f;
    [SerializeField] private float skill1MoveSpeedMultiplier = 1.5f;
    [SerializeField] private GameObject skill1EffectPrefab;
    private float skill1LastUsedTime = -10f;
    private bool isUsingSkill1 = false;
    
    // 스킬 2: 이동하면서 일반 공격 (쿨타임 2초)
    [SerializeField] private float skill2Cooldown = 2f;
    [SerializeField] private float skill2Duration = 0.5f;
    [SerializeField] private int skill2BulletCount = 5;
    [SerializeField] private float skill2BulletInterval = 0.1f;
    private float skill2LastUsedTime = -10f;
    private bool isUsingSkill2 = false;
    
    // 스킬 3: 궁극기 (쿨타임 6초)
    [SerializeField] private float ultimateCooldown = 6f;
    [SerializeField] private float ultimateDuration = 2f;
    [SerializeField] private GameObject ultimateEffectPrefab;
    [SerializeField] private float ultimateDamageRadius = 10f;
    [SerializeField] private int ultimateDamage = 50;
    private float ultimateLastUsedTime = -10f;
    private bool isUsingUltimate = false;
    
    // 스킬 캔슬 관련
    private Coroutine currentSkillCoroutine = null;
    
    [Header("Player Settings")]
    [SerializeField] private string _playerName = "Player"; // 플레이어 이름
    [SerializeField] private bool _isAI = false; // AI 플레이어인지 여부
    private PlayerController _lastAttacker; // 마지막으로 공격한 플레이어
    #endregion

    #region Private Fields
    private NavMeshAgent _agent;             // NavMeshAgent 컴포넌트
    private int _rotationTouchId = -1;       // 터치 회전 제어용 터치 ID (-1: 미사용)
    private Vector3 _lastMousePosition = Vector3.zero; // 에디터에서 마우스 드래그용 이전 마우스 위치
    private float _lastAttackTime = 0f;      // 마지막 공격 시간
    private Transform _currentAutoAimTarget = null; // 현재 자동 조준 타겟
    private Coroutine _autoAimCoroutine = null;    // 자동 조준 코루틴
    private bool _isMouseRotating = false;  // 마우스 오른쪽 버튼으로 회전 중인지 여부
    
    // 캐릭터 타입별 실제 적용 값들
    private float _actualAttackInterval;
    private float _actualDamageMultiplier;
    private float _actualAttackRange;
    #endregion
    
    public Transform LookAtTarget { get { return _lookAtTarget; } set { _lookAtTarget = value; } }
    public CharacterType CharacterTypeProperty => _characterType;
    public float AttackInterval => _actualAttackInterval;
    
    // AI 여부 설정 프로퍼티 추가
    public bool IsAI 
    { 
        get { return _isAI; } 
        set { _isAI = value; } 
    }

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
        
        // 캐릭터 타입별 스탯 초기화
        InitializeCharacterTypeStats();
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        
        // GameManager에 플레이어 등록
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPlayer(this, _playerName);
        }
        
        // EnemySpawner에 플레이어 등록 (AI 플레이어도 포함)
        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.RegisterPlayer(this);
        }
    }

    private void OnEnable()
    {
        // 자동 공격 제거됨
    }

    private void OnDisable()
    {
        // 자동 조준 코루틴 중지
        if (_autoAimCoroutine != null)
        {
            StopCoroutine(_autoAimCoroutine);
            _autoAimCoroutine = null;
        }
    }

    private void Update()
    {
        // AI가 아닌 경우에만 입력 처리
        if (!_isAI)
        {
            HandleKeyboardInput();
            HandleManualAttack();
            
#if UNITY_EDITOR
            HandleRotationByMouse();
#else
            HandleRotationByDrag();
#endif
        }
        
        if (!isDead)
        {
            // AI가 아닌 경우에만 이동 처리
            if (!_isAI)
            {
                HandleMovement();
            }
        }
        
        // AI가 아닌 경우에만 애니메이션 업데이트
        if (!_isAI)
        {
            UpdateAnimation();
        }
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
    
    private void OnDestroy()
    {
        // EnemySpawner에서 플레이어 제거
        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.UnregisterPlayer(this);
        }
    }
    #endregion
    
    #region Character Type Initialization
    /// <summary>
    /// 캐릭터 타입에 따른 스탯 초기화
    /// </summary>
    private void InitializeCharacterTypeStats()
    {
        switch (_characterType)
        {
            case CharacterType.Shotgun:
                _actualAttackInterval = _shotgunAttackInterval;
                _actualDamageMultiplier = _shotgunDamageMultiplier;
                _actualAttackRange = _shotgunRange;
                _autoAimRadius = _shotgunRange;
                break;
                
            case CharacterType.Bazooka:
                _actualAttackInterval = _bazookaAttackInterval;
                _actualDamageMultiplier = _bazookaDamageMultiplier;
                _actualAttackRange = _bazookaRange;
                _autoAimRadius = _bazookaRange;
                break;
                
            case CharacterType.Sword:
                _actualAttackInterval = _swordAttackInterval;
                _actualDamageMultiplier = _swordDamageMultiplier;
                _actualAttackRange = _swordAttackRange;
                _enableAutoAim = false; // 검은 자동 조준 비활성화
                break;
        }
    }
    #endregion
    
    public override void TakeDamage(float damage)
    {
        // 데미지 소스 추적 (임시로 현재 플레이어로 설정, 실제로는 Projectile에서 설정해야 함)
        base.TakeDamage(damage);
    }
    
    public void SetLastAttacker(PlayerController attacker)
    {
        _lastAttacker = attacker;
    }

    #region Input Handling
    /// <summary>
    /// 키보드 입력 처리
    /// </summary>
    private void HandleKeyboardInput()
    {
        if (isDead) return;
        
        // Q - 스킬 1
        if (Input.GetKeyDown(KeyCode.Q))
        {
            OnSkill1Button();
        }
        
        // E - 스킬 2
        if (Input.GetKeyDown(KeyCode.E))
        {
            OnSkill2Button();
        }
        
        // R - 궁극기
        if (Input.GetKeyDown(KeyCode.R))
        {
            OnUltimateButton();
        }
        
        // Shift - 백대쉬
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            OnBackDashButton();
        }
    }
    
    /// <summary>
    /// 수동 공격 처리 (마우스 왼쪽 클릭)
    /// </summary>
    private void HandleManualAttack()
    {
        if (isDead) return;
        
        // 마우스 왼쪽 버튼 클릭 시 공격
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            if (Time.time - _lastAttackTime >= _actualAttackInterval)
            {
                Attack();
                _lastAttackTime = Time.time;
            }
        }
    }
    #endregion

    #region Movement & Rotation
    /// <summary>
    /// 조이스틱 또는 키보드 입력을 기반으로 NavMeshAgent를 이용해 플레이어를 이동시킵니다.
    /// </summary>
    private void HandleMovement()
    {
        Vector2 input = Vector2.zero;
        
        // 키보드 입력 처리 (WASD)
        if (Input.GetKey(KeyCode.W)) input.y += 1f;
        if (Input.GetKey(KeyCode.S)) input.y -= 1f;
        if (Input.GetKey(KeyCode.A)) input.x -= 1f;
        if (Input.GetKey(KeyCode.D)) input.x += 1f;
        
        // 조이스틱 입력이 있으면 조이스틱 우선
        if (_joystick.InputDirection.sqrMagnitude > 0.01f)
        {
            input = _joystick.InputDirection;
        }
        
        if (input.sqrMagnitude > 0.01f)
        {
            // 입력 정규화
            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }
            
            // 카메라 기준 전후 좌우 방향 계산 (수평 성분만 사용)
            Vector3 camForward = Camera.main.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = Camera.main.transform.right;
            camRight.y = 0f;
            camRight.Normalize();

            Vector3 moveDir = camRight * input.x + camForward * input.y;
            
            // 스킬1 사용 중이면 이동속도 증가
            float currentSpeed = isUsingSkill1 ? _moveSpeed * skill1MoveSpeedMultiplier : _moveSpeed;
            _agent.speed = currentSpeed;
            
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
    /// 에디터 환경에서 마우스 오른쪽 버튼 드래그로 플레이어 회전을 처리합니다.
    /// </summary>
    private void HandleRotationByMouse()
    {
        // 마우스 오른쪽 버튼 누름
        if (Input.GetMouseButtonDown(1))
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                _isMouseRotating = true;
                _lastMousePosition = Input.mousePosition;
            }
        }
        
        // 마우스 오른쪽 버튼 드래그 중
        if (Input.GetMouseButton(1) && _isMouseRotating)
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
        
        // 마우스 오른쪽 버튼 뗌
        if (Input.GetMouseButtonUp(1))
        {
            _isMouseRotating = false;
            _lastMousePosition = Vector3.zero;
        }
    }
    #endregion

    #region Animation & Attack
    /// <summary>
    /// 조이스틱 또는 키보드 사용 여부에 따라 애니메이터 파라미터를 업데이트합니다.
    /// </summary>
    private void UpdateAnimation()
    {
        Vector2 input = Vector2.zero;
        
        // 키보드 입력
        if (Input.GetKey(KeyCode.W)) input.y += 1f;
        if (Input.GetKey(KeyCode.S)) input.y -= 1f;
        if (Input.GetKey(KeyCode.A)) input.x -= 1f;
        if (Input.GetKey(KeyCode.D)) input.x += 1f;
        
        // 조이스틱 입력이 있으면 조이스틱 우선
        if (_joystick.IsActive)
        {
            input = _joystick.InputDirection;
        }
        
        _playerAnimator.SetFloat("X", input.x);
        _playerAnimator.SetFloat("Y", input.y);
    }

    /// <summary>
    /// 자동 조준을 적용하여 가장 적합한 적을 찾습니다.
    /// </summary>
    private Transform GetAutoAimTarget()
    {
        if (!_enableAutoAim) return null;
        
        Transform bestTarget = null;
        float bestScore = float.MaxValue;
        
        // 범위 내의 모든 적을 검색
        Collider[] enemies = Physics.OverlapSphere(transform.position, _autoAimRadius, _enemyLayerMask);
        
        foreach (Collider enemy in enemies)
        {
            if (!enemy.CompareTag("Enemy")) continue;
            
            // 죽은 적은 제외
            DamageableController damageable = enemy.GetComponent<DamageableController>();
            if (damageable != null && damageable.isDead) continue;
            
            Vector3 directionToEnemy = enemy.transform.position - transform.position;
            float distanceToEnemy = directionToEnemy.magnitude;
            
            // 플레이어 정면과의 각도 계산
            float angle = Vector3.Angle(transform.forward, directionToEnemy);
            
            // 자동 조준 각도 내에 있는지 확인
            if (angle > _autoAimAngle) continue;
            
            // 화면 중앙과의 거리 계산 (화면에 보이는 적 우선)
            Vector3 screenPoint = Camera.main.WorldToViewportPoint(enemy.transform.position);
            
            // 화면 밖의 적은 제외
            if (screenPoint.z <= 0 || screenPoint.x < 0 || screenPoint.x > 1 || screenPoint.y < 0 || screenPoint.y > 1)
                continue;
            
            float screenDistance = Vector2.Distance(new Vector2(screenPoint.x, screenPoint.y), new Vector2(0.5f, 0.5f));
            
            // 점수 계산 (낮을수록 좋음)
            // 화면 중앙에 가깝고, 거리가 가까우며, 정면에 가까운 적을 우선시
            float angleWeight = angle / _autoAimAngle; // 0~1
            float distanceWeight = distanceToEnemy / _autoAimRadius; // 0~1
            float screenWeight = screenDistance; // 0~약 0.7
            
            float score = (angleWeight * 0.4f) + (distanceWeight * 0.3f) + (screenWeight * 0.3f);
            
            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = enemy.transform;
            }
        }
        
        return bestTarget;
    }

    /// <summary>
    /// 공격 실행: 캐릭터 타입에 따라 다른 공격 방식 적용
    /// </summary>
    public void Attack()
    {
        if (isDead)
            return;

        switch (_characterType)
        {
            case CharacterType.Shotgun:
                ShotgunAttack();
                break;
            case CharacterType.Bazooka:
                BazookaAttack();
                break;
            case CharacterType.Sword:
                SwordAttack();
                break;
        }
    }

    /// <summary>
    /// 샷건 공격: 산탄 형태로 여러 발사체 발사
    /// </summary>
    private void ShotgunAttack()
    {
        // 자동 조준 대상 찾기
        Transform autoAimTarget = GetAutoAimTarget();
        
        // 새로운 타겟이 발견되면 자동 조준 시작
        if (autoAimTarget != null && autoAimTarget != _currentAutoAimTarget)
        {
            _currentAutoAimTarget = autoAimTarget;
            
            // 기존 자동 조준 코루틴 중지
            if (_autoAimCoroutine != null)
            {
                StopCoroutine(_autoAimCoroutine);
            }
            
            // 새로운 자동 조준 코루틴 시작
            _autoAimCoroutine = StartCoroutine(AutoAimRoutine());
        }
        else if (autoAimTarget == null)
        {
            _currentAutoAimTarget = null;
            if (_autoAimCoroutine != null)
            {
                StopCoroutine(_autoAimCoroutine);
                _autoAimCoroutine = null;
            }
        }

        if (_muzzleFlashPrefab != null && _gunSpawnPoint != null)
        {
            Instantiate(_muzzleFlashPrefab, _gunSpawnPoint.position, _gunSpawnPoint.rotation);
        }
        
        if (_projectilePrefab != null && _gunSpawnPoint != null)
        {
            _attackAudioSource.clip = _attackSoundClip;
            _attackAudioSource.Play();

            // 샷건은 여러 발의 산탄을 발사
            for (int i = 0; i < _shotgunPelletCount; i++)
            {
                Vector3 targetPoint;
                
                // 자동 조준 시스템 적용
                if (_currentAutoAimTarget != null && _currentAutoAimTarget.gameObject.activeInHierarchy)
                {
                    // 자동 조준 대상이 있을 때
                    Vector3 targetPosition = _currentAutoAimTarget.position + Vector3.up * 0.8f; // 적의 몸통 높이를 타겟으로
                    
                    // 화면 중앙 방향과 자동 조준 방향을 보간
                    Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                    RaycastHit hit;
                    Vector3 screenTarget = Physics.Raycast(ray, out hit) ? hit.point : ray.GetPoint(1000f);
                    
                    // 자동 조준 방향과 화면 중앙 방향을 보간
                    Vector3 autoAimDirection = (targetPosition - _gunSpawnPoint.position).normalized;
                    Vector3 screenDirection = (screenTarget - _gunSpawnPoint.position).normalized;
                    Vector3 finalDirection = Vector3.Lerp(screenDirection, autoAimDirection, _autoAimStrength);
                    
                    targetPoint = _gunSpawnPoint.position + finalDirection * 100f;
                }
                else
                {
                    // 자동 조준 대상이 없으면 일반 조준
                    Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                    RaycastHit hit;
                    targetPoint = Physics.Raycast(ray, out hit) ? hit.point : ray.GetPoint(1000f);
                }

                // 산탄 퍼짐 효과 추가
                float spreadX = Random.Range(-_shotgunSpreadAngle, _shotgunSpreadAngle);
                float spreadY = Random.Range(-_shotgunSpreadAngle, _shotgunSpreadAngle);
                Vector3 spread = new Vector3(spreadX, spreadY, 0f);
                
                GameObject bullet = Instantiate(_projectilePrefab, _gunSpawnPoint.position, Quaternion.identity);
                bullet.transform.LookAt(targetPoint);
                bullet.transform.Rotate(spread);

                Projectile proj = bullet.GetComponent<Projectile>();
                int currentDamage = (int)((proj.Damage + UpgradeManager.GetDamageBonus()) * _actualDamageMultiplier);
                if (proj != null)
                {
                    proj.userType = UserType.Player;
                    proj.Damage = currentDamage;
                    proj.owner = this; // 발사체 소유자 설정
                }
            }

            CameraShake.ShakeAll();
        }
    }

    /// <summary>
    /// 바주카 공격: 폭발하는 투사체 발사
    /// </summary>
    private void BazookaAttack()
    {
        // 자동 조준 대상 찾기
        Transform autoAimTarget = GetAutoAimTarget();
        
        // 새로운 타겟이 발견되면 자동 조준 시작
        if (autoAimTarget != null && autoAimTarget != _currentAutoAimTarget)
        {
            _currentAutoAimTarget = autoAimTarget;
            
            // 기존 자동 조준 코루틴 중지
            if (_autoAimCoroutine != null)
            {
                StopCoroutine(_autoAimCoroutine);
            }
            
            // 새로운 자동 조준 코루틴 시작
            _autoAimCoroutine = StartCoroutine(AutoAimRoutine());
        }
        else if (autoAimTarget == null)
        {
            _currentAutoAimTarget = null;
            if (_autoAimCoroutine != null)
            {
                StopCoroutine(_autoAimCoroutine);
                _autoAimCoroutine = null;
            }
        }

        if (_muzzleFlashPrefab != null && _gunSpawnPoint != null)
        {
            Instantiate(_muzzleFlashPrefab, _gunSpawnPoint.position, _gunSpawnPoint.rotation);
        }
        
        if (_projectilePrefab != null && _gunSpawnPoint != null)
        {
            _attackAudioSource.clip = _attackSoundClip;
            _attackAudioSource.Play();

            Vector3 targetPoint;
            
            // 자동 조준 시스템 적용
            if (_currentAutoAimTarget != null && _currentAutoAimTarget.gameObject.activeInHierarchy)
            {
                // 자동 조준 대상이 있을 때
                Vector3 targetPosition = _currentAutoAimTarget.position + Vector3.up * 0.8f; // 적의 몸통 높이를 타겟으로
                
                // 화면 중앙 방향과 자동 조준 방향을 보간
                Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                RaycastHit hit;
                Vector3 screenTarget = Physics.Raycast(ray, out hit) ? hit.point : ray.GetPoint(1000f);
                
                // 자동 조준 방향과 화면 중앙 방향을 보간
                Vector3 autoAimDirection = (targetPosition - _gunSpawnPoint.position).normalized;
                Vector3 screenDirection = (screenTarget - _gunSpawnPoint.position).normalized;
                Vector3 finalDirection = Vector3.Lerp(screenDirection, autoAimDirection, _autoAimStrength);
                
                targetPoint = _gunSpawnPoint.position + finalDirection * 100f;
            }
            else
            {
                // 자동 조준 대상이 없으면 일반 조준
                Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                RaycastHit hit;
                targetPoint = Physics.Raycast(ray, out hit) ? hit.point : ray.GetPoint(1000f);
            }

            GameObject bullet = Instantiate(_projectilePrefab, _gunSpawnPoint.position, Quaternion.identity);
            bullet.transform.LookAt(targetPoint);

            Projectile proj = bullet.GetComponent<Projectile>();
            int currentDamage = (int)((proj.Damage + UpgradeManager.GetDamageBonus()) * _actualDamageMultiplier);
            if (proj != null)
            {
                proj.userType = UserType.Player;
                proj.Damage = currentDamage;
                proj.owner = this; // 발사체 소유자 설정
                
                // 바주카 특수 효과: 폭발 이펙트 추가
                StartCoroutine(BazookaExplosionOnImpact(bullet));
            }

            CameraShake.ShakeAll();
        }
    }

    /// <summary>
    /// 바주카 폭발 효과 코루틴
    /// </summary>
    private IEnumerator BazookaExplosionOnImpact(GameObject projectile)
    {
        // 발사체가 파괴될 때까지 대기
        while (projectile != null)
        {
            yield return null;
        }
        
        // 여기에 폭발 효과 구현 (실제로는 Projectile 스크립트에서 처리하는 것이 좋음)
    }

    /// <summary>
    /// 검 공격: 근접 범위 공격
    /// </summary>
    private void SwordAttack()
    {
        if (_swordSlashEffectPrefab != null)
        {
            Instantiate(_swordSlashEffectPrefab, transform.position + transform.forward * 1.5f, transform.rotation);
        }
        
        _attackAudioSource.clip = _attackSoundClip;
        _attackAudioSource.Play();
        
        // 검 휘두르기 애니메이션 트리거
        _playerAnimator.SetTrigger("SwordAttack");
        
        // 전방 범위 내의 모든 적에게 데미지
        Collider[] enemies = Physics.OverlapSphere(transform.position + transform.forward * (_swordAttackRange * 0.5f), _swordAttackRange);
        foreach (Collider enemy in enemies)
        {
            // 적(몬스터) 공격
            if (enemy.CompareTag("Enemy"))
            {
                // 전방 각도 체크
                Vector3 directionToEnemy = (enemy.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, directionToEnemy);
                
                if (angle < 90f) // 전방 180도 범위
                {
                    DamageableController target = enemy.GetComponent<DamageableController>();
                    if (target != null && !target.isDead)
                    {
                        // 적에게 공격자 정보 전달
                        EnemyController enemyController = enemy.GetComponent<EnemyController>();
                        if (enemyController != null)
                        {
                            enemyController.SetAttacker(this);
                        }
                        
                        int damage = (int)((10 + UpgradeManager.GetDamageBonus()) * _actualDamageMultiplier);
                        target.TakeDamage(damage);
                    }
                }
            }
            // 다른 플레이어 공격 (PvP)
            else if (enemy.CompareTag("Player"))
            {
                PlayerController otherPlayer = enemy.GetComponent<PlayerController>();
                // 자기 자신이 아닌 다른 플레이어만 공격
                if (otherPlayer != null && otherPlayer != this && !otherPlayer.isDead)
                {
                    // 전방 각도 체크
                    Vector3 directionToPlayer = (enemy.transform.position - transform.position).normalized;
                    float angle = Vector3.Angle(transform.forward, directionToPlayer);
                    
                    if (angle < 90f) // 전방 180도 범위
                    {
                        otherPlayer.SetLastAttacker(this); // 공격자 설정
                        int damage = (int)((10 + UpgradeManager.GetDamageBonus()) * _actualDamageMultiplier);
                        otherPlayer.TakeDamage(damage);
                    }
                }
            }
        }
        
        CameraShake.ShakeAll();
    }
    
    /// <summary>
    /// 자동 조준 코루틴: 적을 향해 카메라를 부드럽게 회전시킵니다.
    /// </summary>
    private IEnumerator AutoAimRoutine()
    {
        while (_currentAutoAimTarget != null && _currentAutoAimTarget.gameObject.activeInHierarchy)
        {
            // 적이 죽었거나 범위를 벗어났으면 중지
            DamageableController targetDamageable = _currentAutoAimTarget.GetComponent<DamageableController>();
            float distance = Vector3.Distance(transform.position, _currentAutoAimTarget.position);
            
            if ((targetDamageable != null && targetDamageable.isDead) || distance > _autoAimRadius)
            {
                _currentAutoAimTarget = null;
                _autoAimCoroutine = null;
                yield break;
            }
            
            // 적을 향한 방향 계산
            Vector3 directionToTarget = (_currentAutoAimTarget.position - transform.position).normalized;
            directionToTarget.y = 0; // 수평 회전만
            
            // 목표 회전 계산
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            
            // 부드럽게 회전
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _autoAimRotationSpeed * Time.deltaTime);
            
            yield return null;
        }
        
        _currentAutoAimTarget = null;
        _autoAimCoroutine = null;
    }
    #endregion
    
    #region Skill System
    // 백 대쉬 버튼이 눌렸을 때 호출
    public void OnBackDashButton()
    {
        // 이미 대쉬 중이거나 죽은 상태면 실행 안 함
        if (isDashing || isDead) return;

        CancelCurrentSkill();
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
    
    // 스킬 1: 이동하면서 사용하는 스킬 (이동속도 증가 + 이펙트)
    public void OnSkill1Button()
    {
        if (isDead || Time.time - skill1LastUsedTime < skill1Cooldown) return;
        
        CancelCurrentSkill();
        currentSkillCoroutine = StartCoroutine(Skill1Routine());
    }
    
    private IEnumerator Skill1Routine()
    {
        skill1LastUsedTime = Time.time;
        isUsingSkill1 = true;
        
        // 스킬 애니메이션 재생
        _playerAnimator.SetTrigger("Skill1");
        
        // 이펙트 생성
        GameObject effect = null;
        if (skill1EffectPrefab != null)
        {
            effect = Instantiate(skill1EffectPrefab, transform.position, transform.rotation);
            effect.transform.SetParent(transform);
        }
        
        // 스킬 지속시간 동안 대기
        yield return new WaitForSeconds(skill1Duration);
        
        // 이펙트 제거
        if (effect != null)
        {
            Destroy(effect);
        }
        
        isUsingSkill1 = false;
        currentSkillCoroutine = null;
    }
    
    // 스킬 2: 이동하면서 연속 사격
    public void OnSkill2Button()
    {
        if (isDead || Time.time - skill2LastUsedTime < skill2Cooldown) return;
        
        CancelCurrentSkill();
        currentSkillCoroutine = StartCoroutine(Skill2Routine());
    }
    
    private IEnumerator Skill2Routine()
    {
        skill2LastUsedTime = Time.time;
        isUsingSkill2 = true;
        
        // 스킬 애니메이션 재생
        _playerAnimator.SetTrigger("Skill2");
        
        // 연속 사격
        for (int i = 0; i < skill2BulletCount; i++)
        {
            if (isDead) break;
            
            // 캐릭터 타입에 따른 공격 실행
            Attack();
            
            yield return new WaitForSeconds(skill2BulletInterval);
        }
        
        isUsingSkill2 = false;
        currentSkillCoroutine = null;
    }
    
    // 스킬 3: 궁극기 (범위 공격)
    public void OnUltimateButton()
    {
        if (isDead || Time.time - ultimateLastUsedTime < ultimateCooldown) return;
        
        CancelCurrentSkill();
        currentSkillCoroutine = StartCoroutine(UltimateRoutine());
    }
    
    private IEnumerator UltimateRoutine()
    {
        ultimateLastUsedTime = Time.time;
        isUsingUltimate = true;
        
        // 궁극기 애니메이션 재생
        _playerAnimator.SetTrigger("Ultimate");
        
        // 궁극기 이펙트 생성
        GameObject effect = null;
        if (ultimateEffectPrefab != null)
        {
            effect = Instantiate(ultimateEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // 0.5초 후 범위 데미지 적용
        yield return new WaitForSeconds(0.5f);
        
        // 범위 내 모든 적에게 데미지
        Collider[] enemies = Physics.OverlapSphere(transform.position, ultimateDamageRadius);
        foreach (Collider enemy in enemies)
        {
            if (enemy.CompareTag("Enemy"))
            {
                DamageableController target = enemy.GetComponent<DamageableController>();
                if (target != null)
                {
                    target.TakeDamage(ultimateDamage + (int)UpgradeManager.GetDamageBonus());
                }
            }
        }
        
        // 카메라 쉐이크 (강하게)
        CameraShake.ShakeAll();
        
        // 나머지 지속시간 대기
        yield return new WaitForSeconds(ultimateDuration - 0.5f);
        
        // 이펙트 제거
        if (effect != null)
        {
            Destroy(effect);
        }
        
        isUsingUltimate = false;
        currentSkillCoroutine = null;
    }
    
    // 현재 진행 중인 스킬 캔슬
    private void CancelCurrentSkill()
    {
        if (currentSkillCoroutine != null)
        {
            StopCoroutine(currentSkillCoroutine);
            currentSkillCoroutine = null;
        }
        
        isUsingSkill1 = false;
        isUsingSkill2 = false;
        isUsingUltimate = false;
    }
    #endregion

    #region Cooldown Getters
    // UI에서 쿨타임 정보를 가져올 수 있도록 하는 메서드들
    public float GetSkill1CooldownRemaining()
    {
        float elapsed = Time.time - skill1LastUsedTime;
        return Mathf.Max(0, skill1Cooldown - elapsed);
    }
    
    public float GetSkill1Cooldown()
    {
        return skill1Cooldown;
    }
    
    public float GetSkill2CooldownRemaining()
    {
        float elapsed = Time.time - skill2LastUsedTime;
        return Mathf.Max(0, skill2Cooldown - elapsed);
    }
    
    public float GetSkill2Cooldown()
    {
        return skill2Cooldown;
    }
    
    public float GetUltimateCooldownRemaining()
    {
        float elapsed = Time.time - ultimateLastUsedTime;
        return Mathf.Max(0, ultimateCooldown - elapsed);
    }
    
    public float GetUltimateCooldown()
    {
        return ultimateCooldown;
    }
    #endregion

    #region Death Handling
    /// <summary>
    /// 플레이어 사망 처리: 공격 중단, 사망 애니메이션 재생 후 사망 효과 실행
    /// </summary>
    protected override void Die()
    {
        // 자동 조준 코루틴 중지
        if (_autoAimCoroutine != null)
        {
            StopCoroutine(_autoAimCoroutine);
            _autoAimCoroutine = null;
        }
        
        // GameManager에 플레이어 사망 알림 (PvP 킬 처리)
        if (GameManager.Instance != null && _lastAttacker != null && _lastAttacker != this)
        {
            GameManager.Instance.AddPlayerKill(_lastAttacker, this);
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
        
        // 현재 플레이어가 죽었을 때만 게임오버 처리
        if (this == PlayerManager.Instance.curPlayer)
        {
            PlayerManager.Instance.Die();
        }
    }
    #endregion
}