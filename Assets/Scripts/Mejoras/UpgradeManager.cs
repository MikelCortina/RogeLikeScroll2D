using UnityEngine;
using System.Collections.Generic;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    public List<Upgrade> allUpgrades = new List<Upgrade>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }
    public void ShowUpgradeOptions(int count)
    {
        // Escoge aleatoriamente 'count' upgrades
        List<Upgrade> options = new List<Upgrade>();
        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, allUpgrades.Count);
            options.Add(allUpgrades[index]);
        }

        // Aquí abres la UI para que el jugador seleccione uno
       /*Debug.Log("Mostrando opciones de mejoras:");
        foreach (var up in options)
            Debug.Log(up.upgradeName);*/
    }

    public void ApplyUpgrade(Upgrade upgrade)
    {
        upgrade.Apply(StatsManager.Instance);
      //  Debug.Log($"Se aplicó la mejora: {upgrade.upgradeName}");
    }

    public List<Upgrade> GetRandomUpgrades(int count)
    {
        List<Upgrade> options = new List<Upgrade>();
        List<Upgrade> copy = new List<Upgrade>(allUpgrades);

        for (int i = 0; i < count && copy.Count > 0; i++)
        {
            int index = Random.Range(0, copy.Count);
            options.Add(copy[index]);
            copy.RemoveAt(index); // Evita repetidos
        }

        return options;
    }
}
