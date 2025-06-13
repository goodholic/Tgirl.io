using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 모바일 조이스틱 입력을 처리하는 클래스
/// </summary>
public class Joystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] private RectTransform _background;   // 조이스틱 배경
    [SerializeField] private RectTransform _handle;       // 조이스틱 핸들
    [SerializeField] private float _handleRange = 100f;     // 핸들이 이동할 최대 거리

    private Vector2 _inputVector = Vector2.zero;          // 현재 입력 벡터 (-1 ~ 1 범위)
    private bool _isActive;                               // 조이스틱 사용 여부

    /// <summary>
    /// 외부에서 읽을 수 있는 입력 방향
    /// </summary>
    public Vector2 InputDirection => _inputVector;
    public bool IsActive => _isActive;

    /// <summary>
    /// 터치 시작 시 입력 처리 및 활성화 상태 전환
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        ProcessDrag(eventData);
        _isActive = true;
    }

    /// <summary>
    /// 드래그 시 입력 값과 핸들 위치 업데이트
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        ProcessDrag(eventData);
    }

    /// <summary>
    /// 터치 종료 시 조이스틱 상태 초기화
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        ResetJoystick();
    }

    /// <summary>
    /// 터치 입력을 로컬 좌표로 변환하여 입력 벡터와 핸들 위치를 업데이트합니다.
    /// </summary>
    private void ProcessDrag(PointerEventData eventData)
    {
        Vector2 localPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_background, eventData.position, eventData.pressEventCamera, out localPos))
        {
            // 배경 크기에 따라 좌표를 정규화 (-1 ~ 1)
            localPos.x = (localPos.x / _background.sizeDelta.x) * 2f;
            localPos.y = (localPos.y / _background.sizeDelta.y) * 2f;

            _inputVector = new Vector2(localPos.x, localPos.y);
            if (_inputVector.magnitude > 1f)
            {
                _inputVector.Normalize();
            }
            _handle.anchoredPosition = _inputVector * _handleRange;
        }
    }

    /// <summary>
    /// 조이스틱 입력과 핸들 위치를 초기 상태로 복원합니다.
    /// </summary>
    private void ResetJoystick()
    {
        _inputVector = Vector2.zero;
        _handle.anchoredPosition = Vector2.zero;
        _isActive = false;
    }
}
