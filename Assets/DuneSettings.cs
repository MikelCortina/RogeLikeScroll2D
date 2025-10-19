using UnityEngine;
using UnityEngine.U2D;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteShapeController))]
[RequireComponent(typeof(EdgeCollider2D))]
public class InfiniteDunes : MonoBehaviour
{
    [Header("Dune Settings")]
    public int visiblePoints = 30;
    public float segmentWidth = 1f;
    public float maxHeight = 3f;
    public float baseHeight = 2f;
    public float noiseScale = 0.1f;
    public float verticalVariance = 0.5f;

    [Header("Scroll Settings")]
    public Transform player;
    public float spawnBuffer = 10f;

    private SpriteShapeController shapeController;
    private PolygonCollider2D polygonCollider;
    private List<Vector3> topPoints = new List<Vector3>(); // Solo la línea superior
    private float lastX;

    void Start()
    {
        shapeController = GetComponent<SpriteShapeController>();
        polygonCollider = GetComponent<PolygonCollider2D>();
        if (polygonCollider == null)
            polygonCollider = gameObject.AddComponent<PolygonCollider2D>();
        lastX = transform.position.x;

        for (int i = 0; i < visiblePoints; i++)
            AddPoint();

        UpdateShape();
    }

    void Update()
    {
        while (player.position.x + spawnBuffer > lastX)
        {
            AddPoint();
            RemoveOldPoint();
            //UpdateShape();
            UpdatePolygonCollider();
        }
    }

    void AddPoint()
    {
        float noise = Mathf.PerlinNoise(lastX * noiseScale, 0f);
        float y = baseHeight
          + (Mathf.PerlinNoise(lastX * noiseScale, 0f) - 0.5f) * maxHeight
          + (Mathf.PerlinNoise(lastX * noiseScale * 5f, 100f) - 0.5f) * 0.3f;

        topPoints.Add(new Vector3(lastX, y, 0f));
        lastX += segmentWidth;
    }

    void RemoveOldPoint()
    {
        if (topPoints.Count > visiblePoints)
            topPoints.RemoveAt(0);
    }

    void UpdateShape()
    {
        // Crear un polígono cerrado: puntos superiores + puntos en la base
        shapeController.spline.Clear();

        // Agregar puntos superiores
        for (int i = 0; i < topPoints.Count; i++)
        {
            shapeController.spline.InsertPointAt(i, topPoints[i]);
            shapeController.spline.SetTangentMode(i, ShapeTangentMode.Continuous);
        }

        // Agregar puntos inferiores desde derecha a izquierda
        for (int i = topPoints.Count - 1; i >= 0; i--)
        {
            Vector3 basePoint = new Vector3(topPoints[i].x, 0f, 0f); // y=0 es la base
            shapeController.spline.InsertPointAt(shapeController.spline.GetPointCount(), basePoint);
            shapeController.spline.SetTangentMode(shapeController.spline.GetPointCount() - 1, ShapeTangentMode.Continuous);
        }

        // Actualizar EdgeCollider solo con línea superior para físicas
        Vector2[] edgePoints = new Vector2[topPoints.Count];
        for (int i = 0; i < topPoints.Count; i++)
            edgePoints[i] = new Vector2(topPoints[i].x, topPoints[i].y);

        polygonCollider.points = edgePoints;
    }
    void UpdatePolygonCollider()
    {
        List<Vector2> polyPoints = new List<Vector2>();

        // 1️⃣ Agregar la línea superior
        for (int i = 0; i < topPoints.Count; i++)
            polyPoints.Add(new Vector2(topPoints[i].x, topPoints[i].y));

        // 2️⃣ Agregar la línea inferior (la base de la duna) en sentido inverso
        for (int i = topPoints.Count - 1; i >= 0; i--)
            polyPoints.Add(new Vector2(topPoints[i].x, 0f)); // o baseHeight si quieres que no toque 0

        // 3️⃣ Asignar al PolygonCollider
        polygonCollider.SetPath(0, polyPoints.ToArray());
    }
}
