using System.Collections.Generic;
using UnityEngine;

public class InfiniteGroundDynamicFixed : MonoBehaviour
{
    [Header("Configuración del suelo")]
    public List<GameObject> groundPrefabs; // prefabs (pueden tener anchos distintos)
    public float speed = 5f;               // velocidad de desplazamiento (izquierda)
    public Transform startPoint;           // punto de referencia X donde empieza el suelo
    public float spawnBuffer = 2f;         // distancia extra delante del startPoint para decidir spawn
    public float destroyBuffer = 20f;      // distancia detrás del startPoint para destruir tiles

    private List<GameObject> groundTiles = new List<GameObject>();
    private int nextPrefabIndex = 0;

    void Start()
    {
        if (groundPrefabs == null || groundPrefabs.Count == 0)
        {
            Debug.LogError("No hay prefabs asignados en groundPrefabs!");
            enabled = false;
            return;
        }

        // Instanciamos el primer tile justo en startPoint
        SpawnNextTileAtPosition(startPoint.position.x);
    }

    void Update()
    {
        // Movemos todos los tiles
        for (int i = 0; i < groundTiles.Count; i++)
        {
            if (groundTiles[i] != null)
                groundTiles[i].transform.position += Vector3.left * speed * Time.deltaTime;
        }

        // Si no hay tiles, no intentamos acceder a [0] ni al último
        if (groundTiles.Count == 0) return;

        // Comprobamos el último tile para saber si debemos generar uno nuevo
        GameObject lastTile = groundTiles[groundTiles.Count - 1];
        float lastTileRightX = GetRightEdgeX(lastTile);
        // Si el extremo derecho del último tile está por detrás de startPoint.x + spawnBuffer => generamos
        if (lastTileRightX < startPoint.position.x + spawnBuffer)
        {
            SpawnNextTileAtPosition(lastTileRightX);
        }

        // Comprobamos el primer tile para destruirlo si ya está muy a la izquierda
        GameObject firstTile = groundTiles[0];
        float firstTileRightEdge = GetRightEdgeX(firstTile);
        if (firstTileRightEdge < startPoint.position.x - destroyBuffer)
        {
            Destroy(firstTile);
            groundTiles.RemoveAt(0);
        }
    }

    // Genera un nuevo tile posicionándolo de forma que su borde izquierdo esté justo donde le pases (xStart).
    private void SpawnNextTileAtPosition(float xStart)
    {
        GameObject prefab = groundPrefabs[nextPrefabIndex % groundPrefabs.Count];
        nextPrefabIndex++;

        GameObject tile = Instantiate(prefab, transform);
        float tileWidth = GetWidth(tile);

        // Colocamos el tile de modo que su borde izquierdo quede en xStart
        // Para ello la posición.x será xStart + tileWidth/2 (centro del sprite)
        Vector3 pos = new Vector3(xStart + tileWidth * 0.5f, startPoint.position.y, 0f);
        tile.transform.position = pos;

        groundTiles.Add(tile);
    }

    // Devuelve el ancho del tile (busca SpriteRenderer o Collider2D), fallback = 1
    private float GetWidth(GameObject go)
    {
        if (go == null) return 1f;

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) return sr.bounds.size.x;

        // Intentamos buscar en hijos
        sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) return sr.bounds.size.x;

        Collider2D col = go.GetComponent<Collider2D>();
        if (col != null) return col.bounds.size.x;

        col = go.GetComponentInChildren<Collider2D>();
        if (col != null) return col.bounds.size.x;

        // fallback
        return 1f;
    }

    // Devuelve la X del borde derecho del objeto (centroX + width/2)
    private float GetRightEdgeX(GameObject go)
    {
        if (go == null) return float.NegativeInfinity;
        float width = GetWidth(go);
        return go.transform.position.x + width * 0.5f;
    }
}
