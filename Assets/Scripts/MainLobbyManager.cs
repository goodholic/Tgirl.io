using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainLobbyManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject _mainMenuPanel;
    [SerializeField] private GameObject _shopPanel;
    [SerializeField] private GameObject _characterSelectPanel;
    [SerializeField] private GameObject _settingsPanel;
    [SerializeField] private GameObject _mailboxPanel;
    [SerializeField] private GameObject _rankingPanel;
    
    [Header("Main Menu Buttons")]
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _shopButton;
    [SerializeField] private Button _characterButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _mailboxButton;
    [SerializeField] private Button _rankingButton;
    [SerializeField] private Button _exitButton;
    
    [Header("Player Info UI")]
    [SerializeField] private TextMeshProUGUI _playerNameText;
    [SerializeField] private TextMeshProUGUI _playerLevelText;
    [SerializeField] private TextMeshProUGUI _goldText;
    [SerializeField] private TextMeshProUGUI _cashText;
    [SerializeField] private TextMeshProUGUI _ticketText;
    [SerializeField] private Image _playerExpBar;
    [SerializeField] private TextMeshProUGUI _expText;
    
    [Header("Character Display")]
    [SerializeField] private GameObject _characterDisplayArea;
    [SerializeField] private GameObject[] _characterModels;
    [SerializeField] private float _characterRotationSpeed = 30f;
    
    [Header("Daily Rewards")]
    [SerializeField] private GameObject _dailyRewardButton;
    [SerializeField] private GameObject _dailyRewardPanel;
    [SerializeField] private Button[] _dailyRewardSlots;
    [SerializeField] private GameObject _dailyRewardClaimedEffect;
    
    [Header("News & Events")]
    [SerializeField] private GameObject _newsPanel;
    [SerializeField] private TextMeshProUGUI _newsText;
    [SerializeField] private Image _eventBanner;
    
    [Header("Matchmaking")]
    [SerializeField] private GameObject _matchmakingPanel;
    [SerializeField] private TextMeshProUGUI _matchmakingText;
    [SerializeField] private Button _cancelMatchmakingButton;
    [SerializeField] private Image _matchmakingProgressBar;
    
    [Header("Audio")]
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioClip _lobbyBGM;
    [SerializeField] private AudioClip _buttonClickSound;
    
    private int _currentCharacterIndex = 0;
    private GameObject _currentCharacterModel;
    private bool _isMatchmaking = false;
    private Coroutine _matchmakingCoroutine;
    private int _playerLevel = 1;
    private int _playerExp = 0;
    private int _playerCash = 0;
    private int _gachaTickets = 0;
    
    private void Awake()
    {
        // 버튼 이벤트 연결
        if (_playButton) _playButton.onClick.AddListener(OnPlayButtonClicked);
        if (_shopButton) _shopButton.onClick.AddListener(OnShopButtonClicked);
        if (_characterButton) _characterButton.onClick.AddListener(OnCharacterButtonClicked);
        if (_settingsButton) _settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        if (_mailboxButton) _mailboxButton.onClick.AddListener(OnMailboxButtonClicked);
        if (_rankingButton) _rankingButton.onClick.AddListener(OnRankingButtonClicked);
        if (_exitButton) _exitButton.onClick.AddListener(OnExitButtonClicked);
        if (_cancelMatchmakingButton) _cancelMatchmakingButton.onClick.AddListener(CancelMatchmaking);
        
        // 일일 보상 버튼
        if (_dailyRewardButton)
        {
            Button dailyButton = _dailyRewardButton.GetComponent<Button>();
            if (dailyButton) dailyButton.onClick.AddListener(OnDailyRewardButtonClicked);
        }
    }
    
    private void Start()
    {
        LoadPlayerData();
        UpdateUI();
        ShowMainMenu();
        DisplayCharacter(_currentCharacterIndex);
        CheckDailyReward();
        LoadNews();
        
        // 로비 BGM 재생
        if (_bgmSource && _lobbyBGM)
        {
            _bgmSource.clip = _lobbyBGM;
            _bgmSource.loop = true;
            _bgmSource.Play();
        }
    }
    
    private void Update()
    {
        // 캐릭터 모델 회전
        if (_currentCharacterModel != null)
        {
            _currentCharacterModel.transform.Rotate(Vector3.up * _characterRotationSpeed * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// 플레이어 데이터 로드
    /// </summary>
    private void LoadPlayerData()
    {
        // 업그레이드 매니저에서 골드 로드
        UpgradeManager.LoadData();
        
        // 플레이어 데이터 로드
        _playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
        _playerExp = PlayerPrefs.GetInt("PlayerExp", 0);
        _playerCash = PlayerPrefs.GetInt("PlayerCash", 0);
        _gachaTickets = PlayerPrefs.GetInt("GachaTickets", 0);
        _currentCharacterIndex = PlayerPrefs.GetInt("SelectedCharacter", 0);
    }
    
    /// <summary>
    /// UI 업데이트
    /// </summary>
    private void UpdateUI()
    {
        // 플레이어 정보
        if (_playerNameText) _playerNameText.text = PlayerPrefs.GetString("PlayerName", "Player");
        if (_playerLevelText) _playerLevelText.text = $"Lv.{_playerLevel}";
        if (_goldText) _goldText.text = UpgradeManager.Gold.ToString("N0");
        if (_cashText) _cashText.text = _playerCash.ToString("N0");
        if (_ticketText) _ticketText.text = _gachaTickets.ToString();
        
        // 경험치 바
        if (_playerExpBar)
        {
            int expForNextLevel = GetExpRequiredForLevel(_playerLevel + 1);
            float expProgress = (float)_playerExp / expForNextLevel;
            _playerExpBar.fillAmount = expProgress;
            
            if (_expText)
            {
                _expText.text = $"{_playerExp}/{expForNextLevel}";
            }
        }
    }
    
    /// <summary>
    /// 레벨업에 필요한 경험치 계산
    /// </summary>
    private int GetExpRequiredForLevel(int level)
    {
        // 레벨 * 100 + (레벨-1) * 50
        return level * 100 + (level - 1) * 50;
    }
    
    /// <summary>
    /// 메인 메뉴 표시
    /// </summary>
    private void ShowMainMenu()
    {
        _mainMenuPanel.SetActive(true);
        _shopPanel.SetActive(false);
        _characterSelectPanel.SetActive(false);
        _settingsPanel.SetActive(false);
        _mailboxPanel.SetActive(false);
        _rankingPanel.SetActive(false);
        _matchmakingPanel.SetActive(false);
    }
    
    /// <summary>
    /// 플레이 버튼 클릭
    /// </summary>
    private void OnPlayButtonClicked()
    {
        PlayButtonSound();
        StartMatchmaking();
    }
    
    /// <summary>
    /// 매칭 시작
    /// </summary>
    private void StartMatchmaking()
    {
        _isMatchmaking = true;
        _matchmakingPanel.SetActive(true);
        _mainMenuPanel.SetActive(false);
        
        if (_matchmakingCoroutine != null)
        {
            StopCoroutine(_matchmakingCoroutine);
        }
        
        _matchmakingCoroutine = StartCoroutine(MatchmakingRoutine());
    }
    
    /// <summary>
    /// 매칭 루틴
    /// </summary>
    private IEnumerator MatchmakingRoutine()
    {
        float matchmakingTime = 0f;
        float maxMatchmakingTime = Random.Range(3f, 8f); // 3-8초 사이 랜덤 매칭
        
        while (_isMatchmaking && matchmakingTime < maxMatchmakingTime)
        {
            matchmakingTime += Time.deltaTime;
            
            // 진행 바 업데이트
            if (_matchmakingProgressBar)
            {
                _matchmakingProgressBar.fillAmount = matchmakingTime / maxMatchmakingTime;
            }
            
            // 매칭 텍스트 업데이트
            if (_matchmakingText)
            {
                int dots = Mathf.FloorToInt(matchmakingTime) % 4;
                string dotsText = new string('.', dots);
                _matchmakingText.text = $"매칭 중{dotsText}\n{Mathf.FloorToInt(matchmakingTime)}초";
            }
            
            yield return null;
        }
        
        if (_isMatchmaking)
        {
            // 매칭 성공
            _matchmakingText.text = "매칭 완료!";
            yield return new WaitForSeconds(1f);
            
            // 게임 씬으로 이동
            SceneManager.LoadScene("GameScene");
        }
    }
    
    /// <summary>
    /// 매칭 취소
    /// </summary>
    private void CancelMatchmaking()
    {
        PlayButtonSound();
        _isMatchmaking = false;
        
        if (_matchmakingCoroutine != null)
        {
            StopCoroutine(_matchmakingCoroutine);
        }
        
        ShowMainMenu();
    }
    
    /// <summary>
    /// 상점 버튼 클릭
    /// </summary>
    private void OnShopButtonClicked()
    {
        PlayButtonSound();
        _mainMenuPanel.SetActive(false);
        _shopPanel.SetActive(true);
    }
    
    /// <summary>
    /// 캐릭터 선택 버튼 클릭
    /// </summary>
    private void OnCharacterButtonClicked()
    {
        PlayButtonSound();
        _mainMenuPanel.SetActive(false);
        _characterSelectPanel.SetActive(true);
    }
    
    /// <summary>
    /// 설정 버튼 클릭
    /// </summary>
    private void OnSettingsButtonClicked()
    {
        PlayButtonSound();
        _mainMenuPanel.SetActive(false);
        _settingsPanel.SetActive(true);
    }
    
    /// <summary>
    /// 우편함 버튼 클릭
    /// </summary>
    private void OnMailboxButtonClicked()
    {
        PlayButtonSound();
        _mainMenuPanel.SetActive(false);
        _mailboxPanel.SetActive(true);
    }
    
    /// <summary>
    /// 랭킹 버튼 클릭
    /// </summary>
    private void OnRankingButtonClicked()
    {
        PlayButtonSound();
        _mainMenuPanel.SetActive(false);
        _rankingPanel.SetActive(true);
    }
    
    /// <summary>
    /// 종료 버튼 클릭
    /// </summary>
    private void OnExitButtonClicked()
    {
        PlayButtonSound();
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    
    /// <summary>
    /// 캐릭터 표시
    /// </summary>
    private void DisplayCharacter(int index)
    {
        // 기존 캐릭터 제거
        if (_currentCharacterModel != null)
        {
            Destroy(_currentCharacterModel);
        }
        
        // 새 캐릭터 생성
        if (_characterModels != null && index < _characterModels.Length && _characterDisplayArea != null)
        {
            _currentCharacterModel = Instantiate(_characterModels[index], _characterDisplayArea.transform);
            _currentCharacterModel.transform.localPosition = Vector3.zero;
            _currentCharacterModel.transform.localRotation = Quaternion.identity;
            _currentCharacterModel.transform.localScale = Vector3.one;
        }
    }
    
    /// <summary>
    /// 캐릭터 선택
    /// </summary>
    public void SelectCharacter(int index)
    {
        _currentCharacterIndex = index;
        PlayerPrefs.SetInt("SelectedCharacter", index);
        PlayerPrefs.Save();
        
        DisplayCharacter(index);
        PlayButtonSound();
    }
    
    /// <summary>
    /// 일일 보상 확인
    /// </summary>
    private void CheckDailyReward()
    {
        string lastClaimDate = PlayerPrefs.GetString("LastDailyRewardDate", "");
        string today = System.DateTime.Now.ToString("yyyy-MM-dd");
        
        if (lastClaimDate != today)
        {
            // 일일 보상 가능
            if (_dailyRewardButton)
            {
                _dailyRewardButton.SetActive(true);
                
                // 반짝이는 효과
                StartCoroutine(PulseDailyRewardButton());
            }
        }
        else
        {
            // 이미 받음
            if (_dailyRewardButton)
            {
                _dailyRewardButton.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// 일일 보상 버튼 효과
    /// </summary>
    private IEnumerator PulseDailyRewardButton()
    {
        Transform buttonTransform = _dailyRewardButton.transform;
        
        while (_dailyRewardButton.activeSelf)
        {
            // 크기 변화
            buttonTransform.localScale = Vector3.one * 1.1f;
            yield return new WaitForSeconds(0.5f);
            buttonTransform.localScale = Vector3.one;
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    /// <summary>
    /// 일일 보상 버튼 클릭
    /// </summary>
    private void OnDailyRewardButtonClicked()
    {
        PlayButtonSound();
        _dailyRewardPanel.SetActive(true);
        
        // 일일 보상 지급
        int rewardGold = Random.Range(50, 200);
        int rewardTickets = Random.Range(1, 3);
        
        UpgradeManager.Gold += rewardGold;
        UpgradeManager.SaveData();
        
        _gachaTickets += rewardTickets;
        PlayerPrefs.SetInt("GachaTickets", _gachaTickets);
        
        // 날짜 저장
        PlayerPrefs.SetString("LastDailyRewardDate", System.DateTime.Now.ToString("yyyy-MM-dd"));
        PlayerPrefs.Save();
        
        // UI 업데이트
        UpdateUI();
        
        // 효과
        if (_dailyRewardClaimedEffect)
        {
            Instantiate(_dailyRewardClaimedEffect, _dailyRewardPanel.transform);
        }
        
        // 버튼 숨기기
        _dailyRewardButton.SetActive(false);
    }
    
    /// <summary>
    /// 뉴스 로드
    /// </summary>
    private void LoadNews()
    {
        if (_newsText)
        {
            _newsText.text = "🎮 새로운 캐릭터 '니케' 출시!\n⚔️ 주말 경험치 2배 이벤트 진행 중\n🏆 시즌 1 랭킹전 시작";
        }
    }
    
    /// <summary>
    /// 버튼 사운드 재생
    /// </summary>
    private void PlayButtonSound()
    {
        if (_bgmSource && _buttonClickSound)
        {
            _bgmSource.PlayOneShot(_buttonClickSound);
        }
    }
    
    /// <summary>
    /// 패널 닫기 (모든 서브 패널에서 사용)
    /// </summary>
    public void CloseCurrentPanel()
    {
        PlayButtonSound();
        ShowMainMenu();
    }
}