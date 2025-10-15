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
        List<Upgrade> options = new List<Upgrade>();
        int attempts = 0;

        while (options.Count < count && attempts < 100)
        {
            attempts++;
            int index = Random.Range(0, allUpgrades.Count);
            Upgrade candidate = allUpgrades[index];

            if (RollSpawnByQuality(candidate.quality))
            {
                options.Add(candidate); // agregamos el ScriptableObject original, no lo modificamos
            }
        }

        // Aquí abres la UI para que el jugador seleccione uno
        /*Debug.Log("Mostrando opciones de mejoras:");
        foreach (var up in options)
            Debug.Log($"{up.upgradeName} ({up.quality})");*/
    }

    public void ApplyUpgrade(Upgrade upgrade)
    {
        upgrade.Apply(StatsManager.Instance);
        // Debug.Log($"Se aplicó la mejora: {upgrade.upgradeName} ({upgrade.quality})");
    }

    // Método que decide si un upgrade spawnea según su calidad
    private bool RollSpawnByQuality(UpgradeQuality quality)
    {
        float roll = Random.value; // entre 0 y 1

        switch (quality)
        {
            case UpgradeQuality.Rare:
                return roll < 0.6f; // 60% de spawn
            case UpgradeQuality.Epic:
                return roll < 0.3f; // 30% de spawn
            case UpgradeQuality.Legendary:
                return roll < 0.1f; // 10% de spawn
            default:
                return false;
        }
    }

    public List<Upgrade> GetRandomUpgrades(int count)
    {
        List<Upgrade> options = new List<Upgrade>();
        int attempts = 0;

        while (options.Count < count && attempts < 200)
        {
            attempts++;
            int index = Random.Range(0, allUpgrades.Count);
            Upgrade candidate = allUpgrades[index];

            if (RollSpawnByQuality(candidate.quality))
            {
                options.Add(candidate);
            }
        }

        return options;
    }
}
