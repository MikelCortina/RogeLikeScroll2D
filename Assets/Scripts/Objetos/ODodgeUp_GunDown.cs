using UnityEngine;

[CreateAssetMenu(fileName = "DodgeUp_GunDamageDown", menuName = "Items/DodgeUp_GunDamageDown")]
public class DodgeUp_GunDamageDown : IObjetos
{
    private bool effectApplied = false;

    public override void ApplyEffect()
    {
        base.ApplyEffect();

        if (StatsManager.Instance == null)
        {
            Debug.LogWarning("StatsManager no encontrado. No se puede aplicar DodgeUp_GunDamageDown.");
            return;
        }

     

        var stats = StatsManager.Instance.RuntimeStats;

        // +0.15 dodge chance (valor absoluto, no porcentaje)
        stats.dodgeChance = Mathf.Max(0, stats.dodgeChance + 0.15f);

        // -5 gun damage (usa el método oficial)
        StatsManager.Instance.AddGunDamage(-5f);


        Debug.Log($"DodgeUp_GunDamageDown aplicado: +0.15 DodgeChance, -5 GunDamage.");
    }
}
