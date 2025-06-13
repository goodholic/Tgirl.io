using System.Collections;
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
    
    public PlayerController Player => _player;

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
        StartCoroutine(SpawnCoroutine());
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
    /// 플레이어 주변에 몬스터들을 생성합니다.
    /// </summary>
    private void SpawnMonsters()
    {
        if (_monsterPrefabs == null || _monsterPrefabs.Length == 0 || _player == null)
            return;

        for (int i = 0; i < _spawnCount; i++)
        {
            // 몬스터 프리팹 선택
            GameObject monsterPrefab = _monsterPrefabs[0];
            
            if (i == _spawnCount - 1)
            {
                monsterPrefab = _monsterPrefabs[1];
            }

            // 플레이어 주변에서 최소 ~ 최대 스폰 반지름 내의 임의의 위치 계산
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(_minSpawnRadius, _spawnRadius);
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
            Vector3 spawnPos = _player.transform.position + offset;

            // NavMesh 상의 유효한 위치를 찾은 경우 해당 위치에 몬스터 생성
            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, _spawnRadius, NavMesh.AllAreas))
            {
                Instantiate(monsterPrefab, hit.position, Quaternion.identity);
            }
        }
    }
}
