using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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
