using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : DamageableController
{
    [SerializeField] private int _damage = 10;                // 플레이어에게 줄 피해량
    [SerializeField] private float _attackDistance = 1.5f;         // 공격이 가능한 거리
    [SerializeField] private float _attackCooldown = 1f;           // 공격 후 대기 시간 (초)
    [SerializeField] private float _dieDuration = 2f;              // 사망 효과 지속 시간 (초)
    [SerializeField] private int _baseGold = 5;              // 사망 시 지급 골드

    [SerializeField] private GameObject _projectilePrefab;         // 원거리 공격용 발사체 프리팹
    [SerializeField] private Transform _projectileSpawnPoint;      // 발사체 생성 위치

    private NavMeshAgent _agent;                                   // NavMeshAgent 컴포넌트
    private float _lastAttackTime = 0f;                            // 마지막 공격 시각 기록
    private bool _isAttacking = false;                             // 공격 중 여부

    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        // 사망 상태면 에이전트를 정지하고 더 이상 처리하지 않음
        if (isDead)
        {
            if (_agent != null)
                _agent.isStopped = true;
            return;
        }

        // 플레이어가 사망 상태면 공격 애니메이션 해제 후 종료
        if (EnemySpawner.Instance.Player.isDead)
        {
            animator.SetBool("isAttack", false);
            return;
        }

        if (EnemySpawner.Instance.Player == null)
            return;

        // 플레이어 위치 및 거리 계산
        Vector3 playerPos = EnemySpawner.Instance.Player.transform.position;
        float distance = Vector3.Distance(transform.position, playerPos);

        // 공격 중인데 플레이어가 범위를 벗어나면 공격 상태 해제 후 추격 시작
        if (_isAttacking && distance > _attackDistance)
        {
            _isAttacking = false;
            _agent.isStopped = false;
            animator.SetBool("isAttack", false);
        }

        // 공격 중이 아니면 플레이어를 추격
        if (!_isAttacking)
        {
            _agent.SetDestination(playerPos);
        }
        else
        {
            // 공격 중일 때는 수평으로만 플레이어를 바라봄 (높이 값은 유지)
            Vector3 lookTarget = playerPos;
            lookTarget.y = transform.position.y;
            transform.LookAt(lookTarget);
        }

        // 플레이어가 공격 범위 내에 있고, 쿨타임이 지난 경우 공격 시작
        if (distance <= _attackDistance && !_isAttacking && Time.time - _lastAttackTime >= _attackCooldown)
        {
            _isAttacking = true;
            _agent.isStopped = true;
            animator.SetBool("isAttack", true);
        }
    }

    /// <summary>
    /// 근접 공격 애니메이션 이벤트로 호출되어 플레이어에게 피해를 적용합니다.
    /// </summary>
    public void ApplyDamage()
    {
        if (EnemySpawner.Instance.Player != null)
        {
            EnemySpawner.Instance.Player.TakeDamage(_damage);
        }
    }

    /// <summary>
    /// 공격 애니메이션 종료 이벤트로 호출되어 공격 상태를 해제합니다.
    /// </summary>
    public void EndAttack()
    {
        _isAttacking = false;
        _lastAttackTime = Time.time;
        _agent.isStopped = false;
        animator.SetBool("isAttack", false);
    }

    /// <summary>
    /// 원거리 공격을 수행합니다.
    /// 플레이어를 바라보도록 회전한 후, 발사체를 생성하여 공격합니다.
    /// </summary>
    public void RangeAttack()
    {
        if (EnemySpawner.Instance.Player == null)
            return;

        if (_projectilePrefab != null && _projectileSpawnPoint != null)
        {
            GameObject bullet = Instantiate(_projectilePrefab, _projectileSpawnPoint.position, Quaternion.identity);

            // 플레이어의 몸통 부분(높이 오프셋 1.3f)을 목표로 설정
            Vector3 targetPos = EnemySpawner.Instance.Player.transform.position + new Vector3(0f, 0.8f, 0f);
            bullet.transform.LookAt(targetPos);

            Projectile projectile = bullet.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.userType = UserType.Enemy;
                projectile.Damage = _damage;
            }
        }
    }

    /// <summary>
    /// 사망 처리: 모든 자식 콜라이더를 비활성화하고, 사망 애니메이션과 효과 재생 후 오브젝트를 제거합니다.
    /// </summary>
    protected override void Die()
    {
        // 골드 수급량 증가 업그레이드가 있다면 추가 보정
        int totalGold = _baseGold + (int)UpgradeManager.GetGoldGainBonus();
        UpgradeManager.Gold += totalGold;
        UpgradeManager.SaveData();

        UIManager.Instance.RefreshUpgradeUI();
        UIManager.Instance.RefreshGoldUI();
        
        isDead = true;
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        animator.SetBool("isDie", true);
        StartCoroutine(DieEffectCoroutine());
    }

    /// <summary>
    /// 사망 효과를 재생한 후 오브젝트를 파괴하는 코루틴입니다.
    /// </summary>
    private IEnumerator DieEffectCoroutine()
    {
        yield return new WaitForSeconds(1f);
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        float timer = 0f;
        while (timer < _dieDuration)
        {
            float value = Mathf.Lerp(0f, 1f, timer / _dieDuration);
            foreach (Renderer rend in renderers)
            {
                foreach (Material mat in rend.materials)
                {
                    mat.SetFloat("_Cutout", value);
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }
        foreach (Renderer rend in renderers)
        {
            foreach (Material mat in rend.materials)
            {
                mat.SetFloat("_Cutout", 1f);
            }
        }
        Destroy(gameObject);
    }
}
