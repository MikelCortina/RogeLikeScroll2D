using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ParallaxController : MonoBehaviour
{
    [Header("Velocidad base (negativa -> scroll hacia la izquierda)")]
    public float baseSpeed = -2f;

    [Header("Cámara y límites (se moverán en sentido contrario)")]
    public Transform cameraTransform;
    public Transform[] collisionBorders;

    [Header("Multiplicador de movimiento de cámara (1 = misma velocidad que fondo invertido)")]
    public float cameraMoveMultiplier = 1f;

    [Header("Objetos que se moverán con la cámara")]
    public Transform[] objectsToOffset; // enemigos o spawn points

    [Header("Zoom dinámico")]
    public Vector3 cameraStartOffset = new Vector3(0f, 0f, -10f);
    public Vector3 cameraEndOffset = new Vector3(0f, 1f, -10f);
    public float cameraStartSize = 5f;
    public float cameraEndSize = 7f;
    public float cameraTransitionDuration = 3f;

    [System.Serializable]
    public class Layer
    {
        public string name = "Layer";
        public Transform[] tiles;
        [Range(0f, 2f)] public float parallaxMultiplier = 1f;
        [HideInInspector] public float tileWidth = 0f;
    }

    [Header("Capas (orden: 0 = más cercana al jugador, > = más lejana)")]
    public Layer[] layers;

    [HideInInspector] public Transform[] layerParents;

    private Camera cam;
    private float screenHalfWidthWorld;
    private float cameraTransitionElapsed = 0f;
    private float previousHalfHeight;
    private float previousHalfWidth;

    public static ParallaxController Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        cam = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null;

        if (cam == null)
        {
            Debug.LogWarning("ParallaxController: no hay Camera asignada.");
            return;
        }

        screenHalfWidthWorld = cam.orthographicSize * cam.aspect;

        previousHalfHeight = cameraStartSize;
        previousHalfWidth = cameraStartSize * cam.aspect;

        // Calcular tileWidth para cada layer
        foreach (var layer in layers)
        {
            if (layer.tiles == null || layer.tiles.Length == 0) continue;
            if (layer.tileWidth > 0) continue;

            var t0 = layer.tiles[0];
            if (t0 == null) continue;

            SpriteRenderer sr = t0.GetComponent<SpriteRenderer>();
            if (sr != null) layer.tileWidth = sr.bounds.size.x;
            else
            {
                BoxCollider2D bc = t0.GetComponent<BoxCollider2D>();
                if (bc != null) layer.tileWidth = Mathf.Abs(bc.size.x * t0.localScale.x);
                else if (layer.tiles.Length >= 2 && layer.tiles[1] != null)
                    layer.tileWidth = Mathf.Abs(layer.tiles[1].position.x - layer.tiles[0].position.x);
                else
                    Debug.LogWarning($"ParallaxController: no se pudo calcular tileWidth para layer {layer.name}");
            }
        }

        // Crear layerParents si no existen
        if (layers != null)
        {
            if (layerParents == null || layerParents.Length != layers.Length)
            {
                layerParents = new Transform[layers.Length];
                for (int i = 0; i < layers.Length; i++)
                {
                    GameObject go = new GameObject($"ParallaxLayerParent_{i}_{layers[i].name}");
                    go.transform.SetParent(this.transform, false);
                    layerParents[i] = go.transform;
                }
            }

            // Reparentear tiles
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].tiles == null) continue;
                foreach (var t in layers[i].tiles)
                    if (t != null) t.SetParent(layerParents[i], true);
            }
        }
    }

    void FixedUpdate()
    {
        if (layers == null) return;

        // Mover layers
        for (int li = 0; li < layers.Length; li++)
        {
            var layer = layers[li];
            if (layer.tiles == null || layer.tiles.Length == 0) continue;

            float moveAmount = baseSpeed * layer.parallaxMultiplier * Time.deltaTime;

            if (layerParents != null && li >= 0 && li < layerParents.Length && layerParents[li] != null)
                layerParents[li].position += new Vector3(moveAmount, 0f, 0f);
            else
            {
                foreach (var tile in layer.tiles)
                    if (tile != null) tile.position += new Vector3(moveAmount, 0f, 0f);
            }

            // Reposicionamiento robusto
            if (layer.tileWidth <= 0f) continue;

            for (int i = 0; i < layer.tiles.Length; i++)
            {
                var tile = layer.tiles[i];
                if (tile == null) continue;

                float leftThreshold = cameraTransform.position.x - screenHalfWidthWorld - layer.tileWidth * 0.5f;
                float rightThreshold = cameraTransform.position.x + screenHalfWidthWorld + layer.tileWidth * 0.5f;

                if (baseSpeed < 0 && tile.position.x + layer.tileWidth * 0.5f < leftThreshold)
                {
                    float maxX = float.NegativeInfinity;
                    foreach (var t in layer.tiles) if (t != null && t.position.x > maxX) maxX = t.position.x;
                    tile.position = new Vector3(maxX + layer.tileWidth, tile.position.y, tile.position.z);
                }

                if (baseSpeed > 0 && tile.position.x - layer.tileWidth * 0.5f > rightThreshold)
                {
                    float minX = float.PositiveInfinity;
                    foreach (var t in layer.tiles) if (t != null && t.position.x < minX) minX = t.position.x;
                    tile.position = new Vector3(minX - layer.tileWidth, tile.position.y, tile.position.z);
                }
            }
        }

        // Mover cámara horizontalmente
        float cameraMove = -baseSpeed * cameraMoveMultiplier * Time.deltaTime;
        if (cameraTransform != null)
            cameraTransform.position += new Vector3(cameraMove, 0f, 0f);

        // Mover collision borders horizontalmente
        if (collisionBorders != null)
        {
            foreach (var b in collisionBorders)
                if (b != null)
                    b.position += new Vector3(cameraMove, 0f, 0f);
        }
    }

    void LateUpdate()
    {
        if (cam == null || cameraTransform == null) return;

        // Lerp del zoom y posición Y
        cameraTransitionElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(cameraTransitionElapsed / cameraTransitionDuration);

        cam.orthographicSize = Mathf.Lerp(cameraStartSize, cameraEndSize, t);

        Vector3 camPos = cameraTransform.position;
        camPos.y = Mathf.Lerp(cameraStartOffset.y, cameraEndOffset.y, t);
        camPos.z = cameraStartOffset.z;
        cameraTransform.position = camPos;

        // Calcular delta suavizado
        float currentHalfHeight = cam.orthographicSize;
        float currentHalfWidth = cam.orthographicSize * cam.aspect;

        float deltaY = currentHalfHeight - previousHalfHeight;
        float deltaX = currentHalfWidth - previousHalfWidth;

        if (!float.IsNaN(deltaX) && !float.IsNaN(deltaY))
        {
            // Mover collision borders
            if (collisionBorders != null)
            {
                foreach (var b in collisionBorders)
                    if (b != null)
                        b.position += new Vector3(deltaX, deltaY, 0f);
            }

            // Mover objetos spawnables
            if (objectsToOffset != null)
            {
                foreach (var obj in objectsToOffset)
                    if (obj != null)
                        obj.position += new Vector3(deltaX, deltaY, 0f);
            }
        }

        previousHalfHeight = currentHalfHeight;
        previousHalfWidth = currentHalfWidth;
    }
}
