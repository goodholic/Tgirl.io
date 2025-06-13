using UnityEngine;
using System.Collections;
using TMPro;

public class FloatingDamageText : MonoBehaviour
{
    [SerializeField] private TextMeshPro _damageText;   // 3D TextMeshPro 컴포넌트 (월드 공간)
    [SerializeField] private float _duration = 1f;          // 텍스트 지속 시간 (초)
    [SerializeField] private float _verticalOffset = 2f;      // 위로 이동할 최대 높이 (World 단위)

    private Color _originalColor;                           // 텍스트 원래 색상

    private void Awake()
    {
        // TextMeshPro 컴포넌트 자동 할당 (없으면 현재 오브젝트에서 검색)
        if (_damageText == null)
        {
            _damageText = GetComponent<TextMeshPro>();
        }
        _originalColor = _damageText.color;
    }

    /// <summary>
    /// 데미지 문자열을 설정합니다.
    /// </summary>
    /// <param name="text">표시할 데미지 텍스트</param>
    public void SetDamageText(string text)
    {
        _damageText.text = text;
    }

    /// <summary>
    /// 폰트 크기를 설정합니다.
    /// </summary>
    /// <param name="size">폰트 크기 값</param>
    public void SetFontSize(int size)
    {
        if (_damageText != null)
        {
            _damageText.fontSize = size;
        }
    }

    private void Start()
    {
        StartCoroutine(AnimateAndDestroy());
    }

    private void LateUpdate()
    {
        if (Camera.main != null)
        {
            // 카메라를 향하도록 회전 (빌보드 효과)
            Vector3 direction = transform.position - Camera.main.transform.position;
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    /// <summary>
    /// 텍스트를 위로 이동시키며 서서히 페이드 아웃하고, 확대 효과 후 원래 크기로 복원한 뒤 오브젝트를 파괴합니다.
    /// </summary>
    private IEnumerator AnimateAndDestroy()
    {
        float timer = 0f;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.up * _verticalOffset;
        Vector3 startScale = transform.localScale;
        Vector3 punchScale = startScale * 1.2f;  // 초기 확대 효과

        while (timer < _duration)
        {
            timer += Time.deltaTime;
            float t = timer / _duration;

            // 위치 보간 (위로 이동)
            transform.position = Vector3.Lerp(startPos, endPos, t);

            // 색상 알파값 보간 (페이드 아웃)
            Color tempColor = _damageText.color;
            tempColor.a = Mathf.Lerp(_originalColor.a, 0f, t);
            _damageText.color = tempColor;

            // 초반 0.2초 동안 확대 후 원래 크기로 복원
            if (timer < 0.2f)
            {
                transform.localScale = Vector3.Lerp(startScale, punchScale, timer / 0.2f);
            }
            else
            {
                transform.localScale = Vector3.Lerp(punchScale, startScale, (timer - 0.2f) / (_duration - 0.2f));
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
