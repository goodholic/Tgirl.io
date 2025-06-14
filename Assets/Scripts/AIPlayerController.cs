using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIPlayerController : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private AIBehaviorType _behaviorType = AIBehaviorType.Balanced;
    [SerializeField] private float _detectionRadius = 20f;
    [SerializeField] private float _attackRadius = 15f;
    [SerializeField] private float _fleeHealthThreshold = 30f;
    [SerializeField] private float _decisionInterval = 0.5f;
    
    [Header("Target Priority")]
    [SerializeField] private float _playerPriority = 0.7f;
    [SerializeField] private float _zombiePriority = 0.3f;
    [SerializeField] private float _crownPriority = 0.9f;
    [SerializeField] private float _itemPriority = 0.5f;
    
    [Header("Combat Settings")]
    [SerializeField] private float _aimAccuracy = 0.8f; // 0-1 정확도
    [SerializeField] private float _reactionTime = 0.2f;
    [SerializeField] private float _dodgeChance = 0.3f;
    [SerializeField] private float _skillUsageChance = 0.4f;
    
    private PlayerController _playerController;
    private NavMeshAgent _agent;
    private Transform _currentTarget;
    private AIState _currentState = AIState.Idle;
    private float _lastDecisionTime;
    private float _lastAttackTime;
    private List<Transform> _visibleEnemies = new List<Transform>();
    private List<Transform> _visibleItems = new List<Transform>();
    private Crown _crownReference;
    
    public enum AIBehaviorType
    {
        Aggressive,  // 공격적 - 플레이어 우선 공격
        Defensive,   // 방어적 - 안전 우선, 체력 관리
        Balanced,    // 균형 - 상황에 따라 판단
        Hunter,      // 사냥꾼 - 좀비 우선 처치
        CrownSeeker  // 왕관 추적자 - 왕관 획득 우선
    }
    
    public enum AIState
    {
        Idle,
        Patrol,
        Combat,
        Flee,
        SeekCrown,
        SeekItem,
        UseSkill
    }
    
    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        _agent = GetComponent<NavMeshAgent>();
        
        // AI는 수동 조작 비활성화
        _playerController.enabled = false;
    }
    
    private void Start()
    {
        StartCoroutine(AIRoutine());
        StartCoroutine(DetectionRoutine());
    }
    
    /// <summary>
    /// AI 메인 루틴
    /// </summary>
    private IEnumerator AIRoutine()
    {
        while (!_playerController.isDead)
        {
            // 결정 간격마다 상태 업데이트
            if (Time.time - _lastDecisionTime > _decisionInterval)
            {
                UpdateAIState();
                _lastDecisionTime = Time.time;
            }
            
            // 현재 상태에 따른 행동 실행
            ExecuteCurrentState();
            
            yield return null;
        }
    }
    
    /// <summary>
    /// 주변 감지 루틴
    /// </summary>
    private IEnumerator DetectionRoutine()
    {
        while (!_playerController.isDead)
        {
            DetectNearbyObjects();
            yield return new WaitForSeconds(0.2f); // 0.2초마다 감지
        }
    }
    
    /// <summary>
    /// AI 상태 업데이트
    /// </summary>
    private void UpdateAIState()
    {
        // 체력이 낮으면 도망
        if (_playerController._health < _fleeHealthThreshold)
        {
            _currentState = AIState.Flee;
            return;
        }
        
        // 왕관이 있고 CrownSeeker 타입이면 왕관 추적
        if (_behaviorType == AIBehaviorType.CrownSeeker && _crownReference != null)
        {
            _currentState = AIState.SeekCrown;
            return;
        }
        
        // 전투 가능한 적이 있으면 전투
        if (_visibleEnemies.Count > 0)
        {
            _currentTarget = GetBestTarget();
            if (_currentTarget != null)
            {
                _currentState = AIState.Combat;
                return;
            }
        }
        
        // 아이템이 있으면 아이템 획득
        if (_visibleItems.Count > 0 && Random.value < _itemPriority)
        {
            _currentState = AIState.SeekItem;
            return;
        }
        
        // 기본 상태는 순찰
        _currentState = AIState.Patrol;
    }
    
    /// <summary>
    /// 현재 상태 실행
    /// </summary>
    private void ExecuteCurrentState()
    {
        switch (_currentState)
        {
            case AIState.Idle:
                ExecuteIdle();
                break;
            case AIState.Patrol:
                ExecutePatrol();
                break;
            case AIState.Combat:
                ExecuteCombat();
                break;
            case AIState.Flee:
                ExecuteFlee();
                break;
            case AIState.SeekCrown:
                ExecuteSeekCrown();
                break;
            case AIState.SeekItem:
                ExecuteSeekItem();
                break;
            case AIState.UseSkill:
                ExecuteUseSkill();
                break;
        }
    }
    
    /// <summary>
    /// 대기 상태 실행
    /// </summary>
    private void ExecuteIdle()
    {
        // 잠시 대기 후 순찰로 전환
        _agent.SetDestination(transform.position);
    }
    
    /// <summary>
    /// 순찰 상태 실행
    /// </summary>
    private void ExecutePatrol()
    {
        if (!_agent.hasPath || _agent.remainingDistance < 2f)
        {
            // 랜덤한 위치로 이동
            Vector3 randomDirection = Random.insideUnitSphere * 20f;
            randomDirection += transform.position;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, 20f, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
            }
        }
        
        // 이동 중 주변 스캔
        RotateToMovementDirection();
    }
    
    /// <summary>
    /// 전투 상태 실행
    /// </summary>
    private void ExecuteCombat()
    {
        if (_currentTarget == null || !IsTargetValid(_currentTarget))
        {
            _currentState = AIState.Patrol;
            return;
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.position);
        
        // 공격 범위 내에 있으면
        if (distanceToTarget <= _attackRadius)
        {
            // 이동 멈추고 조준
            _agent.SetDestination(transform.position);
            AimAtTarget(_currentTarget);
            
            // 공격
            if (Time.time - _lastAttackTime > _playerController._attackInterval)
            {
                PerformAttack();
                _lastAttackTime = Time.time;
            }
            
            // 일정 확률로 스킬 사용
            if (Random.value < _skillUsageChance)
            {
                UseRandomSkill();
            }
            
            // 회피 동작
            if (Random.value < _dodgeChance && IsUnderAttack())
            {
                PerformDodge();
            }
        }
        else
        {
            // 타겟에게 접근
            _agent.SetDestination(_currentTarget.position);
            RotateToTarget(_currentTarget);
        }
    }
    
    /// <summary>
    /// 도망 상태 실행
    /// </summary>
    private void ExecuteFlee()
    {
        // 가장 가까운 적으로부터 도망
        Transform nearestEnemy = GetNearestEnemy();
        if (nearestEnemy != null)
        {
            Vector3 fleeDirection = (transform.position - nearestEnemy.position).normalized;
            Vector3 fleePosition = transform.position + fleeDirection * 20f;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(fleePosition, out hit, 20f, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
            }
        }
        
        // 체력이 회복되면 다시 전투
        if (_playerController._health > _fleeHealthThreshold * 1.5f)
        {
            _currentState = AIState.Patrol;
        }
    }
    
    /// <summary>
    /// 왕관 추적 상태 실행
    /// </summary>
    private void ExecuteSeekCrown()
    {
        if (_crownReference == null)
        {
            _currentState = AIState.Patrol;
            return;
        }
        
        _agent.SetDestination(_crownReference.transform.position);
        RotateToMovementDirection();
    }
    
    /// <summary>
    /// 아이템 획득 상태 실행
    /// </summary>
    private void ExecuteSeekItem()
    {
        if (_visibleItems.Count == 0)
        {
            _currentState = AIState.Patrol;
            return;
        }
        
        Transform nearestItem = GetNearestItem();
        if (nearestItem != null)
        {
            _agent.SetDestination(nearestItem.position);
            RotateToMovementDirection();
        }
    }
    
    /// <summary>
    /// 스킬 사용 상태 실행
    /// </summary>
    private void ExecuteUseSkill()
    {
        // 스킬 사용 로직
        _currentState = AIState.Combat;
    }
    
    /// <summary>
    /// 주변 오브젝트 감지
    /// </summary>
    private void DetectNearbyObjects()
    {
        _visibleEnemies.Clear();
        _visibleItems.Clear();
        
        Collider[] colliders = Physics.OverlapSphere(transform.position, _detectionRadius);
        
        foreach (Collider col in colliders)
        {
            // 적(좀비) 감지
            if (col.CompareTag("Enemy"))
            {
                DamageableController enemy = col.GetComponent<DamageableController>();
                if (enemy != null && !enemy.isDead)
                {
                    _visibleEnemies.Add(col.transform);
                }
            }
            // 다른 플레이어 감지
            else if (col.CompareTag("Player") && col.gameObject != gameObject)
            {
                PlayerController otherPlayer = col.GetComponent<PlayerController>();
                if (otherPlayer != null && !otherPlayer.isDead)
                {
                    _visibleEnemies.Add(col.transform);
                }
            }
            // 아이템 감지
            else if (col.CompareTag("Item"))
            {
                _visibleItems.Add(col.transform);
            }
            // 왕관 감지
            else if (col.CompareTag("Crown"))
            {
                _crownReference = col.GetComponent<Crown>();
            }
        }
    }
    
    /// <summary>
    /// 최적의 타겟 선택
    /// </summary>
    private Transform GetBestTarget()
    {
        Transform bestTarget = null;
        float bestScore = float.MinValue;
        
        foreach (Transform enemy in _visibleEnemies)
        {
            float score = CalculateTargetScore(enemy);
            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = enemy;
            }
        }
        
        return bestTarget;
    }
    
    /// <summary>
    /// 타겟 점수 계산
    /// </summary>
    private float CalculateTargetScore(Transform target)
    {
        float distance = Vector3.Distance(transform.position, target.position);
        float distanceScore = 1f - (distance / _detectionRadius);
        
        float priorityScore = 0f;
        
        // 플레이어 우선순위
        if (target.CompareTag("Player"))
        {
            priorityScore = _playerPriority;
            
            // 현상금이 있는 플레이어는 더 높은 우선순위
            PlayerController targetPlayer = target.GetComponent<PlayerController>();
            if (targetPlayer != null && GameManager.Instance != null)
            {
                var stats = GameManager.Instance.GetPlayerStats(targetPlayer);
                if (stats != null && stats.bounty > 0)
                {
                    priorityScore += 0.2f;
                }
            }
        }
        // 좀비 우선순위
        else if (target.CompareTag("Enemy"))
        {
            priorityScore = _zombiePriority;
        }
        
        // 행동 타입에 따른 가중치
        switch (_behaviorType)
        {
            case AIBehaviorType.Aggressive:
                if (target.CompareTag("Player")) priorityScore *= 1.5f;
                break;
            case AIBehaviorType.Hunter:
                if (target.CompareTag("Enemy")) priorityScore *= 1.5f;
                break;
        }
        
        return distanceScore * 0.3f + priorityScore * 0.7f;
    }
    
    /// <summary>
    /// 타겟 유효성 검사
    /// </summary>
    private bool IsTargetValid(Transform target)
    {
        if (target == null) return false;
        
        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > _detectionRadius) return false;
        
        // 시야 차단 검사
        RaycastHit hit;
        Vector3 direction = (target.position - transform.position).normalized;
        if (Physics.Raycast(transform.position + Vector3.up, direction, out hit, distance))
        {
            if (hit.transform != target)
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 타겟 조준
    /// </summary>
    private void AimAtTarget(Transform target)
    {
        Vector3 targetPosition = target.position;
        
        // 정확도에 따른 오차 추가
        float inaccuracy = 1f - _aimAccuracy;
        targetPosition += Random.insideUnitSphere * inaccuracy * 2f;
        
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;
        
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
    }
    
    /// <summary>
    /// 공격 수행
    /// </summary>
    private void PerformAttack()
    {
        // PlayerController의 Attack 메서드 직접 호출
        _playerController.Attack();
    }
    
    /// <summary>
    /// 회피 동작
    /// </summary>
    private void PerformDodge()
    {
        // 백대쉬 실행
        _playerController.OnBackDashButton();
    }
    
    /// <summary>
    /// 랜덤 스킬 사용
    /// </summary>
    private void UseRandomSkill()
    {
        int randomSkill = Random.Range(0, 3);
        
        switch (randomSkill)
        {
            case 0:
                if (_playerController.GetSkill1CooldownRemaining() <= 0)
                    _playerController.OnSkill1Button();
                break;
            case 1:
                if (_playerController.GetSkill2CooldownRemaining() <= 0)
                    _playerController.OnSkill2Button();
                break;
            case 2:
                if (_playerController.GetUltimateCooldownRemaining() <= 0)
                    _playerController.OnUltimateButton();
                break;
        }
    }
    
    /// <summary>
    /// 공격받고 있는지 확인
    /// </summary>
    private bool IsUnderAttack()
    {
        // 근처에 발사체가 있는지 확인
        Collider[] projectiles = Physics.OverlapSphere(transform.position, 5f);
        foreach (Collider col in projectiles)
        {
            if (col.CompareTag("Projectile"))
            {
                Projectile proj = col.GetComponent<Projectile>();
                if (proj != null && proj.userType != UserType.Player)
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    /// <summary>
    /// 가장 가까운 적 찾기
    /// </summary>
    private Transform GetNearestEnemy()
    {
        Transform nearest = null;
        float minDistance = float.MaxValue;
        
        foreach (Transform enemy in _visibleEnemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = enemy;
            }
        }
        
        return nearest;
    }
    
    /// <summary>
    /// 가장 가까운 아이템 찾기
    /// </summary>
    private Transform GetNearestItem()
    {
        Transform nearest = null;
        float minDistance = float.MaxValue;
        
        foreach (Transform item in _visibleItems)
        {
            float distance = Vector3.Distance(transform.position, item.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = item;
            }
        }
        
        return nearest;
    }
    
    /// <summary>
    /// 이동 방향으로 회전
    /// </summary>
    private void RotateToMovementDirection()
    {
        if (_agent.velocity.sqrMagnitude > 0.01f)
        {
            Vector3 direction = _agent.velocity.normalized;
            direction.y = 0;
            
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
        }
    }
    
    /// <summary>
    /// 타겟 방향으로 회전
    /// </summary>
    private void RotateToTarget(Transform target)
    {
        if (target != null)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            direction.y = 0;
            
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }
    }
    
    /// <summary>
    /// AI 난이도 설정
    /// </summary>
    public void SetDifficulty(float difficulty)
    {
        // 0 ~ 1 난이도에 따라 AI 파라미터 조정
        _aimAccuracy = Mathf.Lerp(0.5f, 0.95f, difficulty);
        _reactionTime = Mathf.Lerp(0.5f, 0.1f, difficulty);
        _dodgeChance = Mathf.Lerp(0.1f, 0.5f, difficulty);
        _skillUsageChance = Mathf.Lerp(0.2f, 0.6f, difficulty);
    }
    
    private void OnDrawGizmosSelected()
    {
        // 감지 범위 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);
        
        // 공격 범위 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRadius);
    }
}