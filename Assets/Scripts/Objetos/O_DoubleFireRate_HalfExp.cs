using UnityEngine;

[CreateAssetMenu(fileName = "DoubleFireRate_HalfExplosion", menuName = "Items/DoubleFireRate_HalfExplosion")]
public class DoubleFireRate_HalfExplosion : IObjetos
{
    private bool effectApplied = false;

    public override void ApplyEffect()
    {
        base.ApplyEffect();

        if (StatsManager.Instance == null)
        {
            Debug.LogWarning("StatsManager no encontrado. No se puede aplicar DoubleFireRate_HalfExplosion.");
            return;
        }


        var stats = StatsManager.Instance.RuntimeStats;

        // Multiplicamos fireRate por 2
        stats.fireRate *= 2f;

        // Reducimos explosionDamage a la mitad usando método oficial
        StatsManager.Instance.AddExplosionDamage(-stats.explosionDamage / 2f);



        Debug.Log($"DoubleFireRate_HalfExplosion aplicado: fireRate x2 = {stats.fireRate}, explosionDamage = {stats.explosionDamage}.");
    }
}
