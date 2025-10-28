using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Añade pan (arrastrar con click izquierdo) y zoom (rueda) a un RectTransform "content" dentro de un "viewport".
/// - Asume pivot (0.5,0.5) y anchoredPosition (0,0) inicialmente en content.
/// - Clampa la posición para que el contenido no se mueva fuera del viewport.
/// </summary>
[DisallowMultipleComponent]
public class UIPanZoomMap : MonoBehaviour
{
    [Header("References")]
    [Tooltip("RectTransform que actúa como ventana visible (mask).")]
    public RectTransform viewport;

    [Tooltip("RectTransform que contiene el mapa grande (pivot 0.5,0.5).")]
    public RectTransform content;

    [Tooltip("Si tu Canvas es Screen Space - Camera, asigna la cámara aquí; si no, déjalo en null.")]
    public Camera uiCamera;

    [Header("Pan settings")]
    public float panSpeed = 1f; // multiplicador del movimiento del ratón

    [Header("Zoom settings")]
    public float zoomSpeed = 0.1f; // sensibilidad de la rueda
    public float minZoom = 0.5f;
    public float maxZoom = 2.5f;
    public float zoomLerpSpeed = 12f; // suavizado del escalado

    // estado interno
    private bool isDragging = false;
    private Vector2 prevLocalPointerPos;
    private float targetScale = 1f;

    void Start()
    {
        if (viewport == null || content == null)
        {
            Debug.LogError("[UIPanZoomMap] Asigna viewport y content en el inspector.");
            enabled = false;
            return;
        }

        // Inicial
        targetScale = content.localScale.x;
    }

    void Update()
    {
        Vector2 screenMouse = Input.mousePosition;

        // Zoom con rueda cuando el ratón esté sobre el viewport
        if (IsPointerOverViewport(screenMouse))
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                float newTarget = targetScale * (1f + scroll * zoomSpeed * 10f);
                targetScale = Mathf.Clamp(newTarget, minZoom, maxZoom);
            }
        }

        // Suavizar el escalado
        float currentScale = content.localScale.x;
        if (!Mathf.Approximately(currentScale, targetScale))
        {
            float s = Mathf.Lerp(currentScale, targetScale, 1f - Mathf.Exp(-zoomLerpSpeed * Time.unscaledDeltaTime));
            content.localScale = new Vector3(s, s, 1f);
            ClampContentPosition();
        }

        // Inicio de drag: sólo si el ratón está sobre el viewport y botón izquierdo
        if (Input.GetMouseButtonDown(0) && IsPointerOverViewport(screenMouse) && !IsPointerOverUIElementBlocking())
        {
            isDragging = true;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, screenMouse, uiCamera, out prevLocalPointerPos);
        }

        // Fin de drag
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        // Durante drag
        if (isDragging && Input.GetMouseButton(0))
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, screenMouse, uiCamera, out Vector2 localPointerPos))
            {
                Vector2 delta = localPointerPos - prevLocalPointerPos;
                // Convertir delta local (viewport) a movimiento del content considerando escala
                // panSpeed controla la sensibilidad
                Vector2 movement = delta * panSpeed;
                // sumarlo a anchoredPosition (en UI, positivo mueve hacia arriba/derecha)
                content.anchoredPosition += movement;
                prevLocalPointerPos = localPointerPos;
                ClampContentPosition();
            }
        }
    }

    /// <summary>
    /// Comprueba si el puntero está sobre el viewport (en pantalla).
    /// </summary>
    private bool IsPointerOverViewport(Vector2 screenPoint)
    {
        if (viewport == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(viewport, screenPoint, uiCamera);
    }

    /// <summary>
    /// Evita interferir si otra UI (por ejemplo botones) está bajo el cursor y captura eventos.
    /// Si quieres ignorar esta comprobación, devuelve false aquí.
    /// </summary>
    private bool IsPointerOverUIElementBlocking()
    {
        // Si estás usando EventSystem y gráficas, esto evita iniciar drag si el cursor está sobre otro elemento interactivo.
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>
    /// Clamp de anchoredPosition para que no se pueda desplazar el content fuera del viewport.
    /// Requiere pivot (0.5,0.5) en content para que los cálculos sean coherentes.
    /// </summary>
    private void ClampContentPosition()
    {
        // tamaño en píxeles del content escalado
        Vector2 contentSize = Vector2.Scale(content.rect.size, content.localScale);
        Vector2 viewSize = viewport.rect.size;

        Vector2 maxOffset = (contentSize - viewSize) * 0.5f;

        Vector2 ap = content.anchoredPosition;

        if (maxOffset.x <= 0f)
        {
            // content más pequeño que la vista en X -> centrar
            ap.x = 0f;
        }
        else
        {
            ap.x = Mathf.Clamp(ap.x, -maxOffset.x, maxOffset.x);
        }

        if (maxOffset.y <= 0f)
        {
            ap.y = 0f;
        }
        else
        {
            ap.y = Mathf.Clamp(ap.y, -maxOffset.y, maxOffset.y);
        }

        content.anchoredPosition = ap;
    }
}
