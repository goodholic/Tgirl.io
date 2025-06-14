using System;
using UnityEngine;

public class Crown : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 50f;
    [SerializeField] private float _bobSpeed = 2f;
    [SerializeField] private float _bobAmount = 0.5f;
    [SerializeField] private GameObject _pickupEffectPrefab;
    [SerializeField] private Vector3 _wearOffset = new Vector3(0, 2f, 0); // 플레이어 머리 위 오프셋
    
    private Vector3 _startPosition;
    private bool _isPickedUp = false;
    private Transform _currentOwner;
    
    public event Action<PlayerController> OnCrownPickup;
    
    private void Start()
    {
        _startPosition = transform.position;
    }
    
    private void Update()
    {
        if (!_isPickedUp)
        {
            // 회전 애니메이션
            transform.Rotate(Vector3.up * _rotationSpeed * Time.deltaTime);
            
            // 위아래 움직임
            float newY = _startPosition.y + Mathf.Sin(Time.time * _bobSpeed) * _bobAmount;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
        else if (_currentOwner != null)
        {
            // 플레이어를 따라다니며 머리 위에 위치
            transform.position = _currentOwner.position + _wearOffset;
            transform.rotation = Quaternion.identity;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (_isPickedUp) return;
        
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && !player.isDead)
            {
                PickupCrown(player);
            }
        }
    }
    
    private void PickupCrown(PlayerController player)
    {
        _isPickedUp = true;
        _currentOwner = player.transform;
        
        // 콜라이더 비활성화
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }
        
        // 픽업 이펙트 재생
        if (_pickupEffectPrefab != null)
        {
            Instantiate(_pickupEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // 이벤트 발생
        OnCrownPickup?.Invoke(player);
    }
    
    public void TransferToPlayer(PlayerController newOwner)
    {
        if (newOwner != null)
        {
            _currentOwner = newOwner.transform;
            
            // 전송 이펙트 재생
            if (_pickupEffectPrefab != null)
            {
                Instantiate(_pickupEffectPrefab, transform.position, Quaternion.identity);
            }
        }
    }
    
    public void Drop()
    {
        _isPickedUp = false;
        _currentOwner = null;
        
        // 콜라이더 다시 활성화
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }
        
        // 원래 위치로 돌아가기
        transform.position = _startPosition;
        transform.rotation = Quaternion.identity;
    }
}