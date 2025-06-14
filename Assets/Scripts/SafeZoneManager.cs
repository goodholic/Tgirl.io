using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SafeZoneManager : MonoBehaviour
{
    public static SafeZoneManager Instance { get; private set; }
    
    [Header("Safe Zone Settings")]
    [SerializeField] private float _initialRadius = 50f;
    [SerializeField] private float _minimumRadius = 5f;
    [SerializeField] private float _shrinkDelay = 60f; // 첫 수축까지 대기 시간
    [SerializeField] private float _shrinkDuration = 30f; // 수축에 걸리는 시간
    [SerializeField] private float _pauseDuration = 30f; // 수축 후 대기 시간
    [SerializeField] private int _totalPhases = 3; // 총 수축 단계
    
    [Header("Damage Settings")]
    [SerializeField] private float _damagePerSecond = 5f;
    [SerializeField] private float _damageIncreasePerPhase = 2f; // 단계별 데미지 증가량
    
    [Header("Visual Settings")]
    [SerializeField] private GameObject _safeZonePrefab; // 안전 구역 시각화 프리팹
    [SerializeField] private GameObject _warningZonePrefab; // 다음 안전 구역 표시
    [SerializeField] private Material _safeZoneMaterial;
    [SerializeField] private Material _dangerZoneMaterial;
    [SerializeField] private GameObject _poisonFogPrefab; // 독 안개 이펙트
    
    [Header("UI References")]
    [SerializeField] private GameObject _safeZoneUI;
    [SerializeField] private TextMeshProUGUI _phaseText;
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private TextMeshProUGUI _warningText;
    [SerializeField] private Image _miniMapSafeZone;
    [SerializeField] private GameObject _damageIndicator; // 데미지 받을 때 화면 효과
    
    [Header("Audio")]
    [SerializeField] private AudioClip _warningSound;
    [SerializeField] private AudioClip _shrinkingSound;
    [SerializeField] private AudioSource _audioSource;
    
    private GameObject _currentSafeZone;
    private GameObject _nextSafeZone;
    private Vector3 _currentCenter;
    private Vector3 _nextCenter;
    private float _currentRadius;
    private float _nextRadius;
    private int _currentPhase = 0;
    private bool _isShrinking = false;
    private float _currentDamagePerSecond;
    private Coroutine _safeZoneCoroutine;
    private List<PlayerController> _playersInDanger = new List<PlayerController>();
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }
    }
    
    private void Start()
    {
        InitializeSafeZone();
        _safeZoneCoroutine = StartCoroutine(SafeZoneRoutine());
    }
    
    /// <summary>
    /// 안전 구역 초기화
    /// </summary>
    private void InitializeSafeZone()
    {
        _currentCenter = Vector3.zero; // 맵 중앙에서 시작
        _currentRadius = _initialRadius;
        _currentDamagePerSecond = _damagePerSecond;
        
        // 현재 안전 구역 생성
        if (_safeZonePrefab != null)
        {
            _currentSafeZone = Instantiate(_safeZonePrefab, _currentCenter, Quaternion.identity);
            UpdateSafeZoneVisual(_currentSafeZone, _currentRadius);
        }
        
        UpdateUI();
    }
    
    /// <summary>
    /// 안전 구역 루틴
    /// </summary>
    private IEnumerator SafeZoneRoutine()
    {
        // 초기 대기
        yield return StartCoroutine(WaitPhase(_shrinkDelay, "첫 안전 구역 수축까지"));
        
        for (int phase = 0; phase < _totalPhases; phase++)
        {
            _currentPhase = phase + 1;
            
            // 다음 안전 구역 계산
            CalculateNextSafeZone();
            
            // 경고
            ShowWarning();
            yield return new WaitForSeconds(10f);
            
            // 수축 시작
            yield return StartCoroutine(ShrinkSafeZone());
            
            // 데미지 증가
            _currentDamagePerSecond = _damagePerSecond + (_damageIncreasePerPhase * phase);
            
            // 대기
            if (phase < _totalPhases - 1)
            {
                yield return StartCoroutine(WaitPhase(_pauseDuration, "다음 수축까지"));
            }
        }
        
        // 최종 단계
        ShowFinalWarning();
    }
    
    /// <summary>
    /// 대기 단계
    /// </summary>
    private IEnumerator WaitPhase(float duration, string message)
    {
        float timer = duration;
        
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            
            if (_timerText != null)
            {
                _timerText.text = $"{message}: {Mathf.CeilToInt(timer)}초";
            }
            
            yield return null;
        }
    }
    
    /// <summary>
    /// 다음 안전 구역 계산
    /// </summary>
    private void CalculateNextSafeZone()
    {
        // 반경 감소
        _nextRadius = Mathf.Max(_minimumRadius, _currentRadius * 0.6f);
        
        // 새로운 중심점 계산 (현재 구역 내의 랜덤 위치)
        float maxOffset = _currentRadius - _nextRadius;
        Vector2 randomOffset = Random.insideUnitCircle * maxOffset * 0.5f;
        _nextCenter = _currentCenter + new Vector3(randomOffset.x, 0, randomOffset.y);
        
        // 다음 안전 구역 표시
        if (_warningZonePrefab != null)
        {
            if (_nextSafeZone != null)
            {
                Destroy(_nextSafeZone);
            }
            
            _nextSafeZone = Instantiate(_warningZonePrefab, _nextCenter, Quaternion.identity);
            UpdateSafeZoneVisual(_nextSafeZone, _nextRadius);
        }
    }
    
    /// <summary>
    /// 경고 표시
    /// </summary>
    private void ShowWarning()
    {
        if (_warningText != null)
        {
            _warningText.gameObject.SetActive(true);
            _warningText.text = "안전 구역이 곧 수축됩니다!";
        }
        
        if (_warningSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_warningSound);
        }
        
        // 미니맵에 경고 표시
        if (_miniMapSafeZone != null)
        {
            StartCoroutine(FlashMiniMapWarning());
        }
    }
    
    /// <summary>
    /// 미니맵 경고 깜빡임
    /// </summary>
    private IEnumerator FlashMiniMapWarning()
    {
        for (int i = 0; i < 5; i++)
        {
            _miniMapSafeZone.color = Color.red;
            yield return new WaitForSeconds(0.5f);
            _miniMapSafeZone.color = Color.white;
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    /// <summary>
    /// 안전 구역 수축
    /// </summary>
    private IEnumerator ShrinkSafeZone()
    {
        _isShrinking = true;
        
        if (_shrinkingSound != null && _audioSource != null)
        {
            _audioSource.clip = _shrinkingSound;
            _audioSource.Play();
        }
        
        if (_warningText != null)
        {
            _warningText.text = "안전 구역 수축 중!";
        }
        
        float elapsed = 0f;
        Vector3 startCenter = _currentCenter;
        float startRadius = _currentRadius;
        
        // 독 안개 생성
        if (_poisonFogPrefab != null)
        {
            CreatePoisonFog();
        }
        
        while (elapsed < _shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _shrinkDuration;
            
            // 부드러운 수축
            _currentCenter = Vector3.Lerp(startCenter, _nextCenter, t);
            _currentRadius = Mathf.Lerp(startRadius, _nextRadius, t);
            
            // 시각적 업데이트
            if (_currentSafeZone != null)
            {
                _currentSafeZone.transform.position = _currentCenter;
                UpdateSafeZoneVisual(_currentSafeZone, _currentRadius);
            }
            
            // UI 업데이트
            if (_timerText != null)
            {
                _timerText.text = $"수축 중: {Mathf.CeilToInt(_shrinkDuration - elapsed)}초";
            }
            
            yield return null;
        }
        
        // 수축 완료
        _currentCenter = _nextCenter;
        _currentRadius = _nextRadius;
        
        if (_nextSafeZone != null)
        {
            Destroy(_nextSafeZone);
        }
        
        if (_warningText != null)
        {
            _warningText.gameObject.SetActive(false);
        }
        
        _isShrinking = false;
        UpdateUI();
    }
    
    /// <summary>
    /// 독 안개 생성
    /// </summary>
    private void CreatePoisonFog()
    {
        // 맵 전체에 독 안개 효과 생성
        // 실제로는 안전 구역 외부에만 생성하도록 최적화 필요
        if (_poisonFogPrefab != null)
        {
            GameObject fog = Instantiate(_poisonFogPrefab, Vector3.zero, Quaternion.identity);
            fog.transform.localScale = new Vector3(100f, 10f, 100f); // 맵 크기에 맞게 조정
        }
    }
    
    /// <summary>
    /// 안전 구역 시각화 업데이트
    /// </summary>
    private void UpdateSafeZoneVisual(GameObject zone, float radius)
    {
        if (zone == null) return;
        
        // 크기 조정
        zone.transform.localScale = new Vector3(radius * 2f, 0.1f, radius * 2f);
        
        // 머티리얼 업데이트
        Renderer renderer = zone.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = _isShrinking ? _dangerZoneMaterial : _safeZoneMaterial;
        }
    }
    
    /// <summary>
    /// UI 업데이트
    /// </summary>
    private void UpdateUI()
    {
        if (_phaseText != null)
        {
            _phaseText.text = $"단계 {_currentPhase}/{_totalPhases}";
        }
        
        if (_safeZoneUI != null)
        {
            _safeZoneUI.SetActive(true);
        }
    }
    
    /// <summary>
    /// 최종 경고
    /// </summary>
    private void ShowFinalWarning()
    {
        if (_warningText != null)
        {
            _warningText.gameObject.SetActive(true);
            _warningText.text = "최종 안전 구역! 중앙에서 싸우세요!";
            _warningText.color = Color.red;
        }
    }
    
    /// <summary>
    /// 플레이어가 안전 구역 내에 있는지 확인
    /// </summary>
    public bool IsPlayerInSafeZone(Vector3 playerPosition)
    {
        float distance = Vector3.Distance(new Vector3(playerPosition.x, 0, playerPosition.z), 
                                        new Vector3(_currentCenter.x, 0, _currentCenter.z));
        return distance <= _currentRadius;
    }
    
    private void Update()
    {
        // 모든 플레이어의 안전 구역 체크
        CheckPlayersInSafeZone();
    }
    
    /// <summary>
    /// 플레이어들의 안전 구역 체크 및 데미지 적용
    /// </summary>
    private void CheckPlayersInSafeZone()
    {
        // 현재 플레이어만 체크 (멀티플레이 시 모든 플레이어 체크 필요)
        if (PlayerManager.Instance != null && PlayerManager.Instance.curPlayer != null)
        {
            PlayerController player = PlayerManager.Instance.curPlayer;
            bool isInSafeZone = IsPlayerInSafeZone(player.transform.position);
            
            if (!isInSafeZone)
            {
                // 안전 구역 밖에 있으면 데미지
                if (!_playersInDanger.Contains(player))
                {
                    _playersInDanger.Add(player);
                    StartCoroutine(ApplyDamageToPlayer(player));
                }
                
                // 데미지 인디케이터 표시
                if (_damageIndicator != null)
                {
                    _damageIndicator.SetActive(true);
                }
            }
            else
            {
                // 안전 구역 안으로 들어오면 데미지 중지
                if (_playersInDanger.Contains(player))
                {
                    _playersInDanger.Remove(player);
                }
                
                if (_damageIndicator != null)
                {
                    _damageIndicator.SetActive(false);
                }
            }
        }
    }
    
    /// <summary>
    /// 플레이어에게 지속 데미지 적용
    /// </summary>
    private IEnumerator ApplyDamageToPlayer(PlayerController player)
    {
        while (_playersInDanger.Contains(player) && player != null && !player.isDead)
        {
            player.TakeDamage(_currentDamagePerSecond);
            
            // 데미지 틱 간격
            yield return new WaitForSeconds(1f);
        }
    }
    
    /// <summary>
    /// 현재 안전 구역 정보 가져오기
    /// </summary>
    public Vector3 GetSafeZoneCenter()
    {
        return _currentCenter;
    }
    
    public float GetSafeZoneRadius()
    {
        return _currentRadius;
    }
    
    /// <summary>
    /// 다음 안전 구역 정보 가져오기
    /// </summary>
    public Vector3 GetNextSafeZoneCenter()
    {
        return _nextCenter;
    }
    
    public float GetNextSafeZoneRadius()
    {
        return _nextRadius;
    }
    
    private void OnDestroy()
    {
        if (_safeZoneCoroutine != null)
        {
            StopCoroutine(_safeZoneCoroutine);
        }
    }
}