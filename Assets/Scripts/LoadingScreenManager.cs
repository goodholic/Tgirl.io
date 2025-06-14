using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LoadingScreenManager : MonoBehaviour
{
    public static LoadingScreenManager Instance { get; private set; }
    
    [Header("Loading Screen UI")]
    [SerializeField] private GameObject _loadingScreenCanvas;
    [SerializeField] private Image _progressBar;
    [SerializeField] private TextMeshProUGUI _loadingPercentText;
    [SerializeField] private TextMeshProUGUI _loadingStatusText;
    [SerializeField] private TextMeshProUGUI _tipText;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private GameObject _loadingAnimation;
    
    [Header("Tip System")]
    [SerializeField] private float _tipDisplayDuration = 3f;
    [SerializeField] private float _tipFadeDuration = 0.5f;
    [SerializeField] private string[] _gameplayTips;
    [SerializeField] private string[] _characterTips;
    [SerializeField] private string[] _strategyTips;
    
    [Header("Loading Images")]
    [SerializeField] private Sprite[] _loadingBackgrounds;
    [SerializeField] private bool _randomizeBackground = true;
    
    [Header("Fake Loading Settings")]
    [SerializeField] private float _minimumLoadTime = 2f;
    [SerializeField] private float _maximumLoadTime = 5f;
    [SerializeField] private AnimationCurve _loadingCurve;
    
    private AsyncOperation _sceneLoadOperation;
    private Coroutine _tipRotationCoroutine;
    private float _targetProgress = 0f;
    private float _currentProgress = 0f;
    private bool _isLoading = false;
    private string _currentSceneName;
    private Queue<string> _loadingSteps = new Queue<string>();
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 초기 상태 설정
        if (_loadingScreenCanvas)
            _loadingScreenCanvas.SetActive(false);
    }
    
    /// <summary>
    /// 씬 로드 시작
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (_isLoading) return;
        
        _currentSceneName = sceneName;
        StartCoroutine(LoadSceneRoutine(sceneName));
    }
    
    /// <summary>
    /// 씬 로드 루틴
    /// </summary>
    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        _isLoading = true;
        
        // 로딩 화면 표시
        ShowLoadingScreen();
        
        // 로딩 단계 설정
        SetupLoadingSteps(sceneName);
        
        // 최소 로딩 시간
        float elapsedTime = 0f;
        float totalLoadTime = Random.Range(_minimumLoadTime, _maximumLoadTime);
        
        // 씬 비동기 로드 시작
        _sceneLoadOperation = SceneManager.LoadSceneAsync(sceneName);
        _sceneLoadOperation.allowSceneActivation = false;
        
        // 로딩 진행
        while (elapsedTime < totalLoadTime || _sceneLoadOperation.progress < 0.9f)
        {
            elapsedTime += Time.deltaTime;
            
            // 실제 로딩 진행도와 가짜 진행도 중 더 큰 값 사용
            float realProgress = _sceneLoadOperation.progress / 0.9f;
            float fakeProgress = _loadingCurve.Evaluate(elapsedTime / totalLoadTime);
            _targetProgress = Mathf.Max(realProgress, fakeProgress);
            
            // 부드러운 진행바 업데이트
            _currentProgress = Mathf.Lerp(_currentProgress, _targetProgress, Time.deltaTime * 5f);
            UpdateProgressUI(_currentProgress);
            
            // 로딩 단계 텍스트 업데이트
            UpdateLoadingStepText(_currentProgress);
            
            yield return null;
        }
        
        // 로딩 완료
        _targetProgress = 1f;
        while (_currentProgress < 0.99f)
        {
            _currentProgress = Mathf.Lerp(_currentProgress, _targetProgress, Time.deltaTime * 5f);
            UpdateProgressUI(_currentProgress);
            yield return null;
        }
        
        UpdateProgressUI(1f);
        if (_loadingStatusText)
            _loadingStatusText.text = "로딩 완료!";
        
        yield return new WaitForSeconds(0.5f);
        
        // 씬 활성화
        _sceneLoadOperation.allowSceneActivation = true;
        
        // 페이드 아웃
        yield return StartCoroutine(FadeOut());
        
        // 로딩 화면 숨기기
        HideLoadingScreen();
        _isLoading = false;
    }
    
    /// <summary>
    /// 로딩 화면 표시
    /// </summary>
    private void ShowLoadingScreen()
    {
        if (_loadingScreenCanvas)
            _loadingScreenCanvas.SetActive(true);
        
        // 배경 이미지 설정
        if (_randomizeBackground && _loadingBackgrounds != null && _loadingBackgrounds.Length > 0)
        {
            _backgroundImage.sprite = _loadingBackgrounds[Random.Range(0, _loadingBackgrounds.Length)];
        }
        
        // 초기화
        _currentProgress = 0f;
        _targetProgress = 0f;
        UpdateProgressUI(0f);
        
        // 팁 로테이션 시작
        if (_tipRotationCoroutine != null)
            StopCoroutine(_tipRotationCoroutine);
        _tipRotationCoroutine = StartCoroutine(RotateTips());
        
        // 로딩 애니메이션 시작
        if (_loadingAnimation)
            _loadingAnimation.SetActive(true);
    }
    
    /// <summary>
    /// 로딩 화면 숨기기
    /// </summary>
    private void HideLoadingScreen()
    {
        if (_loadingScreenCanvas)
            _loadingScreenCanvas.SetActive(false);
        
        // 팁 로테이션 중지
        if (_tipRotationCoroutine != null)
        {
            StopCoroutine(_tipRotationCoroutine);
            _tipRotationCoroutine = null;
        }
        
        // 로딩 애니메이션 중지
        if (_loadingAnimation)
            _loadingAnimation.SetActive(false);
    }
    
    /// <summary>
    /// 로딩 단계 설정
    /// </summary>
    private void SetupLoadingSteps(string sceneName)
    {
        _loadingSteps.Clear();
        
        switch (sceneName)
        {
            case "GameScene":
                _loadingSteps.Enqueue("맵 데이터 로드 중...");
                _loadingSteps.Enqueue("캐릭터 모델 준비 중...");
                _loadingSteps.Enqueue("적 AI 초기화 중...");
                _loadingSteps.Enqueue("무기 시스템 로드 중...");
                _loadingSteps.Enqueue("네트워크 연결 중...");
                _loadingSteps.Enqueue("게임 시작 준비 중...");
                break;
                
            case "MainLobby":
                _loadingSteps.Enqueue("로비 데이터 로드 중...");
                _loadingSteps.Enqueue("플레이어 정보 동기화 중...");
                _loadingSteps.Enqueue("상점 데이터 로드 중...");
                _loadingSteps.Enqueue("UI 초기화 중...");
                break;
                
            default:
                _loadingSteps.Enqueue("씬 데이터 로드 중...");
                _loadingSteps.Enqueue("리소스 준비 중...");
                _loadingSteps.Enqueue("초기화 중...");
                break;
        }
    }
    
    /// <summary>
    /// 진행도 UI 업데이트
    /// </summary>
    private void UpdateProgressUI(float progress)
    {
        if (_progressBar)
            _progressBar.fillAmount = progress;
        
        if (_loadingPercentText)
            _loadingPercentText.text = $"{Mathf.RoundToInt(progress * 100)}%";
    }
    
    /// <summary>
    /// 로딩 단계 텍스트 업데이트
    /// </summary>
    private void UpdateLoadingStepText(float progress)
    {
        if (_loadingStatusText == null || _loadingSteps.Count == 0) return;
        
        int stepIndex = Mathf.FloorToInt(progress * _loadingSteps.Count);
        stepIndex = Mathf.Clamp(stepIndex, 0, _loadingSteps.Count - 1);
        
        string[] steps = new string[_loadingSteps.Count];
        _loadingSteps.CopyTo(steps, 0);
        
        _loadingStatusText.text = steps[stepIndex];
    }
    
    /// <summary>
    /// 팁 로테이션
    /// </summary>
    private IEnumerator RotateTips()
    {
        List<string> allTips = new List<string>();
        allTips.AddRange(_gameplayTips);
        allTips.AddRange(_characterTips);
        allTips.AddRange(_strategyTips);
        
        // 팁 섞기
        for (int i = 0; i < allTips.Count; i++)
        {
            string temp = allTips[i];
            int randomIndex = Random.Range(i, allTips.Count);
            allTips[i] = allTips[randomIndex];
            allTips[randomIndex] = temp;
        }
        
        int currentTipIndex = 0;
        
        while (true)
        {
            if (allTips.Count > 0)
            {
                // 페이드 인
                yield return StartCoroutine(FadeTip(0f, 1f));
                
                // 팁 표시
                if (_tipText)
                    _tipText.text = $"💡 팁: {allTips[currentTipIndex]}";
                
                yield return new WaitForSeconds(_tipDisplayDuration);
                
                // 페이드 아웃
                yield return StartCoroutine(FadeTip(1f, 0f));
                
                // 다음 팁으로
                currentTipIndex = (currentTipIndex + 1) % allTips.Count;
            }
            
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    /// <summary>
    /// 팁 페이드 효과
    /// </summary>
    private IEnumerator FadeTip(float fromAlpha, float toAlpha)
    {
        if (_tipText == null) yield break;
        
        float elapsed = 0f;
        Color tipColor = _tipText.color;
        
        while (elapsed < _tipFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _tipFadeDuration;
            tipColor.a = Mathf.Lerp(fromAlpha, toAlpha, t);
            _tipText.color = tipColor;
            yield return null;
        }
        
        tipColor.a = toAlpha;
        _tipText.color = tipColor;
    }
    
    /// <summary>
    /// 화면 페이드 아웃
    /// </summary>
    private IEnumerator FadeOut()
    {
        // 페이드 아웃 효과 구현
        // CanvasGroup을 사용하거나 별도의 페이드 이미지 사용
        yield return new WaitForSeconds(0.5f);
    }
    
    /// <summary>
    /// 빠른 로딩 (작은 씬용)
    /// </summary>
    public void QuickLoad(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
    
    /// <summary>
    /// 로딩 진행도 직접 설정 (다운로드 등에 사용)
    /// </summary>
    public void SetProgress(float progress, string status = "")
    {
        _targetProgress = Mathf.Clamp01(progress);
        
        if (!string.IsNullOrEmpty(status) && _loadingStatusText)
            _loadingStatusText.text = status;
    }
    
    /// <summary>
    /// 커스텀 팁 추가
    /// </summary>
    public void AddCustomTip(string tip)
    {
        List<string> tips = new List<string>(_gameplayTips);
        tips.Add(tip);
        _gameplayTips = tips.ToArray();
    }
    
    /// <summary>
    /// 현재 로딩 중인지 확인
    /// </summary>
    public bool IsLoading()
    {
        return _isLoading;
    }
    
    private void OnDestroy()
    {
        if (_tipRotationCoroutine != null)
        {
            StopCoroutine(_tipRotationCoroutine);
        }
    }
}