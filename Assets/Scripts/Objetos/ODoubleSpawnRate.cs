using UnityEngine;

[CreateAssetMenu(fileName = "DoubleSpawnRate_HalfFireRate", menuName = "Items/DoubleSpawnRate_HalfFireRate")]
public class DoubleSpawnRate_HalfFireRate : IObjetos
{
    private bool effectApplied = false;

    public override void ApplyEffect()
    {
        base.ApplyEffect();

        if (StatsManager.Instance == null)
        {
            Debug.LogWarning("StatsManager no encontrado. No se puede aplicar DoubleSpawnRate_HalfFireRate.");
            return;
        }

        var stats = StatsManager.Instance.RuntimeStats;

        // Duplicamos spawnRate
        stats.spawnRate *= 2f;

        // Reducimos fireRate a la mitad
        stats.fireRate /= 2f;



        Debug.Log($"DoubleSpawnRate_HalfFireRate aplicado: spawnRate = {stats.spawnRate}, fireRate = {stats.fireRate}.");
    }
}
