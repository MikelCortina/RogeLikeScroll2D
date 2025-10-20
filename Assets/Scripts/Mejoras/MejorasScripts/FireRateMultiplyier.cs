using UnityEngine;

[CreateAssetMenu(fileName = "FireRateMultiplyier", menuName = "Roguelike/Upgrade/Multiply Fire Rate")]
public class FireRateMultiplyier : Upgrade
{
    [Header("Multiplicador de velocidad de disparo")]
    public float fireRateMultiplier = 1.25f;

    public override void Apply(StatsManager statsManager)
    {
        statsManager.RuntimeStats.fireRate *= fireRateMultiplier;
        Debug.Log($"Velocidad de disparo aumentada en un x{fireRateMultiplier}");
    }
}
