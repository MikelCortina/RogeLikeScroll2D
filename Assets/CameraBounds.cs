using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraBounds : MonoBehaviour
{
    [Header("Collider Settings")]
    public float thickness = 1f;               // Grosor del borde
    public string wallTag = "Boundary";        // Tag para identificar las paredes
    public string wallLayerName = "CameraBounds"; // Nombre del layer

    private void Start()
    {
        Camera cam = GetComponent<Camera>();
        if (!cam.orthographic)
        {
            Debug.LogError("CameraBounds solo funciona con cámaras ortográficas.");
            return;
        }

        // Crear layer si no existe
        int wallLayer = LayerMask.NameToLayer(wallLayerName);
        if (wallLayer == -1)
        {
            Debug.LogWarning($"⚠️ El layer '{wallLayerName}' no existe. Crea uno en Edit > Project Settings > Tags and Layers.");
            wallLayer = 0; // Default
        }

        float height = 2f * cam.orthographicSize;
        float width = height * cam.aspect;
        Vector2 camPos = cam.transform.position;

        GameObject boundsParent = new GameObject("CameraColliders");
        boundsParent.transform.parent = cam.transform;

        // Crear colliders en cada lado
        CreateWall(boundsParent.transform, new Vector2(camPos.x, camPos.y + height / 2 + thickness / 2), new Vector2(width, thickness), wallTag, wallLayer); // Top
        CreateWall(boundsParent.transform, new Vector2(camPos.x, camPos.y - height / 2 - thickness / 2), new Vector2(width, thickness), wallTag, wallLayer); // Bottom
        CreateWall(boundsParent.transform, new Vector2(camPos.x - width / 2 - thickness / 2, camPos.y), new Vector2(thickness, height), wallTag, wallLayer); // Left
        CreateWall(boundsParent.transform, new Vector2(camPos.x + width / 2 + thickness / 2, camPos.y), new Vector2(thickness, height), wallTag, wallLayer); // Right
    }

    private void CreateWall(Transform parent, Vector2 pos, Vector2 size, string tagName, int layer)
    {
        GameObject wall = new GameObject("Wall");
        wall.transform.parent = parent;
        wall.transform.position = pos;
        wall.tag = tagName;
        wall.layer = layer;

        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
        col.size = size;
        col.isTrigger = false; // true si quieres que el jugador solo lo detecte con eventos y no se bloquee
    }
}
