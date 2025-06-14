using System.Collections;
using UnityEngine;
using TMPro;

public class DestructibleObject : MonoBehaviour
{
    [Header("Object Settings")]
    [SerializeField] private float _health = 50f;
    [SerializeField] private float _maxHealth = 50f;
    [SerializeField] private bool _isDestructible = true;
    
    [Header("Reward Settings")]
    [SerializeField] private RewardType _rewardType = RewardType.Gold;
    [SerializeField] private int _goldReward = 10;
    [SerializeField] private float _buffDuration = 10f;
    [SerializeField] private float _buffAmount = 0.5f; // 50% 증가
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject _destructionEffectPrefab;
    [SerializeField] private GameObject _rewardEffectPrefab;
    [SerializeField] private GameObject _damageParticles;
    [SerializeField] private Material _damageMaterial;
    [SerializeField] private AudioClip _hitSound;
    [SerializeField] private AudioClip _destroySound;
    [SerializeField] private AudioClip _rewardSound;
    
    [Header("Drop Settings")]
    [SerializeField] private GameObject _dropItemPrefab;
    [SerializeField] private float _dropChance = 0.3f; // 30% 확률
    [SerializeField] private float _dropForce = 5f;
    
    [Header("UI")]
    [SerializeField] private GameObject _floatingTextPrefab;
    
    private AudioSource _audioSource;
    private Renderer _renderer;
    private Collider _collider;
    private Material _originalMaterial;
    private bool _isDestroyed = false;
    
