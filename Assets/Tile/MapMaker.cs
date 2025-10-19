using UnityEngine;
using UnityEngine.Tilemaps;    

public class MapMaker : MonoBehaviour
{
    public Tilemap tilemap;
    public RuleTile tile;

    public int mapWidth;
    public int mapHeight;

    private int[,] mapData;

    public PerlinData perlinData;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.mapData = this.perlinData.GenerateData(mapWidth, mapHeight);
        GenerateTiles(); 
    }
    void GenerateTiles()
    {
        for (int i = 0; i < mapWidth; i++)
        {
            for (int j = 0; j < mapHeight; j++)
            {
                if (this.mapData[i, j] == 1)
                {
                    this.tilemap.SetTile(
                        new Vector3Int(i, j, 0), this.tile
                        );
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
