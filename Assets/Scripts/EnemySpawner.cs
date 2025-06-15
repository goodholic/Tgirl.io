using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    // 싱글톤 인스턴스 (전역 접근용)
    public static EnemySpawner Instance { get; private set; }

    [SerializeField] private float _spawnInterval = 3f;      // 몬스터 생성 간격 (초)
    [SerializeField] private int _spawnCount = 5;            // 한 번에 생성할 몬스터 수
    [SerializeField] private float _spawnRadius = 10f;         // 플레이어를 중심으로 한 최대 스폰 반지름
    [SerializeField] private float _minSpawnRadius = 3f;       // 플레이어와의 최소 스폰 거리
    [SerializeField] private GameObject[] _monsterPrefabs;     // 생성할 몬스터 프리팹 배열
    [SerializeField] private PlayerController _player;         // 스폰 기준 대상 (보통 플레이어)
    
    [Header("Spawn Ratio Settings")]
    [SerializeField] private float _rangedMonsterSpawnChance = 0.2f; // 원거리 몬스터 스폰 확률 (20%)
    [SerializeField] private int _minMonstersBeforeRanged = 3;       // 원거리 몬스터 스폰 전 최소 근거리 몬스터 수
    
    // 모든 플레이어 리스트 추가 (AI 포함)
    private List<PlayerController> _allPlayers = new List<PlayerController>();
    private int _meleeMonsterCount = 0; // 근거리 몬스터 스폰 카운트
    
    public PlayerController Player => _player;
    public List<PlayerController> AllPlayers => _allPlayers;

    private void Awake()
    {
        // 싱글톤 패턴 구현: 중복 인스턴스가 있으면 제거
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // _player가 할당되지 않았다면 태그 "Player"를 가진 객체에서 PlayerController를 검색
        if (_player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                _player = playerObj.GetComponent<PlayerController>();
            }
        }
    }

    private void Start()
    {
        // 모든 플레이어 찾기 (AI 포함)
        RefreshPlayerList();
        StartCoroutine(SpawnCoroutine());
        
        // 주기적으로 플레이어 리스트 갱신
        StartCoroutine(RefreshPlayerListRoutine());
    }

    /// <summary>
    /// 주기적으로 플레이어 리스트를 갱신하는 코루틴
    /// </summary>
    private IEnumerator RefreshPlayerListRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);
            RefreshPlayerList();
        }
    }

    /// <summary>
    /// 주기적으로 몬스터를 생성하는 코루틴
    /// </summary>
    private IEnumerator SpawnCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(_spawnInterval);
            SpawnMonsters();
        }
    }

    /// <summary>
    /// 스폰 기준 대상을 갱신합니다.
    /// </summary>
    /// <param name="playerController">새로운 플레이어 컨트롤러</param>
    public void SetPlayer(PlayerController playerController)
    {
        _player = playerController;
    }

    /// <summary>
    /// 플레이어 리스트를 새로고침합니다.
    /// </summary>
    public void RefreshPlayerList()
    {
        _allPlayers.Clear();
        
        // 모든 Player 태그를 가진 오브젝트 찾기
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject playerObj in playerObjects)
        {
            PlayerController pc = playerObj.GetComponent<PlayerController>();
            if (pc != null && !pc.isDead)
            {
                _allPlayers.Add(pc);
            }
        }
    }

    /// <summary>
    /// 플레이어를 리스트에 추가합니다.
    /// </summary>
    public void RegisterPlayer(PlayerController player)
    {
        if (!_allPlayers.Contains(player))
        {
            _allPlayers.Add(player);
        }
    }

    /// <summary>
    /// 플레이어를 리스트에서 제거합니다.
    /// </summary>
    public void UnregisterPlayer(PlayerController player)
    {
        if (_allPlayers.Contains(player))
        {
            _allPlayers.Remove(player);
        }
    }

    /// <summary>
    /// 가장 가까운 살아있는 플레이어를 반환합니다.
    /// </summary>
    public PlayerController GetNearestAlivePlayer(Vector3 position)
    {
        PlayerController nearest = null;
        float minDistance = float.MaxValue;

        foreach (PlayerController player in _allPlayers)
        {
            if (player != null && !player.isDead)
            {
                float distance = Vector3.Distance(position, player.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = player;
                }
            }
        }

        // 리스트에서 못 찾으면 메인 플레이어 반환
        if (nearest == null && _player != null && !_player.isDead)
        {
            nearest = _player;
        }

        return nearest;
    }

    /// <summary>
    /// 플레이어 주변에 몬스터들을 생성합니다.
    /// </summary>
    private void SpawnMonsters()
    {
        if (_monsterPrefabs == null || _monsterPrefabs.Length == 0)
            return;

        // 살아있는 플레이어가 없으면 스폰 안함
        RefreshPlayerList();
        if (_allPlayers.Count == 0 && (_player == null || _player.isDead))
            return;

        // 각 플레이어 주변에 몬스터 스폰
        List<PlayerController> playersToSpawn = new List<PlayerController>(_allPlayers);
        
        // 메인 플레이어도 포함
        if (_player != null && !_player.isDead && !playersToSpawn.Contains(_player))
        {
            playersToSpawn.Add(_player);
        }

        // 각 플레이어당 스폰할 몬스터 수 계산
        int monstersPerPlayer = Mathf.Max(1, _spawnCount / Mathf.Max(1, playersToSpawn.Count));

        foreach (PlayerController targetPlayer in playersToSpawn)
        {
            if (targetPlayer == null || targetPlayer.isDead)
                continue;

            for (int i = 0; i < monstersPerPlayer; i++)
            {
                GameObject monsterPrefab;
                
                // 원거리 몬스터 스폰 조건 체크
                bool shouldSpawnRanged = false;
                
                // 1. 최소 근거리 몬스터 수를 충족했는지 확인
                if (_meleeMonsterCount >= _minMonstersBeforeRanged && _monsterPrefabs.Length > 1)
                {
                    // 2. 확률 체크
                    if (Random.value < _rangedMonsterSpawnChance)
                    {
                        shouldSpawnRanged = true;
                    }
                }
                
                // 몬스터 프리팹 선택
                if (shouldSpawnRanged)
                {
                    monsterPrefab = _monsterPrefabs[1]; // 원거리 몬스터
                    _meleeMonsterCount = 0; // 카운터 리셋
                }
                else
                {
                    monsterPrefab = _monsterPrefabs[0]; // 근거리 몬스터
                    _meleeMonsterCount++;
                }

                // 플레이어 주변에서 최소 ~ 최대 스폰 반지름 내의 임의의 위치 계산
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float distance = Random.Range(_minSpawnRadius, _spawnRadius);
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
                Vector3 spawnPos = targetPlayer.transform.position + offset;

                // NavMesh 상의 유효한 위치를 찾은 경우 해당 위치에 몬스터 생성
                if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, _spawnRadius, NavMesh.AllAreas))
                {
                    Instantiate(monsterPrefab, hit.position, Quaternion.identity);
                }
            }
        }
    }
}