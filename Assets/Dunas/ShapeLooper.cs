using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mueve una lista de tiles en X (velocidad configurable).
/// Cuando el borde derecho de un tile cruza PontoDeDestino lo reposiciona
/// a la derecha del tile actualmente más a la derecha, creando un bucle infinito.
/// Funciona con 3 o más tiles (también con 2 o incluso 1, aunque 1 no es un loop real).
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

    void Start()
    {
        // Si no hay tiles asignados, busca hijos.
        if (tiles == null) tiles = new List<Transform>();
        if (tiles.Count == 0)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                tiles.Add(transform.GetChild(i));
            }
        }

        if (tiles.Count == 0)
        {
            Debug.LogError("No hay tiles asignados ni hijos para usar como tiles.");
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

        // mover todos los tiles
        for (int i = 0; i < tiles.Count; i++)
            tiles[i].position += new Vector3(dx, 0f, 0f);

        // reposicionar tiles que cruzan PontoDeDestino
        for (int i = 0; i < tiles.Count; i++)
        {
            Transform t = tiles[i];
            float w = GetWidth(t);
            float right = t.position.x + (w / 2f);

            if (right <= PontoDeDestino)
            {
                // buscar el tile actualmente más a la derecha
                float maxRight = float.NegativeInfinity;
                foreach (var tile in tiles)
                {
                    if (tile == t) continue;
                    float tileRight = tile.position.x + GetWidth(tile) / 2f;
                    if (tileRight > maxRight) maxRight = tileRight;
                }

                // colocar el tile reposicionado a la derecha
                float newCenterX = maxRight + spacing + (w / 2f);
                t.position = new Vector3(newCenterX, t.position.y, t.position.z);
            }
        }
    }

    /// <summary>
    /// Intenta obtener el ancho X usando Renderer.bounds; si no hay Renderer devuelve 1f como fallback.
    /// Puedes ajustar el fallback si tus tiles tienen otra escala.
    /// </summary>
    float GetWidth(Transform t)
    {
        if (t == null) return 1f;
        Renderer r = t.GetComponentInChildren<Renderer>();
        if (r != null)
            return r.bounds.size.x;

        // FallBack: si tu tile es un SpriteShapeRenderer (deriva de Renderer) GetComponentInChildren lo encuentra,
        // pero si aún así no hay Renderer devolvemos 1.
        return 1f;
    }

#if UNITY_EDITOR
    // Para visualizar en el editor: dibuja líneas que marquen PontoDeDestino y los bordes de tiles.
   /* void OnDrawGizmosSelected()
    {
        // línea vertical en PontoDeDestino
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(PontoDeDestino, -1000f, 0f), new Vector3(PontoDeDestino, 1000f, 0f));

        if (tiles != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var t in tiles)
            {
                if (t == null) continue;
                float w = GetWidth(t);
                Vector3 left = new Vector3(t.position.x - w / 2f, t.position.y, t.position.z);
                Vector3 right = new Vector3(t.position.x + w / 2f, t.position.y, t.position.z);
                Gizmos.DrawSphere(left, 0.1f);
                Gizmos.DrawSphere(right, 0.1f);
                Gizmos.DrawLine(left + Vector3.up * 0.1f, right + Vector3.up * 0.1f);
            }
        }
    }*/
#endif
}
