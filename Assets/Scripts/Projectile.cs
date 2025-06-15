using UnityEngine;

public enum UserType
{
    Player,
    Enemy
}

public class Projectile : MonoBehaviour
{
    [SerializeField] private float _projectileSpeed = 20f;    // 발사체 이동 속도
    [SerializeField] private int _damage = 10;               // 발사체 피해량
    [SerializeField] private float _lifetime = 5f;              // 발사체 수명 (초)
    [SerializeField] private GameObject _hitEffectPrefab;       // 타격 시 재생할 이펙트 프리팹

    [SerializeField] private Rigidbody _rb;                   // Rigidbody 컴포넌트 (Inspector에서 할당하거나 자동 획득)

    // 발사체 소유자 (외부에서 할당)
    public UserType userType;
    public PlayerController owner; // 플레이어가 발사한 경우 소유자 추적
    
    public int Damage { get { return _damage; } set { _damage = value; } }

    private void Start()
    {
        // 수명 경과 시 자동 파괴
        Destroy(gameObject, _lifetime);

        // Rigidbody 컴포넌트가 할당되지 않은 경우 자동으로 획득
        if (_rb == null)
        {
            _rb = GetComponent<Rigidbody>();
        }
    }

    private void Update()
    {
        // Rigidbody를 사용하여 발사체를 전방으로 이동
        if (_rb != null)
        {
            _rb.linearVelocity = transform.forward * _projectileSpeed;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // "Effect" 태그는 무시
        if (other.CompareTag("Effect"))
        {
            return;
        }

        // 플레이어 소유 발사체는 자기 자신과 충돌 무시
        if (userType == UserType.Player && owner != null && other.gameObject == owner.gameObject)
        {
            return;
        }

        // 적 소유 발사체는 적과 충돌 무시
        if (userType == UserType.Enemy && other.CompareTag("Enemy"))
        {
            return;
        }

        // 타격 이펙트 재생 - ClosestPoint 대신 충돌 위치 사용
        if (_hitEffectPrefab != null)
        {
            Vector3 hitPos = transform.position; // 발사체의 현재 위치를 사용
            
            // 더 정확한 충돌 지점이 필요한 경우에만 ClosestPoint 사용
            // 단, convex 콜라이더인 경우에만
            Collider collider = other.GetComponent<Collider>();
            if (collider != null)
            {
                // BoxCollider, SphereCollider, CapsuleCollider는 ClosestPoint 사용 가능
                if (collider is BoxCollider || collider is SphereCollider || collider is CapsuleCollider)
                {
                    hitPos = collider.ClosestPoint(transform.position);
                }
                else if (collider is MeshCollider meshCollider && meshCollider.convex)
                {
                    // Convex MeshCollider도 사용 가능
                    hitPos = collider.ClosestPoint(transform.position);
                }
                // 그 외의 경우는 발사체 위치 사용
            }
            
            Instantiate(_hitEffectPrefab, hitPos, Quaternion.identity);
        }
        
        // 충돌 대상이 DamageableController를 가지고 있다면, 소유자에 따라 피해 적용
        DamageableController target = other.GetComponent<DamageableController>();
        if (target != null)
        {
            // 플레이어가 적을 공격
            if (userType == UserType.Player && other.CompareTag("Enemy"))
            {
                // Enemy에게 공격자 정보 전달
                EnemyController enemyTarget = other.GetComponent<EnemyController>();
                if (enemyTarget != null && owner != null)
                {
                    enemyTarget.SetAttacker(owner);
                }
                target.TakeDamage(_damage);
            }
            // 적이 플레이어를 공격
            else if (userType == UserType.Enemy && other.CompareTag("Player"))
            {
                PlayerController playerTarget = other.GetComponent<PlayerController>();
                if (playerTarget != null)
                {
                    playerTarget.SetLastAttacker(null); // 적이 공격한 경우
                }
                target.TakeDamage(_damage);
            }
            // 플레이어가 다른 플레이어를 공격 (PvP)
            else if (userType == UserType.Player && other.CompareTag("Player") && owner != null)
            {
                PlayerController playerTarget = other.GetComponent<PlayerController>();
                if (playerTarget != null && playerTarget != owner)
                {
                    playerTarget.SetLastAttacker(owner); // 공격자 설정
                    target.TakeDamage(_damage);
                }
            }
        }

        // 발사체 파괴
        Destroy(gameObject);
    }
}