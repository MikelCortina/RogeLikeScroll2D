using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class TargetZone : MonoBehaviour
{
    [Header("Viewport Coordinates (0 = left/bottom, 1 = right/top)")]
    [Range(0f, 1f)] public float minX = 0.2f;
    [Range(0f, 1f)] public float minY = 0.2f;
    [Range(0f, 1f)] public float maxX = 0.8f;
    [Range(0f, 1f)] public float maxY = 0.8f;

    [Header("Gizmo Options")]
    public Color gizmoColor = Color.green;

    // Propiedades para usar desde otros scripts
    public Vector2 minViewport => new Vector2(minX, minY);
    public Vector2 maxViewport => new Vector2(maxX, maxY);

    private void OnDrawGizmos()
    {
        if (Camera.main == null) return;

        Vector3 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(minX, minY, Camera.main.nearClipPlane));
        Vector3 topRight = Camera.main.ViewportToWorldPoint(new Vector3(maxX, maxY, Camera.main.nearClipPlane));
        Vector3 topLeft = new Vector3(bottomLeft.x, topRight.y, 0);
        Vector3 bottomRight = new Vector3(topRight.x, bottomLeft.y, 0);

        Gizmos.color = gizmoColor;

        Gizmos.DrawLine(bottomLeft, topLeft);
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
    }

#if UNITY_EDITOR
    // Para hacer más visual la edición en el Scene View
    private void OnDrawGizmosSelected()
    {
        Handles.color = gizmoColor;
        Vector3 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(minX, minY, Camera.main.nearClipPlane));
        Vector3 topRight = Camera.main.ViewportToWorldPoint(new Vector3(maxX, maxY, Camera.main.nearClipPlane));
        Vector3 topLeft = new Vector3(bottomLeft.x, topRight.y, 0);
        Vector3 bottomRight = new Vector3(topRight.x, bottomLeft.y, 0);

        Handles.DrawLine(bottomLeft, topLeft);
        Handles.DrawLine(topLeft, topRight);
        Handles.DrawLine(topRight, bottomRight);
        Handles.DrawLine(bottomRight, bottomLeft);
    }
#endif
}
