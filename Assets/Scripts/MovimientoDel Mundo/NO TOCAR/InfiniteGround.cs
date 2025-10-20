using System.Collections.Generic;
using UnityEngine;

public class InfiniteGroundDynamic : MonoBehaviour
{
    [Header("Configuración del suelo")]
    public List<GameObject> groundPrefabs; // Lista de prefabs de suelo
    public float speed = 5f; // Velocidad de desplazamiento
    public Transform startPoint; // Punto donde empieza el suelo

    private List<GameObject> groundTiles = new List<GameObject>();
    private float lastTileEndX; // Posición X del final del último tile

    void Start()
    {
        if (groundPrefabs.Count == 0)
        {
            Debug.LogError("No hay prefabs de suelo asignados!");
            return;
        }

        lastTileEndX = startPoint.position.x;
        SpawnNextTile(); // Instanciamos el primer tile
    }

    void Update()
    {
        // Movemos todos los tiles
        for (int i = 0; i < groundTiles.Count; i++)
        {
            groundTiles[i].transform.position += Vector3.left * speed * Time.deltaTime;
        }

        // Verificamos si el último tile está lo suficientemente cerca para generar uno nuevo
        GameObject lastTile = groundTiles[groundTiles.Count - 1];
        SpriteRenderer sr = lastTile.GetComponent<SpriteRenderer>();
        float tileWidth = sr.bounds.size.x;

        if (lastTile.transform.position.x + tileWidth / 2 < lastTileEndX)
        {
            SpawnNextTile();
        }

        // Eliminamos tiles que ya están fuera de la cámara
        GameObject firstTile = groundTiles[0];
        if (firstTile.transform.position.x + tileWidth < startPoint.position.x - 20f) // margen opcional
        {
            Destroy(firstTile);
            groundTiles.RemoveAt(0);
        }
    }

    void SpawnNextTile()
    {
        // Elegimos el prefab siguiente (puede alternar o aleatorio)
        GameObject prefab = groundPrefabs[groundTiles.Count % groundPrefabs.Count];

        // Instanciamos el tile justo después del último
        GameObject tile = Instantiate(prefab);
        SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
        float tileWidth = sr.bounds.size.x;

        tile.transform.position = new Vector3(lastTileEndX + tileWidth / 2, startPoint.position.y, 0);

        lastTileEndX = tile.transform.position.x + tileWidth / 2;
        groundTiles.Add(tile);
    }
}
