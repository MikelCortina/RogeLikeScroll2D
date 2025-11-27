using UnityEngine;
using UnityEngine.EventSystems;

public class SkillTreePanZoom : MonoBehaviour, IScrollHandler, IDragHandler, IBeginDragHandler
{
    [Header("Referencias")]
    public RectTransform content; // El panel que contiene todos los nodos
    public RectTransform viewport; // El área visible (por ejemplo, el panel padre con máscara)

    [Header("Zoom")]
    public float zoomSpeed = 0.1f;
    public float minZoom = 0.5f;
    public float maxZoom = 2.5f;
    public float initialZoom = 1f; // Zoom inicial configurable

    [Header("Movimiento")]
    public float moveSpeed = 1f;
    public Vector2 initialPosition = Vector2.zero; // Posición inicial configurable

    private Vector2 lastMousePos;

    private void Awake()
    {
        if (content == null)
            content = GetComponent<RectTransform>();

        if (viewport == null)
            viewport = content.parent as RectTransform;

        // Aplicar el zoom inicial
        content.localScale = Vector3.one * Mathf.Clamp(initialZoom, minZoom, maxZoom);

        // Aplicar la posición inicial
        content.anchoredPosition = initialPosition;

        ClampToViewport(); // Asegurarse de que la posición inicial esté dentro del viewport
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        lastMousePos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (content == null) return;

        Vector2 delta = eventData.position - lastMousePos;
        content.anchoredPosition += delta * moveSpeed;
        lastMousePos = eventData.position;

        ClampToViewport();
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (content == null) return;

        float scroll = eventData.scrollDelta.y;
        Vector3 scale = content.localScale;
        scale += Vector3.one * (scroll * zoomSpeed);
        scale.x = Mathf.Clamp(scale.x, minZoom, maxZoom);
        scale.y = Mathf.Clamp(scale.y, minZoom, maxZoom);

        content.localScale = scale;

        ClampToViewport();
    }

    private void ClampToViewport()
    {
        if (viewport == null || content == null) return;

        Vector2 viewSize = viewport.rect.size;
        Vector2 contentSize = Vector2.Scale(content.rect.size, content.localScale);

        Vector2 pos = content.anchoredPosition;

        float clampX = Mathf.Max(0, (contentSize.x - viewSize.x) / 2f);
        float clampY = Mathf.Max(0, (contentSize.y - viewSize.y) / 2f);

        pos.x = Mathf.Clamp(pos.x, -clampX, clampX);
        pos.y = Mathf.Clamp(pos.y, -clampY, clampY);

        content.anchoredPosition = pos;
    }

    public void ResetView()
    {
        if (content == null) return;

        // Aplicar zoom y posición inicial
        content.localScale = Vector3.one * Mathf.Clamp(initialZoom, minZoom, maxZoom);
        content.anchoredPosition = initialPosition;

        ClampToViewport();
    }
}
