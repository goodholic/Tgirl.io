using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game Settings")]
    [SerializeField] private float _gameDuration = 300f; // 5분 = 300초
    [SerializeField] private GameObject _crownPrefab;
    [SerializeField] private Transform _crownSpawnPoint;
    [SerializeField] private float _crownSpawnDelay = 30f; // 게임 시작 30초 후 왕관 스폰
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private TextMeshProUGUI _zombieKillText;
    [SerializeField] private TextMeshProUGUI _playerKillText;
    [SerializeField] private TextMeshProUGUI _crownHolderText;
    [SerializeField] private GameObject _gameEndPanel;
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private TextMeshProUGUI _rankingText;
    
    [Header("Result Screen UI")]
    [SerializeField] private GameObject _resultPanel;
    [SerializeField] private TextMeshProUGUI _rankText;
    [SerializeField] private TextMeshProUGUI _playerNameText;
    [SerializeField] private TextMeshProUGUI _killCountText;
    [SerializeField] private TextMeshProUGUI _zombieCountText;
    [SerializeField] private TextMeshProUGUI _crownTimeText;
    [SerializeField] private TextMeshProUGUI _goldRewardText;
    [SerializeField] private TextMeshProUGUI _expRewardText;
    [SerializeField] private Button _watchAdButton;
    [SerializeField] private Button _continueButton;
    [SerializeField] private GameObject _adRewardPanel;
    [SerializeField] private TextMeshProUGUI _adRewardText;
    
    [Header("Reward Settings")]
    [SerializeField] private int[] _rankGoldRewards = { 100, 70, 50, 30, 20, 10 }; // 순위별 골드 보상
    [SerializeField] private int[] _rankExpRewards = { 50, 35, 25, 15, 10, 5 }; // 순위별 경험치 보상
    [SerializeField] private int _killGoldReward = 5; // 킬당 추가 골드
    [SerializeField] private int _zombieGoldReward = 2; // 좀비 킬당 추가 골드
    [SerializeField] private float _adRewardMultiplier = 2f; // 광고 시청 시 보상 배수
    
    [Header("Player Stats")]
    private int _zombieKills = 0;
    private int _playerKills = 0;
    private PlayerController _currentCrownHolder = null;
    private float _gameTimer;
    private bool _isGameActive = false;
    private GameObject _crownInstance;
    
    // 플레이어별 점수 추적
    private Dictionary<PlayerController, PlayerStats> _playerStats = new Dictionary<PlayerController, PlayerStats>();
    
    // 게임 결과 저장
    private List<KeyValuePair<PlayerController, PlayerStats>> _finalRankings;
    private int _currentPlayerRank = -1;
    private int _baseGoldReward = 0;
    private int _baseExpReward = 0;
    
    [System.Serializable]
    public class PlayerStats
    {
        public int zombieKills = 0;
        public int playerKills = 0;
        public int bounty = 0; // 현상금
        public bool hasCrown = false;
        public float crownHoldTime = 0f;
        public string playerName = "Player";
    }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 버튼 이벤트 연결
        if (_watchAdButton != null)
        {
            _watchAdButton.onClick.AddListener(OnWatchAdButtonClicked);
        }
        
        if (_continueButton != null)
        {
            _continueButton.onClick.AddListener(OnContinueButtonClicked);
        }
    }
    
    private void Start()
    {
        StartGame();
    }
    
    private void StartGame()
    {
        _isGameActive = true;
        _gameTimer = _gameDuration;
        
        // 왕관 스폰 코루틴 시작
        StartCoroutine(SpawnCrownAfterDelay());
        
        // 게임 타이머 업데이트
        StartCoroutine(GameTimerRoutine());
        
        UpdateUI();
    }
    
    private IEnumerator SpawnCrownAfterDelay()
    {
        yield return new WaitForSeconds(_crownSpawnDelay);
        
        if (_crownPrefab != null && _crownSpawnPoint != null)
        {
            _crownInstance = Instantiate(_crownPrefab, _crownSpawnPoint.position, Quaternion.identity);
            Crown crown = _crownInstance.GetComponent<Crown>();
            if (crown != null)
            {
                crown.OnCrownPickup += HandleCrownPickup;
            }
        }
    }
    
    private IEnumerator GameTimerRoutine()
    {
        while (_isGameActive && _gameTimer > 0)
        {
            _gameTimer -= Time.deltaTime;
            UpdateTimerUI();
            
            // 왕관 보유 시간 업데이트
            if (_currentCrownHolder != null)
            {
                if (_playerStats.ContainsKey(_currentCrownHolder))
                {
                    _playerStats[_currentCrownHolder].crownHoldTime += Time.deltaTime;
                }
            }
            
            yield return null;
        }
        
        EndGame();
    }
    
    public void RegisterPlayer(PlayerController player, string playerName)
    {
        if (!_playerStats.ContainsKey(player))
        {
            PlayerStats stats = new PlayerStats();
            stats.playerName = playerName;
            _playerStats[player] = stats;
        }
    }
    
    public void AddZombieKill(PlayerController killer)
    {
        if (killer == PlayerManager.Instance.curPlayer)
        {
            _zombieKills++;
        }
        
        if (_playerStats.ContainsKey(killer))
        {
            _playerStats[killer].zombieKills++;
        }
        
        UpdateUI();
    }
    
    public void AddPlayerKill(PlayerController killer, PlayerController victim)
    {
        if (killer == PlayerManager.Instance.curPlayer)
        {
            _playerKills++;
        }
        
        if (_playerStats.ContainsKey(killer))
        {
            _playerStats[killer].playerKills++;
            
            // 현상금 시스템
            _playerStats[killer].bounty += 10; // 킬당 현상금 10 증가
            
            // 피해자가 현상금을 가지고 있었다면 추가 보상
            if (_playerStats.ContainsKey(victim) && _playerStats[victim].bounty > 0)
            {
                int bountyReward = _playerStats[victim].bounty;
                UpgradeManager.Gold += bountyReward;
                UpgradeManager.SaveData();
                
                // 현상금 초기화
                _playerStats[victim].bounty = 0;
                
                ShowBountyMessage($"{_playerStats[killer].playerName}이(가) 현상금 {bountyReward} 획득!");
            }
            
            // 점수 빼앗기 시스템 (피해자 점수의 절반을 빼앗음)
            if (_playerStats.ContainsKey(victim))
            {
                int stolenZombieKills = _playerStats[victim].zombieKills / 2;
                int stolenPlayerKills = _playerStats[victim].playerKills / 2;
                
                _playerStats[victim].zombieKills -= stolenZombieKills;
                _playerStats[victim].playerKills -= stolenPlayerKills;
                
                _playerStats[killer].zombieKills += stolenZombieKills;
                _playerStats[killer].playerKills += stolenPlayerKills;
                
                // 왕관 빼앗기
                if (_playerStats[victim].hasCrown)
                {
                    TransferCrown(victim, killer);
                }
            }
        }
        
        UpdateUI();
    }
    
    private void HandleCrownPickup(PlayerController player)
    {
        if (_currentCrownHolder != null && _playerStats.ContainsKey(_currentCrownHolder))
        {
            _playerStats[_currentCrownHolder].hasCrown = false;
        }
        
        _currentCrownHolder = player;
        if (_playerStats.ContainsKey(player))
        {
            _playerStats[player].hasCrown = true;
        }
        
        UpdateCrownUI();
        ShowCrownMessage($"{GetPlayerName(player)}이(가) 왕관을 획득했습니다!");
    }
    
    private void TransferCrown(PlayerController from, PlayerController to)
    {
        if (_playerStats.ContainsKey(from))
        {
            _playerStats[from].hasCrown = false;
        }
        
        if (_playerStats.ContainsKey(to))
        {
            _playerStats[to].hasCrown = true;
        }
        
        _currentCrownHolder = to;
        
        // 왕관 오브젝트를 새로운 소유자에게 이동
        if (_crownInstance != null)
        {
            Crown crown = _crownInstance.GetComponent<Crown>();
            if (crown != null)
            {
                crown.TransferToPlayer(to);
            }
        }
        
        UpdateCrownUI();
        ShowCrownMessage($"{GetPlayerName(to)}이(가) {GetPlayerName(from)}에게서 왕관을 빼앗았습니다!");
    }
    
    private string GetPlayerName(PlayerController player)
    {
        if (_playerStats.ContainsKey(player))
        {
            return _playerStats[player].playerName;
        }
        return "Unknown";
    }
    
    private void UpdateUI()
    {
        if (_zombieKillText != null)
            _zombieKillText.text = $"좀비: {_zombieKills}";
            
        if (_playerKillText != null)
            _playerKillText.text = $"킬: {_playerKills}";
    }
    
    private void UpdateTimerUI()
    {
        if (_timerText != null)
        {
            int minutes = Mathf.FloorToInt(_gameTimer / 60);
            int seconds = Mathf.FloorToInt(_gameTimer % 60);
            _timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }
    
    private void UpdateCrownUI()
    {
        if (_crownHolderText != null)
        {
            if (_currentCrownHolder != null)
            {
                _crownHolderText.text = $"왕관: {GetPlayerName(_currentCrownHolder)}";
            }
            else
            {
                _crownHolderText.text = "왕관: 없음";
            }
        }
    }
    
    private void ShowBountyMessage(string message)
    {
        // 화면에 현상금 메시지 표시 (추후 구현)
        Debug.Log(message);
    }
    
    private void ShowCrownMessage(string message)
    {
        // 화면에 왕관 메시지 표시 (추후 구현)
        Debug.Log(message);
    }
    
    private void EndGame()
    {
        _isGameActive = false;
        Time.timeScale = 0f;
        
        // 순위 계산
        _finalRankings = new List<KeyValuePair<PlayerController, PlayerStats>>(_playerStats);
        
        // 1등: 마지막까지 왕관을 가진 자
        // 2등: 킬 수가 가장 많은 자
        // 3등: 좀비 처치를 가장 많이 한 자
        _finalRankings.Sort((a, b) =>
        {
            // 왕관 보유자가 1등
            if (a.Value.hasCrown && !b.Value.hasCrown) return -1;
            if (!a.Value.hasCrown && b.Value.hasCrown) return 1;
            
            // 왕관이 없으면 플레이어 킬 수로 정렬
            if (a.Value.playerKills != b.Value.playerKills)
                return b.Value.playerKills.CompareTo(a.Value.playerKills);
            
            // 플레이어 킬 수가 같으면 좀비 킬 수로 정렬
            return b.Value.zombieKills.CompareTo(a.Value.zombieKills);
        });
        
        ShowGameResults();
    }
    
    private void ShowGameResults()
    {
        if (_resultPanel != null)
        {
            _resultPanel.SetActive(true);
        }
        
        // 현재 플레이어의 순위 찾기
        PlayerController currentPlayer = PlayerManager.Instance.curPlayer;
        for (int i = 0; i < _finalRankings.Count; i++)
        {
            if (_finalRankings[i].Key == currentPlayer)
            {
                _currentPlayerRank = i + 1;
                break;
            }
        }
        
        // 순위 표시
        if (_rankText != null)
        {
            string rankEmoji = "";
            switch (_currentPlayerRank)
            {
                case 1: rankEmoji = "👑"; break;
                case 2: rankEmoji = "🥈"; break;
                case 3: rankEmoji = "🥉"; break;
                default: rankEmoji = ""; break;
            }
            _rankText.text = $"{rankEmoji} {_currentPlayerRank}등";
        }
        
        // 플레이어 정보 표시
        if (_playerStats.ContainsKey(currentPlayer))
        {
            PlayerStats stats = _playerStats[currentPlayer];
            
            if (_playerNameText != null)
                _playerNameText.text = stats.playerName;
            
            if (_killCountText != null)
                _killCountText.text = $"플레이어 킬: {stats.playerKills}";
            
            if (_zombieCountText != null)
                _zombieCountText.text = $"좀비 킬: {stats.zombieKills}";
            
            if (_crownTimeText != null)
            {
                int minutes = Mathf.FloorToInt(stats.crownHoldTime / 60);
                int seconds = Mathf.FloorToInt(stats.crownHoldTime % 60);
                _crownTimeText.text = $"왕관 보유 시간: {minutes:00}:{seconds:00}";
            }
            
            // 보상 계산
            CalculateRewards(stats);
        }
        
        // 전체 순위 표시
        if (_rankingText != null)
        {
            string rankingStr = "=== 전체 순위 ===\n\n";
            
            for (int i = 0; i < _finalRankings.Count && i < 6; i++)
            {
                var player = _finalRankings[i];
                string rank = "";
                
                if (i == 0 && player.Value.hasCrown)
                    rank = "👑 1등";
                else if (i == 0)
                    rank = "🥇 1등";
                else if (i == 1)
                    rank = "🥈 2등";
                else if (i == 2)
                    rank = "🥉 3등";
                else
                    rank = $"{i + 1}등";
                
                rankingStr += $"{rank} - {player.Value.playerName}\n";
                rankingStr += $"킬: {player.Value.playerKills} | 좀비: {player.Value.zombieKills}\n";
                
                if (player.Value.hasCrown)
                {
                    rankingStr += $"왕관 보유 시간: {player.Value.crownHoldTime:F1}초\n";
                }
                
                rankingStr += "\n";
            }
            
            _rankingText.text = rankingStr;
        }
    }
    
    private void CalculateRewards(PlayerStats stats)
    {
        // 기본 순위 보상
        if (_currentPlayerRank > 0 && _currentPlayerRank <= _rankGoldRewards.Length)
        {
            _baseGoldReward = _rankGoldRewards[_currentPlayerRank - 1];
            _baseExpReward = _rankExpRewards[_currentPlayerRank - 1];
        }
        
        // 추가 보상 계산
        _baseGoldReward += stats.playerKills * _killGoldReward;
        _baseGoldReward += stats.zombieKills * _zombieGoldReward;
        
        // 왕관 보유 시간 보너스
        if (stats.hasCrown)
        {
            _baseGoldReward += Mathf.FloorToInt(stats.crownHoldTime / 10f) * 5; // 10초마다 5골드
        }
        
        // 보상 표시
        if (_goldRewardText != null)
            _goldRewardText.text = $"골드: +{_baseGoldReward}";
        
        if (_expRewardText != null)
            _expRewardText.text = $"경험치: +{_baseExpReward}";
        
        // 광고 버튼 텍스트 업데이트
        if (_watchAdButton != null)
        {
            TextMeshProUGUI buttonText = _watchAdButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = $"광고 시청하고 {_adRewardMultiplier}배 보상 받기";
            }
        }
    }
    
    private void OnWatchAdButtonClicked()
    {
        // 광고 시청
        AdMobManager.Instance.ShowRewardedInterstitialAd(() =>
        {
            // 광고 시청 성공 시 보상 지급
            int finalGoldReward = Mathf.FloorToInt(_baseGoldReward * _adRewardMultiplier);
            int finalExpReward = Mathf.FloorToInt(_baseExpReward * _adRewardMultiplier);
            
            UpgradeManager.Gold += finalGoldReward;
            UpgradeManager.SaveData();
            
            // 경험치도 저장 (경험치 시스템이 있다면)
            // PlayerPrefs.SetInt("PlayerExp", PlayerPrefs.GetInt("PlayerExp", 0) + finalExpReward);
            // PlayerPrefs.Save();
            
            // 광고 보상 패널 표시
            if (_adRewardPanel != null)
            {
                _adRewardPanel.SetActive(true);
                if (_adRewardText != null)
                {
                    _adRewardText.text = $"광고 보상!\n골드 +{finalGoldReward}\n경험치 +{finalExpReward}";
                }
            }
            
            // 버튼 비활성화
            _watchAdButton.interactable = false;
            
            // UI 업데이트
            UIManager.Instance.RefreshGoldUI();
        });
    }
    
    private void OnContinueButtonClicked()
    {
        // 광고를 보지 않은 경우 기본 보상 지급
        if (_watchAdButton.interactable)
        {
            UpgradeManager.Gold += _baseGoldReward;
            UpgradeManager.SaveData();
            
            // 경험치도 저장 (경험치 시스템이 있다면)
            // PlayerPrefs.SetInt("PlayerExp", PlayerPrefs.GetInt("PlayerExp", 0) + _baseExpReward);
            // PlayerPrefs.Save();
            
            UIManager.Instance.RefreshGoldUI();
        }
        
        // 메인 로비로 이동
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainLobby");
    }
    
    public PlayerStats GetPlayerStats(PlayerController player)
    {
        if (_playerStats.ContainsKey(player))
        {
            return _playerStats[player];
        }
        return null;
    }
}