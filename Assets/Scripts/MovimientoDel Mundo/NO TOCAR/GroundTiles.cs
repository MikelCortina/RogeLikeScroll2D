using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D; // SpriteShapeController

/// <summary>
/// Spawner que funciona con Sprites normales y SpriteShape (detecta y calcula bounds desde el spline).
/// Alinea bordes para evitar gaps.
/// </summary>
public class GroundTileSpawner_SpriteShape : MonoBehaviour
{
    public Camera targetCamera;
    public Transform layerParent;
    public GameObject[] groundPrefabs;
    public int initialTiles = 6;
    public int poolExtra = 4;
    public float spawnY = 0f;
    public float snapOverlap = -0.001f;
    public bool debugLogs = false;

    private List<GameObject> pool = new List<GameObject>();
    private LinkedList<GameObject> activeChain = new LinkedList<GameObject>();
    private float screenHalfWidthWorld = 10f;

    void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera != null) screenHalfWidthWorld = targetCamera.orthographicSize * targetCamera.aspect;
    }

    void Start()
    {
        if (targetCamera == null) Debug.LogWarning("No Camera asignada ni Camera.main encontrada.");

        int poolSize = Mathf.Max(initialTiles + poolExtra, 4);
        for (int i = 0; i < poolSize; i++)
            pool.Add(InstantiateFromRandomPrefab(inactive: true));

        float cursorLeft = CameraX() - screenHalfWidthWorld;
        float cursorRightTarget = CameraX() + screenHalfWidthWorld + 1f;

        if (initialTiles <= 0) return;

        var first = GetFromPool();
        PlaceTileAlignLeft(first, cursorLeft);
        activeChain.AddLast(first);

        // Rellenar hasta cubrir la cámara + margen
        while (GetLastTileRightEdgeX() < cursorRightTarget && activeChain.Count < initialTiles)
        {
            var next = GetFromPool();
            float leftToPlace = GetLastTileRightEdgeX() + snapOverlap;
            PlaceTileAlignLeft(next, leftToPlace);
            activeChain.AddLast(next);
        }

        for (int i = activeChain.Count; i < initialTiles; i++)
        {
            var next = GetFromPool();
            float leftToPlace = GetLastTileRightEdgeX() + snapOverlap;
            PlaceTileAlignLeft(next, leftToPlace);
            activeChain.AddLast(next);
        }
    }

    void FixedUpdate()
    {
        if (activeChain.Count == 0) return;
        if (targetCamera != null) screenHalfWidthWorld = targetCamera.orthographicSize * targetCamera.aspect;

        float cameraLeft = CameraX() - screenHalfWidthWorld;
        var firstNode = activeChain.First;
        if (firstNode != null)
        {
            var first = firstNode.Value;
            if (first == null) { activeChain.RemoveFirst(); return; }

            float firstRight = GetObjectRightEdgeX(first);
            if (firstRight < cameraLeft - 0.5f)
            {
                float leftToPlace = GetLastTileRightEdgeX() + snapOverlap;
                activeChain.RemoveFirst();
                PlaceTileAlignLeft(first, leftToPlace);
                activeChain.AddLast(first);
                if (debugLogs) Debug.Log($"Recycled to left {leftToPlace:F3} newRight {GetObjectRightEdgeX(first):F3}");
            }
        }
    }

    // ---------- Placement & bounds (con soporte SpriteShape) ----------

    private void PlaceTileAlignLeft(GameObject go, float desiredLeftX)
    {
        if (go == null) return;

        go.transform.SetParent(layerParent != null ? layerParent : this.transform, true);
        bool wasActive = go.activeSelf;
        if (!wasActive) go.SetActive(true); // activar para que componentes se inicialicen

        // Si es SpriteShape, forzamos refresco rápido para que spline/renderer/collider estén actualizados
        var ssc = go.GetComponent<SpriteShapeController>();
        if (ssc != null)
        {
            // Force update: toggle enabled to trigger internal rebuild (no API universal para Bake en todas las versiones)
            ssc.enabled = false;
            ssc.enabled = true;
            // Si existe BakeCollider (present en algunas versiones), llamarlo con try/catch para compatibilidad
            try
            {
                // reflection-safe call por si el método existe
                var mi = typeof(SpriteShapeController).GetMethod("BakeCollider", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (mi != null) mi.Invoke(ssc, null);
            }
            catch { /* no crítico si falla */ }
        }

        // Para medir bounds de forma consistente colocamos temporalmente en x=0 (manteniendo spawnY)
        Vector3 savedPos = go.transform.position;
        go.transform.position = new Vector3(0f, spawnY, savedPos.z);

        Bounds b = CalculateBoundsForGameObject(go);
        float currentLeft = b.min.x;
        float deltaX = desiredLeftX - currentLeft;
        go.transform.position = new Vector3(deltaX, spawnY, savedPos.z);

        if (!wasActive) go.SetActive(true);

        if (debugLogs)
        {
            Bounds nb = CalculateBoundsForGameObject(go);
            Debug.Log($"Placed '{go.name}' left {nb.min.x:F3} right {nb.max.x:F3} center {nb.center.x:F3}");
        }
    }

    // Calcula bounds robustos: si hay SpriteShapeController usa sus puntos del spline (transformados a world).
    private Bounds CalculateBoundsForGameObject(GameObject go)
    {
        // 1) SpriteShapeController -> usar spline points
        var ssc = go.GetComponent<SpriteShapeController>();
        if (ssc != null)
        {
            var spline = ssc.spline;
            if (spline != null)
            {
                int cnt = spline.GetPointCount();
                if (cnt > 0)
                {
                    // inicializar bounds con primer punto transformado
                    Vector3 p0 = ssc.transform.TransformPoint(spline.GetPosition(0));
                    Bounds b = new Bounds(p0, Vector3.zero);
                    for (int i = 1; i < cnt; i++)
                    {
                        Vector3 pi = ssc.transform.TransformPoint(spline.GetPosition(i));
                        b.Encapsulate(pi);
                    }
                    // Los renderers del SpriteShape pueden sobresalir un poco por el sprite; encapsulamos renderers también si existen
                    var rr = go.GetComponentsInChildren<Renderer>();
                    if (rr != null && rr.Length > 0)
                        foreach (var r in rr) b.Encapsulate(r.bounds);
                    return b;
                }
            }
        }

        // 2) Renderers (SpriteRenderer, MeshRenderer...)
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends != null && rends.Length > 0)
        {
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b;
        }

        // 3) Colliders fallback
        var cols = go.GetComponentsInChildren<Collider2D>();
        if (cols != null && cols.Length > 0)
        {
            Bounds b = cols[0].bounds;
            for (int i = 1; i < cols.Length; i++) b.Encapsulate(cols[i].bounds);
            return b;
        }

        // 4) fallback final: un bounds en la posición actual
        return new Bounds(go.transform.position, Vector3.one * 0.1f);
    }

    private float GetLastTileRightEdgeX()
    {
        var last = activeChain.Last;
        if (last == null) return CameraX() + screenHalfWidthWorld;
        return GetObjectRightEdgeX(last.Value);
    }

    private float GetObjectRightEdgeX(GameObject go)
    {
        if (go == null) return CameraX() + screenHalfWidthWorld;
        Bounds b = CalculateBoundsForGameObject(go);
        return b.max.x;
    }

    // ---------- Pool ----------

    private GameObject GetFromPool()
    {
        for (int i = 0; i < pool.Count; i++)
            if (pool[i] != null && !pool[i].activeSelf)
                return pool[i];

        var n = InstantiateFromRandomPrefab(inactive: true);
        pool.Add(n);
        return n;
    }

    private GameObject InstantiateFromRandomPrefab(bool inactive)
    {
        if (groundPrefabs == null || groundPrefabs.Length == 0)
        {
            var placeholder = new GameObject("GroundPlaceholder");
            placeholder.SetActive(!inactive);
            return placeholder;
        }
        var prefab = groundPrefabs[Random.Range(0, groundPrefabs.Length)];
        var inst = Instantiate(prefab, this.transform);
        inst.SetActive(!inactive);
        var rb = inst.GetComponent<Rigidbody2D>();
        if (rb == null) rb = inst.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        return inst;
    }

    private float CameraX() => targetCamera != null ? targetCamera.transform.position.x : 0f;
}
