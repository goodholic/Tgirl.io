using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    
    [Header("Player Stats")]
    private int _zombieKills = 0;
    private int _playerKills = 0;
    private PlayerController _currentCrownHolder = null;
    private float _gameTimer;
    private bool _isGameActive = false;
    private GameObject _crownInstance;
    
    // 플레이어별 점수 추적
    private Dictionary<PlayerController, PlayerStats> _playerStats = new Dictionary<PlayerController, PlayerStats>();
    
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
        List<KeyValuePair<PlayerController, PlayerStats>> rankings = new List<KeyValuePair<PlayerController, PlayerStats>>(_playerStats);
        
        // 1등: 마지막까지 왕관을 가진 자
        // 2등: 킬 수가 가장 많은 자
        // 3등: 좀비 처치를 가장 많이 한 자
        rankings.Sort((a, b) =>
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
        
        ShowGameResults(rankings);
    }
    
    private void ShowGameResults(List<KeyValuePair<PlayerController, PlayerStats>> rankings)
    {
        if (_gameEndPanel != null)
        {
            _gameEndPanel.SetActive(true);
        }
        
        if (_rankingText != null)
        {
            string rankingStr = "=== 게임 결과 ===\n\n";
            
            for (int i = 0; i < rankings.Count && i < 3; i++)
            {
                var player = rankings[i];
                string rank = "";
                
                if (i == 0 && player.Value.hasCrown)
                    rank = "👑 1등 (왕관 보유자)";
                else if (i == 1 || (i == 0 && !player.Value.hasCrown))
                    rank = "🥈 2등 (최다 킬)";
                else
                    rank = "🥉 3등 (최다 좀비 처치)";
                
                rankingStr += $"{rank}\n";
                rankingStr += $"{player.Value.playerName}\n";
                rankingStr += $"플레이어 킬: {player.Value.playerKills} | 좀비 킬: {player.Value.zombieKills}\n";
                
                if (player.Value.hasCrown)
                {
                    rankingStr += $"왕관 보유 시간: {player.Value.crownHoldTime:F1}초\n";
                }
                
                rankingStr += "\n";
            }
            
            _rankingText.text = rankingStr;
        }
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