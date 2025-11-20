using UnityEngine;

[CreateAssetMenu(fileName = "TransferExplosionToGunDamage", menuName = "Items/TransferExplosionToGunDamage")]
public class TransferExplosionToGunDamage : IObjetos
{
    private bool effectApplied = false;

    public override void ApplyEffect()
    {
        base.ApplyEffect();

        if (StatsManager.Instance == null)
        {
            Debug.LogWarning("StatsManager no encontrado. No se puede aplicar TransferExplosionToGunDamage.");
            return;
        }

      
        var stats = StatsManager.Instance.RuntimeStats;

        float amount = stats.explosionDamage;

        // Transferimos el daño
        StatsManager.Instance.AddGunDamage(amount);         // Sumar a gunDamage
        StatsManager.Instance.AddExplosionDamage(-amount);  // Dejar explosionDamage en 0

     

        Debug.Log($"TransferExplosionToGunDamage aplicado: gunDamage += {amount}, explosionDamage = 0.");
    }
}
