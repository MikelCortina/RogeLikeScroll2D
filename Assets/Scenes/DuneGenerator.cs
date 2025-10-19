using System.Collections;
using UnityEngine;
using UnityEngine.U2D;

[RequireComponent(typeof(SpriteShapeController))]
public class DuneGenerator : MonoBehaviour
{
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

    private Spline spline;
    private float lastX = 0f;
    private bool initialized = false;
    private Camera mainCamera;

    // Usamos IEnumerator Start para ceder un frame y evitar bloqueos en inicialización
    IEnumerator Start()
    {
        // Intento asignar el componente si no está
        if (spriteShapeController == null) spriteShapeController = GetComponent<SpriteShapeController>();

        // Cede un frame para que Unity tenga tiempo de inicializar internals del SpriteShapeController
        yield return null;

        // Seguridad adicional por si algo no está listo aún
        if (spriteShapeController == null)
        {
            Debug.LogError("[DuneGenerator] No hay SpriteShapeController en este GameObject. Deshabilitando script.");
            enabled = false;
            yield break;
        }

        // Cache de cámara (no llamar Camera.main cada frame)
        mainCamera = Camera.main;

        // Intentamos obtener spline y validarlo
        try
        {
            spline = spriteShapeController.spline;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[DuneGenerator] Excepción al acceder a spriteShapeController.spline: " + ex.Message);
            enabled = false;
            yield break;
        }

        if (spline == null)
        {
            Debug.LogError("[DuneGenerator] spline es null. Comprueba versión del package SpriteShape.");
            enabled = false;
            yield break;
        }

        // Limpia spline de forma segura: no while sin control, borramos por índices con límite
        int existing = spline.GetPointCount();
        int safetyLimit = Mathf.Min(existing, 1000); // no más de 1000 borrados por si acaso
        for (int i = existing - 1; i >= 0 && safetyLimit > 0; i--, safetyLimit--)
        {
            try
            {
                spline.RemovePointAt(i);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[DuneGenerator] Error al eliminar punto " + i + ": " + ex.Message);
            }
        }

        lastX = 0f;

        // Inicializa puntos (mantén esto pequeño para no hacer mucho trabajo en Start)
        int initialToAdd = Mathf.Clamp(visiblePoints, 1, 100);
        for (int i = 0; i < initialToAdd; i++)
        {
            float x = lastX;
            float perlin = Mathf.PerlinNoise((x + perlinOffset) * perlinScale, 0f);
            float y = Mathf.Lerp(minHeight, maxHeight, perlin * heightBlend);

            try
            {
                spline.InsertPointAt(spline.GetPointCount(), new Vector3(x, y, 0f));
                // algunos paquetes antiguos no soportan SetTangentMode en runtime; lo hacemos en try/catch
                spline.SetTangentMode(spline.GetPointCount() - 1, ShapeTangentMode.Continuous);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[DuneGenerator] Error al insertar punto inicial: " + ex.Message);
            }

            lastX += duneLength;
            // Cede ocasionalmente para evitar bloquear (útil si visiblePoints es grande)
            if (i % 20 == 0) yield return null;
        }

        // Actualiza tangentes (poco costoso para <100 puntos)
        UpdateTangentsForAllPoints();

        initialized = true;
        Debug.Log("[DuneGenerator] Inicializado correctamente. Puntos: " + spline.GetPointCount());
    }

    void Update()
    {
        if (!initialized) return;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        // Generar nuevos puntos cuando la cámara se acerca al final
        if (mainCamera.transform.position.x + visiblePoints * duneLength * 0.5f > lastX - duneLength)
        {
            AddPointAtEnd();

            // Elimina puntos antiguos con protección
            try
            {
                if (spline.GetPointCount() > visiblePoints + 5)
                {
                    spline.RemovePointAt(0);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[DuneGenerator] Error al eliminar punto: " + ex.Message);
            }
        }
    }

    void AddPointAtEnd()
    {
        float x = lastX;
        float perlin = Mathf.PerlinNoise((x + perlinOffset) * perlinScale, 0f);
        float y = Mathf.Lerp(minHeight, maxHeight, perlin * heightBlend);
        try
        {
            spline.InsertPointAt(spline.GetPointCount(), new Vector3(x, y, 0f));
            spline.SetTangentMode(spline.GetPointCount() - 1, ShapeTangentMode.Continuous);
            lastX += duneLength;

            int cnt = spline.GetPointCount();
            for (int i = Mathf.Max(0, cnt - 4); i < cnt; i++)
                UpdateTangentsForPoint(i);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[DuneGenerator] Error al añadir punto final: " + ex.Message);
        }
    }

    void UpdateTangentsForAllPoints()
    {
        int cnt = spline.GetPointCount();
        for (int i = 0; i < cnt; i++)
            UpdateTangentsForPoint(i);
    }

    void UpdateTangentsForPoint(int index)
    {
        int cnt = spline.GetPointCount();
        if (cnt < 2) return;
        if (index < 0 || index >= cnt) return;

        Vector3 p = spline.GetPosition(index);
        Vector3 prev = index > 0 ? spline.GetPosition(index - 1) : p;
        Vector3 next = index < cnt - 1 ? spline.GetPosition(index + 1) : p;

        Vector3 dir = (next - prev) * 0.5f;
        float tangentLen = Mathf.Max(0.001f, duneLength * tangentLengthFactor);

        Vector3 leftTangent = -dir.normalized * tangentLen;
        Vector3 rightTangent = dir.normalized * tangentLen;

        try
        {
            spline.SetLeftTangent(index, leftTangent);
            spline.SetRightTangent(index, rightTangent);
            spline.SetTangentMode(index, ShapeTangentMode.Continuous);
        }
        catch (System.Exception)
        {
            // Algunas versiones del package no exponen SetLeft/RightTangent en runtime — ignoramos si falla.
        }
    }
}
