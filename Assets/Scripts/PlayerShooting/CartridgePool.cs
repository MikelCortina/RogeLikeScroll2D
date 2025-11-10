using System.Collections.Generic;
using UnityEngine;

public class CartridgePool2D : MonoBehaviour
{
    public static CartridgePool2D Instance;

    public GameObject cartridgePrefab;
    public int initialSize = 20;
    private Queue<GameObject> pool = new Queue<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        for (int i = 0; i < initialSize; i++)
        {
            GameObject g = Instantiate(cartridgePrefab);
            g.SetActive(false);
            pool.Enqueue(g);
        }
    }

    public GameObject Get()
    {
        if (pool.Count > 0)
        {
            GameObject g = pool.Dequeue();
            g.SetActive(true);
            return g;
        }
        else
        {
            return Instantiate(cartridgePrefab);
        }
    }

    public void Return(GameObject g)
    {
        g.SetActive(false);
        pool.Enqueue(g);
    }
}
