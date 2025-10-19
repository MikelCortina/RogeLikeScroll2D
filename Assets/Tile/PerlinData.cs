using UnityEngine;

public class PerlinData : MonoBehaviour
{

    public float fillPercent = 0.15f;

    public float scale = 10f;

    public int[,] GenerateData(int w, int h)
    {
        int[,] mapData = new int[w, h];
        for (int i = 0; i < w; i++)
        {
            for (int j = 0; j < h; j++)
            {
              float heightPercent = Mathf.Pow(1-(float)j/ (float)h,3);

                float value = Mathf.PerlinNoise((float)i / scale, (float)j / scale) - heightPercent;

                mapData[i, j] = (value < this.fillPercent) ? 1 : 0;
            }
           
        }
        return mapData;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
