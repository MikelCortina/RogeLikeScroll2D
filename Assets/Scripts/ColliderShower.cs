using UnityEngine;

public class DrawColliderGizmoIngame : MonoBehaviour
{
    public Color gizmoColor = Color.green;

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return; // Solo en modo Play

        Gizmos.color = gizmoColor;

        // BOX COLLIDER 2D
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Matrix4x4 rotMatrix = Matrix4x4.TRS(
                box.transform.position + (Vector3)box.offset,
                box.transform.rotation,
                Vector3.one
            );

            Gizmos.matrix = rotMatrix;
            Gizmos.DrawWireCube(Vector3.zero, box.size);
        }

        // CIRCLE COLLIDER 2D
        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        if (circle != null)
        {
            Gizmos.matrix = Matrix4x4.TRS(
                circle.transform.position + (Vector3)circle.offset,
                circle.transform.rotation,
                Vector3.one
            );

            Gizmos.DrawWireSphere(Vector3.zero, circle.radius);
        }

        // POLYGON COLLIDER 2D
        PolygonCollider2D poly = GetComponent<PolygonCollider2D>();
        if (poly != null)
        {
            Gizmos.matrix = Matrix4x4.TRS(
                poly.transform.position,
                poly.transform.rotation,
                Vector3.one
            );

            for (int p = 0; p < poly.pathCount; p++)
            {
                Vector2[] path = poly.GetPath(p);

                for (int i = 0; i < path.Length; i++)
                {
                    Vector3 a = (Vector3)(path[i] + poly.offset);
                    Vector3 b = (Vector3)(path[(i + 1) % path.Length] + poly.offset);
                    Gizmos.DrawLine(a, b);
                }
            }
        }
    }
}