    public enum RewardType
    {
        Gold,
        AttackBuff,
        SpeedBuff,
        HealthRestore,
        ShieldBuff,
        Random
    }
    
    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            _originalMaterial = _renderer.material;
        }
        
        _collider = GetComponent<Collider>();
    }
    
    /// <summary>
    /// 데미지를 받아 처리합니다
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (!_isDestructible || _isDestroyed)
            return;
        
        _health -= damage;
        
        // 피격 효과
        ShowDamageEffect();
        
        // 피격 사운드
        if (_hitSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_hitSound);
        }
        
        // 데미지 파티클
        if (_damageParticles != null)
        {
            Instantiate(_damageParticles, transform.position, Quaternion.identity);
        }
        
        // 체력 비율에 따른 시각적 효과
        if (_damageMaterial != null && _renderer != null)
        {
            float healthRatio = _health / _maxHealth;
            _renderer.material.Lerp(_originalMaterial, _damageMaterial, 1f - healthRatio);
        }
        
        if (_health <= 0f)
        {
            Destroy();
        }
    }
    
    /// <summary>
    /// 피격 효과를 표시합니다
    /// </summary>
    private void ShowDamageEffect()
    {
        StartCoroutine(DamageFlash());
    }
    
    private IEnumerator DamageFlash()
    {
        if (_renderer != null)
        {
            Color originalColor = _renderer.material.color;
            _renderer.material.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            _renderer.material.color = originalColor;
        }
    }
    
    /// <summary>
    /// 오브젝트를 파괴하고 보상을 지급합니다
    /// </summary>
    private void Destroy()
    {
        if (_isDestroyed)
            return;
        
        _isDestroyed = true;
        
        // 파괴 효과
        if (_destructionEffectPrefab != null)
        {
            GameObject effect = Instantiate(_destructionEffectPrefab, transform.position, transform.rotation);
            Destroy(effect, 3f);
        }
        
        // 파괴 사운드
        if (_destroySound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_destroySound);
        }
        
        // 보상 지급
        GiveReward();
        
        // 아이템 드롭
        if (Random.value < _dropChance)
        {
            DropItem();
        }
        
        // 콜라이더 비활성화
        if (_collider != null)
        {
            _collider.enabled = false;
        }
        
        // 오브젝트 제거 애니메이션
        StartCoroutine(DestroyAnimation());
    }
    
    /// <summary>
    /// 보상을 지급합니다
    /// </summary>
    private void GiveReward()
    {
        RewardType rewardToGive = _rewardType;
        
        // Random 타입인 경우 랜덤 선택
        if (rewardToGive == RewardType.Random)
        {
            rewardToGive = (RewardType)Random.Range(0, System.Enum.GetValues(typeof(RewardType)).Length - 1);
        }
        
        PlayerController player = PlayerManager.Instance.curPlayer;
        if (player == null)
            return;
        
        string rewardText = "";
        Color textColor = Color.white;
        
        switch (rewardToGive)
        {
            case RewardType.Gold:
                UpgradeManager.Gold += _goldReward;
                UpgradeManager.SaveData();
                UIManager.Instance.RefreshGoldUI();
                rewardText = $"+{_goldReward} 골드";
                textColor = Color.yellow;
                break;
                
            case RewardType.AttackBuff:
                StartCoroutine(ApplyAttackBuff(player));
                rewardText = $"공격력 +{_buffAmount * 100}%";
                textColor = Color.red;
                break;
                
            case RewardType.SpeedBuff:
                StartCoroutine(ApplySpeedBuff(player));
                rewardText = $"이동속도 +{_buffAmount * 100}%";
                textColor = Color.cyan;
                break;
                
            case RewardType.HealthRestore:
                float healAmount = player._maxHealth * 0.3f; // 최대 체력의 30% 회복
                player._health = Mathf.Min(player._health + healAmount, player._maxHealth);
                rewardText = $"+{healAmount:F0} HP";
                textColor = Color.green;
                break;
                
            case RewardType.ShieldBuff:
                StartCoroutine(ApplyShieldBuff(player));
                rewardText = "보호막 활성화";
                textColor = Color.blue;
                break;
        }
        
        // 보상 효과
        if (_rewardEffectPrefab != null)
        {
            GameObject effect = Instantiate(_rewardEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // 보상 사운드
        if (_rewardSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_rewardSound);
        }
        
        // 플로팅 텍스트
        ShowFloatingText(rewardText, textColor);
    }
    
    /// <summary>
    /// 공격력 버프를 적용합니다
    /// </summary>
    private IEnumerator ApplyAttackBuff(PlayerController player)
    {
        // 임시로 Projectile의 데미지를 증가시킴
        // 실제로는 PlayerController에 버프 시스템을 추가하는 것이 좋음
        float originalDamage = 10f; // 기본 데미지
        
        // 버프 시작 효과
        GameObject buffEffect = null;
        if (player != null)
        {
            // 버프 이펙트를 플레이어에 부착
            // buffEffect = Instantiate(attackBuffEffectPrefab, player.transform);
        }
        
        yield return new WaitForSeconds(_buffDuration);
        
        // 버프 종료
        if (buffEffect != null)
        {
            Destroy(buffEffect);
        }
    }
    
    /// <summary>
    /// 이동속도 버프를 적용합니다
    /// </summary>
    private IEnumerator ApplySpeedBuff(PlayerController player)
    {
        if (player == null)
            yield break;
        
        // NavMeshAgent의 속도 증가
        UnityEngine.AI.NavMeshAgent agent = player.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            float originalSpeed = agent.speed;
            agent.speed *= (1f + _buffAmount);
            
            yield return new WaitForSeconds(_buffDuration);
            
            agent.speed = originalSpeed;
        }
    }
    
    /// <summary>
    /// 보호막 버프를 적용합니다
    /// </summary>
    private IEnumerator ApplyShieldBuff(PlayerController player)
    {
        // 임시 무적 상태 구현
        // 실제로는 별도의 Shield 컴포넌트를 만드는 것이 좋음
        
        yield return new WaitForSeconds(_buffDuration);
    }
    
    /// <summary>
    /// 아이템을 드롭합니다
    /// </summary>
    private void DropItem()
    {
        if (_dropItemPrefab == null)
            return;
        
        Vector3 dropPosition = transform.position + Vector3.up * 1f;
        GameObject droppedItem = Instantiate(_dropItemPrefab, dropPosition, Quaternion.identity);
        
        // 아이템에 물리 효과 적용
        Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 randomDirection = new Vector3(
                Random.Range(-1f, 1f),
                1f,
                Random.Range(-1f, 1f)
            ).normalized;
            
            rb.AddForce(randomDirection * _dropForce, ForceMode.Impulse);
        }
    }
    
    /// <summary>
    /// 플로팅 텍스트를 표시합니다
    /// </summary>
    private void ShowFloatingText(string text, Color color)
    {
        if (_floatingTextPrefab == null)
            return;
        
        Vector3 spawnPosition = transform.position + Vector3.up * 2f;
        GameObject floatingText = Instantiate(_floatingTextPrefab, spawnPosition, Quaternion.identity);
        
        TextMeshPro tmp = floatingText.GetComponent<TextMeshPro>();
        if (tmp != null)
        {
            tmp.text = text;
            tmp.color = color;
        }
        
        // FloatingDamageText 컴포넌트가 있다면 활용
        FloatingDamageText floatingDamage = floatingText.GetComponent<FloatingDamageText>();
        if (floatingDamage != null)
        {
            floatingDamage.SetDamageText(text);
        }
    }
    
    /// <summary>
    /// 파괴 애니메이션을 재생합니다
    /// </summary>
    private IEnumerator DestroyAnimation()
    {
        float duration = 1f;
        float elapsed = 0f;
        Vector3 originalScale = transform.localScale;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // 크기 축소
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            
            // 회전
            transform.Rotate(Vector3.up * 360f * Time.deltaTime);
            
            // 투명도 감소
            if (_renderer != null)
            {
                Color color = _renderer.material.color;
                color.a = 1f - t;
                _renderer.material.color = color;
            }
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 충돌 처리 (Projectile과의 충돌)
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Projectile"))
        {
            Projectile projectile = other.GetComponent<Projectile>();
            if (projectile != null && projectile.userType == UserType.Player)
            {
                TakeDamage(projectile.Damage);
            }
        }
    }
    
    /// <summary>
    /// 근접 공격 처리
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        // 검 캐릭터의 근접 공격 처리
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null && player.CharacterTypeProperty == CharacterType.Sword)
            {
                // 검 공격 중인지 확인 (애니메이션 상태나 별도 플래그로 체크)
                // TakeDamage(swordDamage);
            }
        }
    }
}