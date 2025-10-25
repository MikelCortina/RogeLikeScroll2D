using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraZoomAndBounds : MonoBehaviour
{
    [Header("Zoom y movimiento")]
    public Vector3 startOffset = new Vector3(0.21f, -0.2f, -10f);
    public Vector3 endOffset = new Vector3(0.21f, 0.5f, -10f);
    public float startSize = 1.5f;
    public float endSize =2.5f;
    public float duration = 3f;

    [Header("Bordes")]
    public float thickness = 1f;
    public string wallTag = "Boundary";
    public string wallLayerName = "CameraBounds";

    private float elapsed = 0f;
    private Camera cam;
    private Transform boundsParent;
    private BoxCollider2D[] walls; // Top, Bottom, Left, Right

    void Start()
    {
        cam = GetComponent<Camera>();
        if (!cam.orthographic)
        {
            Debug.LogError("CameraZoomAndBounds solo funciona con cámaras ortográficas.");
            return;
        }

        // Crear layer
        int wallLayer = LayerMask.NameToLayer(wallLayerName);
        if (wallLayer == -1) wallLayer = 0;

        // Crear objeto padre de bordes
        boundsParent = new GameObject("CameraColliders").transform;
        boundsParent.SetParent(transform);

        walls = new BoxCollider2D[4]; // Top, Bottom, Left, Right
        for (int i = 0; i < 4; i++)
        {
            GameObject w = new GameObject("Wall");
            w.transform.parent = boundsParent;
            w.tag = wallTag;
            w.layer = wallLayer;
            walls[i] = w.AddComponent<BoxCollider2D>();
        }

        UpdateBounds(); // Posición inicial
    }

    void LateUpdate()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        // Interpolar tamaño
        cam.orthographicSize = Mathf.Lerp(startSize, endSize, t);

        // Interpolar offset Y
        Vector3 pos = transform.position;
        pos.y = Mathf.Lerp(startOffset.y, endOffset.y, t);
        transform.position = new Vector3(pos.x, pos.y, startOffset.z); // mantener Z constante

        // Actualizar bordes
        UpdateBounds();
    }

    void UpdateBounds()
    {
        float height = 2f * cam.orthographicSize;
        float width = height * cam.aspect;
        Vector3 camPos = transform.position;

        // Top
        walls[0].transform.position = camPos + Vector3.up * (height / 2 + thickness / 2);
        walls[0].size = new Vector2(width, thickness);

        // Bottom
        walls[1].transform.position = camPos + Vector3.down * (height / 2 + thickness / 2);
        walls[1].size = new Vector2(width, thickness);

        // Left
        walls[2].transform.position = camPos + Vector3.left * (width / 2 + thickness / 2);
        walls[2].size = new Vector2(thickness, height);

        // Right
        walls[3].transform.position = camPos + Vector3.right * (width / 2 + thickness / 2);
        walls[3].size = new Vector2(thickness, height);
    }
}
