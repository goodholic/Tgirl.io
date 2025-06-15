using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Diagnostics;

/// <summary>
/// 체력 관리, 피해 처리 및 피격 효과를 담당하는 기본 클래스
/// 자식 클래스에서 Die() 메소드를 구현하여 사망 시 로직을 정의합니다.
/// </summary>
public abstract class DamageableController : MonoBehaviour
{
    public float _maxHealth = 100f;     // 최대 체력
    public float _health = 100f;        // 현재 체력
    [SerializeField] private GameObject _damageTextPrefab;  // 데미지 텍스트 프리팹
    [SerializeField] private float _damageTextYOffset = 1f;   // 데미지 텍스트 표시 Y 오프셋

    [SerializeField] private Slider _healthBar;             // 체력바 UI (옵션)
    [SerializeField] private Text _healthText;              // 체력 텍스트 UI (옵션)

    protected Animator _animator;                           // Animator 컴포넌트
    public bool isDead { get; protected set; } = false;       // 사망 상태
    public Animator animator => _animator;

    /// <summary>
    /// 초기화: Animator 획득 및 UI 초기값 설정
    /// </summary>
    protected virtual void Awake()
    {
        _animator = GetComponent<Animator>();

        if (_healthBar != null)
        {
            _healthBar.maxValue = _maxHealth;
            _healthBar.value = _health;
        }
        if (_healthText != null)
        {
            _healthText.text = $"{Mathf.RoundToInt(_health)} / {Mathf.RoundToInt(_maxHealth)}";
        }
    }

    /// <summary>
    /// 외부에서 피해를 줄 때 호출하는 메소드
    /// 체력을 감소시키고, UI와 데미지 텍스트, 피격 효과를 처리한 후 체력이 0 이하이면 사망 처리합니다.
    /// </summary>
    /// <param name="damage">입은 피해량</param>
    public virtual void TakeDamage(float damage)
    {
        if (isDead)
            return;

        // 데미지 원인 추적을 위한 디버그 로그
        LogDamageSource(damage);

        _health -= damage;
        if (_health < 0f)
            _health = 0f;

        if (_healthBar != null)
            _healthBar.value = _health;
        if (_healthText != null)
            _healthText.text = $"{Mathf.RoundToInt(_health)} / {Mathf.RoundToInt(_maxHealth)}";

        // 데미지 텍스트 표시
        if (_damageTextPrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * _damageTextYOffset;
            GameObject dmgInstance = Instantiate(_damageTextPrefab, spawnPos, Quaternion.identity);
            FloatingDamageText dmgText = dmgInstance.GetComponent<FloatingDamageText>();
            if (dmgText != null)
            {
                dmgText.SetDamageText(damage.ToString());
            }
        }

        // 피격 플래시 효과 실행
        StartCoroutine(FlashDamageEffect());

        if (_health <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// 데미지 원인을 추적하는 메서드
    /// </summary>
    private void LogDamageSource(float damage)
    {
        // 호출 스택 추적
        StackTrace stackTrace = new StackTrace(true);
        string damageSource = "Unknown";
        
        // 스택 프레임을 순회하며 데미지 원인 찾기
        for (int i = 2; i < stackTrace.FrameCount && i < 10; i++)
        {
            StackFrame frame = stackTrace.GetFrame(i);
            var method = frame.GetMethod();
            if (method != null)
            {
                string className = method.DeclaringType?.Name ?? "UnknownClass";
                string methodName = method.Name;
                
                // Projectile, EnemyController 등 주요 클래스 확인
                if (className == "Projectile" || className == "EnemyController" || 
                    className == "PlayerController" || className == "AIPlayerController" ||
                    className == "SafeZoneManager" || className == "DestructibleObject")
                {
                    damageSource = $"{className}.{methodName}";
                    break;
                }
            }
        }

        // 플레이어인 경우에만 로그 출력
        if (gameObject.CompareTag("Player"))
        {
            UnityEngine.Debug.Log($"[DAMAGE] {gameObject.name} 받은 데미지: {damage} | 원인: {damageSource} | 남은 체력: {_health}/{_maxHealth}");
        }
    }

    /// <summary>
    /// 사망 시 실행할 로직을 자식 클래스에서 구현합니다.
    /// </summary>
    protected abstract void Die();

    /// <summary>
    /// 피격 시 재질 속성("_RimLigInt")를 변경하여 플래시 효과를 연출합니다.
    /// </summary>
    private IEnumerator FlashDamageEffect()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        // 모든 재질에 초기값 1 적용
        foreach (Renderer rend in renderers)
        {
            foreach (Material mat in rend.materials)
            {
                mat.SetFloat("_RimLigInt", 1f);
            }
        }

        yield return new WaitForSeconds(0.1f);

        float duration = 0.2f;
        float timer = 0f;
        while (timer < duration)
        {
            float value = Mathf.Lerp(1f, 0f, timer / duration);
            foreach (Renderer rend in renderers)
            {
                foreach (Material mat in rend.materials)
                {
                    mat.SetFloat("_RimLigInt", value);
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }
    }
}