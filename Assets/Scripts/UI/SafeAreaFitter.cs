using UnityEngine;

/// <summary>
/// 부착된 RectTransform의 anchor를 Screen.safeArea에 맞춥니다. (노치/펀치홀 대응 — CLAUDE.md §6)
/// 노치에 걸릴 위험이 있는 콘텐츠(HUD)에만 부착한다 — 전체 화면을 덮어야 하는
/// dim 배경(일시정지/결과 패널)에 부착하면 노치 영역이 비어 보이므로 부착하지 않는다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Rect _appliedSafeArea;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        Apply(Screen.safeArea);
    }

    private void Update()
    {
        // 기기 회전·폴더블 전개 등으로 safeArea가 런타임에 바뀔 수 있다. (struct 비교라 GC 없음)
        if (Screen.safeArea != _appliedSafeArea) Apply(Screen.safeArea);
    }

    private void Apply(Rect safeArea)
    {
        _appliedSafeArea = safeArea;

        // 픽셀 단위 safeArea를 부모 기준 정규화 anchor로 변환한다.
        // (앵커만 옮기면 자식들의 상대 배치가 그대로 따라온다)
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        _rectTransform.anchorMin = anchorMin;
        _rectTransform.anchorMax = anchorMax;
        _rectTransform.offsetMin = Vector2.zero;
        _rectTransform.offsetMax = Vector2.zero;
    }
}
