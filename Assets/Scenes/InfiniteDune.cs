using UnityEngine;
using UnityEngine.U2D;

[RequireComponent(typeof(SpriteShapeController))]
public class InfiniteDunes : MonoBehaviour
{
    public SpriteShapeController spriteShapeController;
    public Transform player;

    [Header("Dune Settings")]
    public int initialPoints = 20;
    public float pointSpacing = 1f;
    public float minHeight = -1f;
    public float maxHeight = 2f;
    public float noiseScale = 0.5f;
    public float generateAheadDistance = 15f;
    public float removeBehindDistance = 10f;

    private Spline spline;
    private float lastX;

    void Start()
    {
        spline = spriteShapeController.spline;
        spline.Clear();
        lastX = 0f;

        // Generamos los puntos iniciales
        for (int i = 0; i < initialPoints; i++)
            AddPoint();

        spriteShapeController.BakeCollider();
    }

    void Update()
    {
        if (player == null || spline == null) return;

        // Generar nuevos puntos adelante
        while (lastX < player.position.x + generateAheadDistance)
            AddPoint();

        // Eliminar puntos atrás
        RemoveOldPoints();

        // Actualizar collider
        spriteShapeController.BakeCollider();
    }

    void AddPoint()
    {
        float y = Mathf.PerlinNoise(lastX * noiseScale, 0f) * (maxHeight - minHeight) + minHeight;
        spline.InsertPointAt(spline.GetPointCount(), new Vector3(lastX, y, 0f));
        spline.SetTangentMode(spline.GetPointCount() - 1, ShapeTangentMode.Continuous);
        lastX += pointSpacing;
    }

    void RemoveOldPoints()
    {
        while (spline.GetPointCount() > 1 && spline.GetPosition(1).x < player.position.x - removeBehindDistance)
        {
            spline.RemovePointAt(0);
        }
    }
}
