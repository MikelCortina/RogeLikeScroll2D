using UnityEngine;

[CreateAssetMenu(fileName = "TransferGunToExplosionDamage", menuName = "Items/TransferGunToExplosionDamage")]
public class TransferGunToExplosionDamage : IObjetos
{
    private bool effectApplied = false;

    public override void ApplyEffect()
    {
        base.ApplyEffect();

        if (StatsManager.Instance == null)
        {
            Debug.LogWarning("StatsManager no encontrado. No se puede aplicar TransferGunToExplosionDamage.");
            return;
        }

   
        var stats = StatsManager.Instance.RuntimeStats;

        float amount = stats.gunDamage;

        // Transferimos el daño
        StatsManager.Instance.AddExplosionDamage(amount); // Sumar a explosionDamage
        StatsManager.Instance.AddGunDamage(-amount);       // Dejar gunDamage en 0

    

        Debug.Log($"TransferGunToExplosionDamage aplicado: explosionDamage += {amount}, gunDamage = 0.");
    }
}
