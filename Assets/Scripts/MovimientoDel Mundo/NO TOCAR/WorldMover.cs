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

    [System.Serializable]
    public class Layer
    {
        public string name = "Layer";
        public Transform[] tiles;              // Mínimo 1 tile, puede ser más
        [Range(0f, 2f)] public float parallaxMultiplier = 1f;
        [HideInInspector] public float tileWidth = 0f;  // calculado en Start
    }

    [Header("Capas (orden: 0 = más cercana al jugador, > = más lejana)")]
    public Layer[] layers;

    // Parent containers que se moverán; si no los asignas en el inspector se crearán en Start()
    [HideInInspector] public Transform[] layerParents;

    private Camera cam;
    private float screenHalfWidthWorld;

    public static ParallaxController Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    /// <summary>
    /// Devuelve la velocidad horizontal (unidades/seg) de la layer indicada.
    /// Si layerIndex es inválido devuelve 0.
    /// </summary>
    public float GetLayerWorldVelocity(int layerIndex)
    {
        if (layers == null || layerIndex < 0 || layerIndex >= layers.Length) return 0f;
        return baseSpeed * layers[layerIndex].parallaxMultiplier;
    }

    /// <summary>
    /// Versión por defecto que devuelve la velocidad del primer layer (índice 0)
    /// o la layer con parallaxMultiplier más cercana a 1 si no se especifica.
    /// </summary>
    public float GetDefaultWorldVelocity()
    {
        if (layers == null || layers.Length == 0) return baseSpeed;
        int best = 0;
        float bestDiff = Mathf.Abs(layers[0].parallaxMultiplier - 1f);
        for (int i = 1; i < layers.Length; i++)
        {
            float d = Mathf.Abs(layers[i].parallaxMultiplier - 1f);
            if (d < bestDiff) { bestDiff = d; best = i; }
        }
        return GetLayerWorldVelocity(best);
    }

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        cam = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null;

        if (cam == null)
            Debug.LogWarning("ParallaxControllerRobust: no hay Camera asignada.");

        screenHalfWidthWorld = cam != null ? cam.orthographicSize * cam.aspect : 10f;

        // Calcula tileWidth para cada layer
        foreach (var layer in layers)
        {
            if (layer.tiles == null || layer.tiles.Length == 0) continue;
            if (layer.tileWidth > 0) continue;

            var t0 = layer.tiles[0];
            if (t0 == null) continue;

            SpriteRenderer sr = t0.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                layer.tileWidth = sr.bounds.size.x;
            }
            else
            {
                BoxCollider2D bc = t0.GetComponent<BoxCollider2D>();
                if (bc != null) layer.tileWidth = Mathf.Abs(bc.size.x * t0.localScale.x);
                else if (layer.tiles.Length >= 2 && layer.tiles[1] != null)
                    layer.tileWidth = Mathf.Abs(layer.tiles[1].position.x - layer.tiles[0].position.x);
                else
                    Debug.LogWarning($"ParallaxControllerRobust: no se pudo calcular tileWidth para layer {layer.name}");
            }
        }

        // Crear/asegurar layerParents
        if (layers != null)
        {
            // Si layerParents es null o de distinto tamaño, crear nuevos padres
            if (layerParents == null || layerParents.Length != layers.Length)
            {
                layerParents = new Transform[layers.Length];
                for (int i = 0; i < layers.Length; i++)
                {
                    GameObject go = new GameObject($"ParallaxLayerParent_{i}_{layers[i].name}");
                    // Opcional: ponerlo como hijo del ParallaxController para mantener la jerarquía ordenada
                    go.transform.SetParent(this.transform, false);
                    layerParents[i] = go.transform;
                }
            }

            // Reparentear tiles a su correspondiente parent (mantener posición mundial)
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].tiles == null) continue;
                foreach (var t in layers[i].tiles)
                {
                    if (t != null)
                    {
                        t.SetParent(layerParents[i], true); // true -> mantener world position
                    }
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (layers == null) return;

        for (int li = 0; li < layers.Length; li++)
        {
            var layer = layers[li];
            if (layer.tiles == null || layer.tiles.Length == 0) continue;

            float moveAmount = baseSpeed * layer.parallaxMultiplier * Time.deltaTime;

            // Mover el parent si existe; si no, mover cada tile individualmente (compatibilidad)
            if (layerParents != null && li >= 0 && li < layerParents.Length && layerParents[li] != null)
            {
                layerParents[li].position += new Vector3(moveAmount, 0f, 0f);
            }
            else
            {
                foreach (var tile in layer.tiles)
                {
                    if (tile != null)
                        tile.position += new Vector3(moveAmount, 0f, 0f);
                }
            }

            // Reposicionamiento robusto (usa positions en world space)
            if (layer.tileWidth <= 0f) continue;

            for (int i = 0; i < layer.tiles.Length; i++)
            {
                var tile = layer.tiles[i];
                if (tile == null) continue;

                float leftThreshold = (cameraTransform != null ? cameraTransform.position.x - screenHalfWidthWorld - layer.tileWidth * 0.5f : -9999f);

                if (baseSpeed < 0 && tile.position.x + layer.tileWidth * 0.5f < leftThreshold)
                {
                    float maxX = float.NegativeInfinity;
                    foreach (var t in layer.tiles)
                        if (t != null && t.position.x > maxX) maxX = t.position.x;

                    tile.position = new Vector3(maxX + layer.tileWidth, tile.position.y, tile.position.z);
                }

                float rightThreshold = (cameraTransform != null ? cameraTransform.position.x + screenHalfWidthWorld + layer.tileWidth * 0.5f : 9999f);

                if (baseSpeed > 0 && tile.position.x - layer.tileWidth * 0.5f > rightThreshold)
                {
                    float minX = float.PositiveInfinity;
                    foreach (var t in layer.tiles)
                        if (t != null && t.position.x < minX) minX = t.position.x;

                    tile.position = new Vector3(minX - layer.tileWidth, tile.position.y, tile.position.z);
                }
            }
        }

        // Mover cámara y colisiones en sentido contrario
        float cameraMove = -baseSpeed * cameraMoveMultiplier * Time.deltaTime;

        if (cameraTransform != null)
            cameraTransform.position += new Vector3(cameraMove, 0f, 0f);

        if (collisionBorders != null)
        {
            foreach (var b in collisionBorders)
                if (b != null)
                    b.position += new Vector3(cameraMove, 0f, 0f);
        }
    }
}
