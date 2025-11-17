using UnityEngine;
using System.Collections.Generic;

public class ObjectManager : MonoBehaviour
{
    public static ObjectManager Instance;

    [Header("Objetos disponibles")]
    public List<IObjetos> allObjects;

    [Header("Probabilidades base por rareza (0-1)")]
    public float rareChance = 0.6f;
    public float epicChance = 0.3f;
    public float legendaryChance = 0.1f;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Devuelve un objeto aleatorio según su rareza.
    /// </summary>
    public IObjetos GetRandomObject()
    {
        List<IObjetos> filtered = new List<IObjetos>();

        // Construimos lista ponderada según rareza
        foreach (var obj in allObjects)
        {
            float chance = 0f;
            switch (obj.quality)
            {
                case ObjectQuality.Rare: chance = rareChance; break;
                case ObjectQuality.Epic: chance = epicChance; break;
                case ObjectQuality.Legendary: chance = legendaryChance; break;
            }

            // Cada objeto entra varias veces según su peso
            int weight = Mathf.RoundToInt(chance * 100);
            for (int i = 0; i < weight; i++)
                filtered.Add(obj);
        }

        if (filtered.Count == 0)
            return null;

        int index = Random.Range(0, filtered.Count);
        return filtered[index];
    }
}
