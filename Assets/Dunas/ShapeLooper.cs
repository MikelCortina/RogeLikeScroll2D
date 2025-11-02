using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ShapeLooper mejorado:
/// - Reposiciona tiles cuando el borde derecho cruza PontoDeDestino.
/// - Evita acumulación de error y problemas cuando varios tiles cruzan en el mismo frame.
/// </summary>
public class ShapeLooper : MonoBehaviour
{
    [Header("Tiles")]
    [Tooltip("Lista de tiles (Transform). Si está vacía, se intentarán usar los hijos de este GameObject.")]
    public List<Transform> tiles = new List<Transform>();

    [Header("Movimiento")]
    [Tooltip("Velocidad en unidades/segundo. Usa valor negativo para desplazar a la izquierda.")]
    public float speed = -2f;

    [Header("Puntos")]
    [Tooltip("Si el borde derecho del tile es <= PontoDeDestino se reposiciona.")]
    public float PontoDeDestino = -20f;
    [Tooltip("Valor opcional inicial (se rellena con la posición X de tile[0] si está vacío).")]
    public float PontoOriginal = 0f;

    [Header("Ajustes")]
    [Tooltip("Espacio (unidades) entre tiles al colocarlos uno al lado del otro.")]
    public float spacing = 0f;

    [Header("Seguridad")]
    [Tooltip("Límite de reposiciones en un único frame para evitar loops infinitos en caso de error.")]
    public int maxRepositionsPerFrame = 10;

    // pequeño margen para evitar empates exactos numéricos
    const float epsilon = 0.0001f;

    void Start()
    {
        if (tiles == null) tiles = new List<Transform>();
        if (tiles.Count == 0)
        {
            for (int i = 0; i < transform.childCount; i++)
                tiles.Add(transform.GetChild(i));
        }

        if (tiles.Count == 0)
        {
            Debug.LogError("ShapeLooper: No hay tiles asignados ni hijos para usar como tiles.");
            enabled = false;
            return;
        }

        // Guardar PontoOriginal como la x del primer tile si no se asignó
        PontoOriginal = tiles[0].position.x;

        // Alinea todos los tiles uno detrás de otro empezando por tiles[0]
        for (int i = 1; i < tiles.Count; i++)
        {
            float prevWidth = GetWidth(tiles[i - 1]);
            float currWidth = GetWidth(tiles[i]);
            float newX = tiles[i - 1].position.x + (prevWidth / 2f) + (currWidth / 2f) + spacing;
            tiles[i].position = new Vector3(newX, tiles[i].position.y, tiles[i].position.z);
        }
    }

    void Update()
    {
        if (tiles == null || tiles.Count == 0) return;

        float dx = speed * Time.deltaTime;

        // Mover todos los tiles
        for (int i = 0; i < tiles.Count; i++)
            tiles[i].position += new Vector3(dx, 0f, 0f);

        // Reposicionar: primero detectamos todos los tiles que cruzaron el umbral
        List<Transform> toReposition = new List<Transform>();
        for (int i = 0; i < tiles.Count; i++)
        {
            Transform t = tiles[i];
            if (t == null) continue;
            float w = GetWidth(t);
            float right = t.position.x + (w / 2f);
            if (right <= PontoDeDestino + epsilon)
                toReposition.Add(t);
        }

        // Si no hay ninguno, salir
        if (toReposition.Count == 0) return;

        // Seguridad: limitar el número de reposiciones por frame
        int repCount = 0;

        // Vamos reposicionando uno a uno, pero recalculando el "más a la derecha" en cada paso
        // para evitar colocar mal cuando varios atraviesan en el mismo frame.
        while (toReposition.Count > 0 && repCount < maxRepositionsPerFrame)
        {
            Transform t = toReposition[0];
            toReposition.RemoveAt(0);

            if (t == null) { repCount++; continue; }

            float w = GetWidth(t);

            // calcular el borde derecho actual más a la derecha entre todos los tiles (incluyendo los que ya estaban)
            float maxRight = float.NegativeInfinity;
            foreach (var tile in tiles)
            {
                if (tile == null || tile == t) continue;
                float tileRight = tile.position.x + (GetWidth(tile) / 2f);
                if (tileRight > maxRight) maxRight = tileRight;
            }

            // Si no había otros tiles válidos (caso extremo), usar PontoOriginal como referencia
            if (maxRight == float.NegativeInfinity)
                maxRight = PontoOriginal - (w / 2f);

            // Nueva posición centrada a la derecha del más derecho
            float newCenterX = maxRight + spacing + (w / 2f) + epsilon;

            // Protección: no reposicionar por detrás de PontoOriginal (opcional, evita deriva hacia la izquierda)
            if (newCenterX < PontoOriginal - 0.001f)
                newCenterX = PontoOriginal;

            t.position = new Vector3(newCenterX, t.position.y, t.position.z);

            repCount++;
        }

        if (repCount >= maxRepositionsPerFrame)
        {
            Debug.LogWarning("ShapeLooper: se alcanzó maxRepositionsPerFrame en un frame. Revisa tamaños/velocidad/limites.");
        }
    }

    /// <summary>
    /// Intenta obtener el ancho X usando Renderer.bounds; si no hay Renderer devuelve 1f como fallback.
    /// </summary>
    float GetWidth(Transform t)
    {
        if (t == null) return 1f;
        Renderer r = t.GetComponentInChildren<Renderer>();
        if (r != null)
            return r.bounds.size.x;

        // Fallback: si tu tile es un SpriteShapeRenderer (deriva de Renderer) GetComponentInChildren lo encuentra,
        // pero si aún así no hay Renderer devolvemos 1.
        return 1f;
    }
}
