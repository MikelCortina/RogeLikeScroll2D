using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

[RequireComponent(typeof(SpriteShapeController))]
public class DuneGenerator : MonoBehaviour
{
    [Header("Referencias")]
    public SpriteShapeController spriteShapeController;

    [Header("Puntos / tamaño")]
    public int visiblePoints = 20;
    public float duneLength = 0.8f;

    [Header("Altura")]
    public float minHeight = -0.6f;
    public float maxHeight = 0.6f;

    [Header("Perlin / suavizado")]
    public float perlinScale = 0.08f;
    public float perlinOffset = 0f;
    [Range(0f, 1f)] public float heightBlend = 0.9f;

    [Header("Tangentes")]
    public float tangentLengthFactor = 0.8f;

    [Header("Collider runtime (Polygon)")]
    public bool generateRuntimeCollider = true;
    public PolygonCollider2D runtimePolyCollider;
    [Tooltip("Altura del suelo bajo la duna para cerrar el polígono")]
    public float baseHeight = -5f;
    [Tooltip("Cuánto desplazar el collider hacia la cresta (en unidades locales).")]
    public float colliderOffset = 0.25f;

    [Header("Ignorar pequeños escalones")]
    public bool ignoreSmallSteps = true;
    public float stepHeightThreshold = 0.05f;
    public bool flattenSteps = true;
    [Range(1, 11)] public int smoothingWindow = 1;
    public float minSegmentLength = 0.02f;

    private Spline spline;
    private float lastX = 0f;
    private bool initialized = false;
    private Camera mainCamera;

    IEnumerator Start()
    {
        if (spriteShapeController == null)
            spriteShapeController = GetComponent<SpriteShapeController>();

        // esperamos un frame para garantizar que el controller esté listo en editor/runtime
        yield return new WaitForEndOfFrame();

        spline = spriteShapeController.spline;
        if (spline == null)
        {
            Debug.LogError("[DuneGenerator] No se encontró spline.");
            yield break;
        }

        // Limpia puntos existentes
        int existing = spline.GetPointCount();
        for (int i = existing - 1; i >= 0; i--)
            spline.RemovePointAt(i);

        lastX = 0f;

        // Genera puntos iniciales
        for (int i = 0; i < visiblePoints; i++)
        {
            float x = lastX;
            float perlin = Mathf.PerlinNoise((x + perlinOffset) * perlinScale, 0f);
            float y = Mathf.Lerp(minHeight, maxHeight, perlin * heightBlend);
            spline.InsertPointAt(spline.GetPointCount(), new Vector3(x, y, 0f));
            spline.SetTangentMode(spline.GetPointCount() - 1, ShapeTangentMode.Continuous);
            lastX += duneLength;
        }

        UpdateTangentsForAllPoints();

        if (generateRuntimeCollider) EnsureRuntimeCollider();
        RegeneratePolygonCollider();

        // FORZAR actualización visual y de collider del SpriteShapeController
        TryRefreshSpriteShapeController();

        mainCamera = Camera.main;
        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;
        if (mainCamera == null) mainCamera = Camera.main;

        if (mainCamera.transform.position.x + visiblePoints * duneLength * 0.5f > lastX - duneLength)
        {
            AddPointAtEnd();
            if (spline.GetPointCount() > visiblePoints + 5)
            {
                spline.RemovePointAt(0);
                RegeneratePolygonCollider();
            }
        }
    }

    void AddPointAtEnd()
    {
        float x = lastX;
        float perlin = Mathf.PerlinNoise((x + perlinOffset) * perlinScale, 0f);
        float y = Mathf.Lerp(minHeight, maxHeight, perlin * heightBlend);

        spline.InsertPointAt(spline.GetPointCount(), new Vector3(x, y, 0f));
        spline.SetTangentMode(spline.GetPointCount() - 1, ShapeTangentMode.Continuous);
        lastX += duneLength;

        int cnt = spline.GetPointCount();
        for (int i = Mathf.Max(0, cnt - 4); i < cnt; i++)
            UpdateTangentsForPoint(i);

        RegeneratePolygonCollider();

        // FORZAR actualización del SpriteShape después de añadir puntos
        TryRefreshSpriteShapeController();
    }

    void UpdateTangentsForAllPoints()
    {
        for (int i = 0; i < spline.GetPointCount(); i++)
            UpdateTangentsForPoint(i);
    }

    void UpdateTangentsForPoint(int index)
    {
        int cnt = spline.GetPointCount();
        if (cnt < 2) return;

        Vector3 p = spline.GetPosition(index);
        Vector3 prev = index > 0 ? spline.GetPosition(index - 1) : p;
        Vector3 next = index < cnt - 1 ? spline.GetPosition(index + 1) : p;

        Vector3 dir = (next - prev) * 0.5f;
        float tangentLen = duneLength * tangentLengthFactor;
        spline.SetLeftTangent(index, -dir.normalized * tangentLen);
        spline.SetRightTangent(index, dir.normalized * tangentLen);
        spline.SetTangentMode(index, ShapeTangentMode.Continuous);
    }

