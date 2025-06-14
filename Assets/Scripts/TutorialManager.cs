using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }
    
    [Header("Tutorial UI")]
    [SerializeField] private GameObject _tutorialCanvas;
    [SerializeField] private GameObject _tutorialPanel;
    [SerializeField] private TextMeshProUGUI _tutorialText;
    [SerializeField] private TextMeshProUGUI _tutorialTitleText;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _skipButton;
    [SerializeField] private Image _highlightImage;
    [SerializeField] private GameObject _arrowIndicator;
    
    [Header("Tutorial Steps")]
    [SerializeField] private TutorialStep[] _tutorialSteps;
    
    [Header("Target References")]
    [SerializeField] private Joystick _joystick;
    [SerializeField] private Button _backDashButton;
    [SerializeField] private GameObject _dummyEnemy;
    [SerializeField] private Transform _dummyEnemySpawnPoint;
    
    [Header("Settings")]
    [SerializeField] private float _typewriterSpeed = 0.05f;
    [SerializeField] private bool _isFirstTimePlayer = true;
    
    private int _currentStepIndex = 0;
    private bool _isTyping = false;
    private Coroutine _typewriterCoroutine;
    
    [System.Serializable]
    public class TutorialStep
    {
        public string title;
        [TextArea(3, 5)]
        public string description;
        public TutorialAction action;
        public GameObject highlightTarget;
        public Vector2 arrowOffset;
        public bool waitForAction = true;
    }
    
    public enum TutorialAction
    {
        None,
        Move,
        Rotate,
        Attack,
        BackDash,
        Complete
    }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // 첫 접속 여부 확인
        _isFirstTimePlayer = PlayerPrefs.GetInt("HasCompletedTutorial", 0) == 0;
        
        if (_nextButton != null)
        {
            _nextButton.onClick.AddListener(OnNextButtonClicked);
        }
        
        if (_skipButton != null)
        {
            _skipButton.onClick.AddListener(SkipTutorial);
        }
    }
    
    private void Start()
    {
        if (_isFirstTimePlayer)
        {
            StartTutorial();
        }
        else
        {
            // 튜토리얼을 이미 완료한 경우 바로 메인 게임으로
            _tutorialCanvas.SetActive(false);
            EnableGameplay();
        }
    }
    
    /// <summary>
    /// 튜토리얼 시작
    /// </summary>
    public void StartTutorial()
    {
        _tutorialCanvas.SetActive(true);
        _currentStepIndex = 0;
        Time.timeScale = 0f; // 게임 일시정지
        DisableGameplay();
        ShowCurrentStep();
    }
    
    /// <summary>
    /// 현재 튜토리얼 단계 표시
    /// </summary>
    private void ShowCurrentStep()
    {
        if (_currentStepIndex >= _tutorialSteps.Length)
        {
            CompleteTutorial();
            return;
        }
        
        TutorialStep currentStep = _tutorialSteps[_currentStepIndex];
        
        // 제목 설정
        if (_tutorialTitleText != null)
        {
            _tutorialTitleText.text = currentStep.title;
        }
        
        // 설명 텍스트 타이핑 효과
        if (_typewriterCoroutine != null)
        {
            StopCoroutine(_typewriterCoroutine);
        }
        _typewriterCoroutine = StartCoroutine(TypewriterEffect(currentStep.description));
        
        // 하이라이트 표시
        if (currentStep.highlightTarget != null && _highlightImage != null)
        {
            _highlightImage.gameObject.SetActive(true);
            PositionHighlight(currentStep.highlightTarget);
        }
        else
        {
            _highlightImage.gameObject.SetActive(false);
        }
        
        // 화살표 표시
        if (currentStep.highlightTarget != null && _arrowIndicator != null)
        {
            _arrowIndicator.SetActive(true);
            PositionArrow(currentStep.highlightTarget, currentStep.arrowOffset);
        }
        else
        {
            _arrowIndicator.SetActive(false);
        }
        
        // 액션에 따른 처리
        HandleTutorialAction(currentStep.action);
        
        // 다음 버튼 활성화 설정
        _nextButton.interactable = !currentStep.waitForAction;
    }
    
    /// <summary>
    /// 타이핑 효과로 텍스트 표시
    /// </summary>
    private IEnumerator TypewriterEffect(string text)
    {
        _isTyping = true;
        _tutorialText.text = "";
        
        foreach (char letter in text.ToCharArray())
        {
            _tutorialText.text += letter;
            yield return new WaitForSecondsRealtime(_typewriterSpeed);
        }
        
        _isTyping = false;
    }
    
    /// <summary>
    /// 하이라이트 위치 조정
    /// </summary>
    private void PositionHighlight(GameObject target)
    {
        RectTransform targetRect = target.GetComponent<RectTransform>();
        if (targetRect != null)
        {
            RectTransform highlightRect = _highlightImage.GetComponent<RectTransform>();
            highlightRect.position = targetRect.position;
            highlightRect.sizeDelta = targetRect.sizeDelta * 1.2f; // 약간 크게
        }
    }
    
    /// <summary>
    /// 화살표 위치 조정
    /// </summary>
    private void PositionArrow(GameObject target, Vector2 offset)
    {
        RectTransform targetRect = target.GetComponent<RectTransform>();
        if (targetRect != null)
        {
            RectTransform arrowRect = _arrowIndicator.GetComponent<RectTransform>();
            arrowRect.position = targetRect.position + new Vector3(offset.x, offset.y, 0);
        }
    }
    
    /// <summary>
    /// 튜토리얼 액션 처리
    /// </summary>
    private void HandleTutorialAction(TutorialAction action)
    {
        switch (action)
        {
            case TutorialAction.Move:
                // 이동 튜토리얼
                EnableMovement();
                StartCoroutine(CheckMovementCompletion());
                break;
                
            case TutorialAction.Rotate:
                // 회전 튜토리얼
                EnableRotation();
                StartCoroutine(CheckRotationCompletion());
                break;
                
            case TutorialAction.Attack:
                // 공격 튜토리얼
                EnableAttack();
                SpawnDummyEnemy();
                StartCoroutine(CheckAttackCompletion());
                break;
                
            case TutorialAction.BackDash:
                // 백대쉬 튜토리얼
                EnableBackDash();
                StartCoroutine(CheckBackDashCompletion());
                break;
                
            case TutorialAction.Complete:
                // 튜토리얼 완료
                _nextButton.interactable = true;
                break;
        }
    }
    
    /// <summary>
    /// 이동 완료 체크
    /// </summary>
    private IEnumerator CheckMovementCompletion()
    {
        float moveDistance = 0f;
        Vector3 lastPosition = PlayerManager.Instance.curPlayer.transform.position;
        
        while (moveDistance < 5f) // 5유닛 이상 이동 시 완료
        {
            yield return new WaitForSecondsRealtime(0.1f);
            Vector3 currentPosition = PlayerManager.Instance.curPlayer.transform.position;
            moveDistance += Vector3.Distance(lastPosition, currentPosition);
            lastPosition = currentPosition;
        }
        
        OnActionCompleted();
    }
    
    /// <summary>
    /// 회전 완료 체크
    /// </summary>
    private IEnumerator CheckRotationCompletion()
    {
        float totalRotation = 0f;
        Quaternion lastRotation = PlayerManager.Instance.curPlayer.transform.rotation;
        
        while (totalRotation < 90f) // 90도 이상 회전 시 완료
        {
            yield return new WaitForSecondsRealtime(0.1f);
            Quaternion currentRotation = PlayerManager.Instance.curPlayer.transform.rotation;
            totalRotation += Quaternion.Angle(lastRotation, currentRotation);
            lastRotation = currentRotation;
        }
        
        OnActionCompleted();
    }
    
    /// <summary>
    /// 공격 완료 체크
    /// </summary>
    private IEnumerator CheckAttackCompletion()
    {
        while (_dummyEnemy != null && _dummyEnemy.activeSelf)
        {
            yield return new WaitForSecondsRealtime(0.1f);
        }
        
        OnActionCompleted();
    }
    
    /// <summary>
    /// 백대쉬 완료 체크
    /// </summary>
    private IEnumerator CheckBackDashCompletion()
    {
        bool dashPressed = false;
        
        // 백대쉬 버튼이 눌릴 때까지 대기
        while (!dashPressed)
        {
            // 모바일에서는 버튼 클릭, PC에서는 Shift 키 체크
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                dashPressed = true;
            }
            yield return null;
        }
        
        // 백대쉬 애니메이션이 끝날 때까지 대기
        yield return new WaitForSecondsRealtime(0.5f);
        
        OnActionCompleted();
    }
    
    /// <summary>
    /// 더미 적 생성
    /// </summary>
    private void SpawnDummyEnemy()
    {
        if (_dummyEnemy != null && _dummyEnemySpawnPoint != null)
        {
            _dummyEnemy.SetActive(true);
            _dummyEnemy.transform.position = _dummyEnemySpawnPoint.position;
            
            // 더미 적의 체력을 낮게 설정
            DamageableController damageable = _dummyEnemy.GetComponent<DamageableController>();
            if (damageable != null)
            {
                damageable._health = 10f;
                damageable._maxHealth = 10f;
            }
        }
    }
    
    /// <summary>
    /// 액션 완료 시 호출
    /// </summary>
    private void OnActionCompleted()
    {
        _nextButton.interactable = true;
        
        // 완료 효과음 재생
        // AudioManager.Instance.PlaySound("TutorialComplete");
        
        // 완료 메시지 표시
        if (_tutorialText != null)
        {
            _tutorialText.text += "\n\n<color=green>완료! 다음 버튼을 클릭하세요.</color>";
        }
    }
    
    /// <summary>
    /// 다음 버튼 클릭
    /// </summary>
    private void OnNextButtonClicked()
    {
        if (_isTyping)
        {
            // 타이핑 중이면 즉시 완료
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
            }
            _tutorialText.text = _tutorialSteps[_currentStepIndex].description;
            _isTyping = false;
        }
        else
        {
            // 다음 단계로
            _currentStepIndex++;
            ShowCurrentStep();
        }
    }
    
    /// <summary>
    /// 튜토리얼 건너뛰기
    /// </summary>
    private void SkipTutorial()
    {
        CompleteTutorial();
    }
    
    /// <summary>
    /// 튜토리얼 완료
    /// </summary>
    private void CompleteTutorial()
    {
        // 완료 플래그 저장
        PlayerPrefs.SetInt("HasCompletedTutorial", 1);
        PlayerPrefs.Save();
        
        // UI 숨기기
        _tutorialCanvas.SetActive(false);
        
        // 게임플레이 활성화
        Time.timeScale = 1f;
        EnableGameplay();
        
        // 메인 로비로 이동
        // SceneManager.LoadScene("MainLobby");
    }
    
    /// <summary>
    /// 게임플레이 비활성화
    /// </summary>
    private void DisableGameplay()
    {
        if (PlayerManager.Instance != null && PlayerManager.Instance.curPlayer != null)
        {
            PlayerController player = PlayerManager.Instance.curPlayer;
            player.enabled = false;
        }
        
        if (_joystick != null)
        {
            _joystick.enabled = false;
        }
    }
    
    /// <summary>
    /// 게임플레이 활성화
    /// </summary>
    private void EnableGameplay()
    {
        if (PlayerManager.Instance != null && PlayerManager.Instance.curPlayer != null)
        {
            PlayerController player = PlayerManager.Instance.curPlayer;
            player.enabled = true;
        }
        
        if (_joystick != null)
        {
            _joystick.enabled = true;
        }
    }
    
    /// <summary>
    /// 이동만 활성화
    /// </summary>
    private void EnableMovement()
    {
        if (_joystick != null)
        {
            _joystick.enabled = true;
        }
    }
    
    /// <summary>
    /// 회전만 활성화
    /// </summary>
    private void EnableRotation()
    {
        if (PlayerManager.Instance != null && PlayerManager.Instance.curPlayer != null)
        {
            PlayerController player = PlayerManager.Instance.curPlayer;
            player.enabled = true;
        }
    }
    
    /// <summary>
    /// 공격만 활성화
    /// </summary>
    private void EnableAttack()
    {
        EnableGameplay();
    }
    
    /// <summary>
    /// 백대쉬만 활성화
    /// </summary>
    private void EnableBackDash()
    {
        if (_backDashButton != null)
        {
            _backDashButton.interactable = true;
        }
    }
}