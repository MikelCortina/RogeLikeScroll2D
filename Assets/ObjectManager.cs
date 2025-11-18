using UnityEngine;
using System.Collections.Generic;

public  class ObjectManager : MonoBehaviour
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
        if (allObjects.Count == 0)
            return null;

        List<IObjetos> weightedList = new List<IObjetos>();

        foreach (var obj in allObjects)
        {
            float chance = 0f;
            switch (obj.quality)
            {
                case ObjectQuality.Rare: chance = rareChance; break;
                case ObjectQuality.Epic: chance = epicChance; break;
                case ObjectQuality.Legendary: chance = legendaryChance; break;
            }

            int weight = Mathf.RoundToInt(chance * 100f);
            for (int i = 0; i < weight; i++)
                weightedList.Add(obj);
        }

        if (weightedList.Count == 0)
            return null;

        int index = Random.Range(0, weightedList.Count);
        return weightedList[index]; // ❗ Ya NO elimina nada
    }
}