    void EnsureRuntimeCollider()
    {
        if (runtimePolyCollider == null)
        {
            runtimePolyCollider = GetComponent<PolygonCollider2D>();
            if (runtimePolyCollider == null)
                runtimePolyCollider = gameObject.AddComponent<PolygonCollider2D>();
        }
    }

    public void RegeneratePolygonCollider()
    {
        if (!generateRuntimeCollider || spline == null) return;
        EnsureRuntimeCollider();

        int cnt = spline.GetPointCount();
        if (cnt < 2) return;

        // 1. Cálculo de puntos superiores
        List<Vector2> topPoints = new List<Vector2>();
        for (int i = 0; i < cnt; i++)
        {
            Vector3 p = spline.GetPosition(i);
            Vector3 prev = i > 0 ? spline.GetPosition(i - 1) : p;
            Vector3 next = i < cnt - 1 ? spline.GetPosition(i + 1) : p;

            Vector3 tangent = (next - prev).normalized;
            Vector2 normal = new Vector2(-tangent.y, tangent.x).normalized;
            if (normal.y < 0) normal = -normal;

            Vector2 offsetPoint = (Vector2)p + normal * colliderOffset;
            topPoints.Add(transform.InverseTransformPoint(offsetPoint));
        }

        // 2. Suavizado + filtrado
        List<Vector2> processed = ProcessPointsForCollider(topPoints);

        // 3. Cierra el polígono con la base
        if (processed.Count >= 2)
        {
            Vector2 last = processed[processed.Count - 1];
            Vector2 first = processed[0];
            processed.Add(new Vector2(last.x, baseHeight));
            processed.Add(new Vector2(first.x, baseHeight));
        }

        // 4. Asigna al PolygonCollider2D
        runtimePolyCollider.pathCount = 1;
        runtimePolyCollider.SetPath(0, processed);
    }

    List<Vector2> ProcessPointsForCollider(List<Vector2> topLocal)
    {
        if (topLocal == null || topLocal.Count == 0)
            return new List<Vector2>();

        // Suavizado (media móvil)
        List<Vector2> smooth = new List<Vector2>();
        int w = Mathf.Max(1, smoothingWindow);
        int half = w / 2;

        for (int i = 0; i < topLocal.Count; i++)
        {
            float sumY = 0f;
            int count = 0;
            for (int k = Mathf.Max(0, i - half); k <= Mathf.Min(topLocal.Count - 1, i + half); k++)
            {
                sumY += topLocal[k].y;
                count++;
            }
            float avgY = sumY / count;
            smooth.Add(new Vector2(topLocal[i].x, avgY));
        }

        // Ignora o aplana pequeños escalones
        List<Vector2> filtered = new List<Vector2>();
        filtered.Add(smooth[0]);
        for (int i = 1; i < smooth.Count; i++)
        {
            Vector2 prev = filtered[filtered.Count - 1];
            Vector2 cur = smooth[i];
            float dy = Mathf.Abs(cur.y - prev.y);
            float dx = Mathf.Abs(cur.x - prev.x);

            if (ignoreSmallSteps && dy < stepHeightThreshold && dx <= minSegmentLength)
            {
                if (flattenSteps)
                    filtered.Add(new Vector2(cur.x, prev.y)); // aplana
            }
            else
            {
                filtered.Add(cur);
            }
        }

        return filtered;
    }

    // Forzar refresh/bake del SpriteShapeController (visual y colliders)
    void TryRefreshSpriteShapeController()
    {
        if (spriteShapeController == null) return;

        // Fuerza que se regenere la forma en el siguiente frame si está visible
        spriteShapeController.RefreshSpriteShape();

        // Opcional: actualizar parámetros y bakear malla/collider inmediatamente
        // (BakeMesh devuelve un JobHandle; se ignora aquí por simplicidad)
        try
        {
            spriteShapeController.UpdateSpriteShapeParameters();
            spriteShapeController.BakeMesh();
        }
        catch (System.Exception) { /* métodos pueden faltar en versiones antiguas -> ignorar */ }

        try
        {
            spriteShapeController.BakeCollider();
        }
        catch (System.Exception) { /* idem */ }
    }

    // Debug visual opcional
    void OnDrawGizmosSelected()
    {
        if (runtimePolyCollider != null && runtimePolyCollider.pathCount > 0)
        {
            Gizmos.color = Color.yellow;
            var pts = runtimePolyCollider.GetPath(0);
            for (int i = 0; i < pts.Length - 1; i++)
                Gizmos.DrawLine(transform.TransformPoint(pts[i]), transform.TransformPoint(pts[i + 1]));
        }
    }
}
