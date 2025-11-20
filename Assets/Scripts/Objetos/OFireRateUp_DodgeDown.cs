using UnityEngine;

[CreateAssetMenu(fileName = "FireRateUp_DodgeDown", menuName = "Items/FireRateUp_DodgeDown")]
public class FireRateUp_DodgeDown : IObjetos
{
    private bool effectApplied = false;

    public override void ApplyEffect()
    {
        base.ApplyEffect();

        if (StatsManager.Instance == null)
        {
            Debug.LogWarning("StatsManager no encontrado. No se puede aplicar FireRateUp_DodgeDown.");
            return;
        }


        var stats = StatsManager.Instance.RuntimeStats;

        // Aplicar los cambios
        StatsManager.Instance.AddRadiusToGun(0); // ignorar, solo para evitar warnings

        // +0.5 Fire Rate
        stats.fireRate += 0.5f;

        // -10 Dodge
        stats.dodgeChance = Mathf.Max(0, stats.dodgeChance - 2.5f);

    

        Debug.Log($"FireRateUp_DodgeDown aplicado: +0.5 FireRate, -10 Dodge.");
    }
}
