using UnityEngine;

[CreateAssetMenu(fileName = "UnifiedDamageObject", menuName = "Items/UnifiedDamage")]
public class UnifiedDamageObject : IObjetos
{
    // Mantiene referencia al último valor unificado
    private float unifiedDamage = 0f;

    public override void ApplyEffect()
    {
        base.ApplyEffect();

        if (StatsManager.Instance == null)
        {
            Debug.LogWarning("StatsManager no encontrado. No se puede aplicar UnifiedDamageObject.");
            return;
        }

        // Inicializamos con el valor mayor de ambos daños
        var stats = StatsManager.Instance.RuntimeStats;
        unifiedDamage = Mathf.Max(stats.gunDamage, stats.explosionDamage);

        // Aplicamos a ambos daños
        StatsManager.Instance.AddGunDamage(unifiedDamage - stats.gunDamage);
        StatsManager.Instance.AddExplosionDamage(unifiedDamage - stats.explosionDamage);

        // Nos suscribimos a los cambios para sincronizar ambos daños en tiempo real
        StatsManager.Instance.OnGunDamageChanged += OnGunDamageChanged;
        StatsManager.Instance.OnExplosionDamageChanged += OnExplosionDamageChanged;

        Debug.Log($"UnifiedDamageObject aplicado: daño unificado = {unifiedDamage}");
    }

    private void OnGunDamageChanged(float newGunDamage)
    {
        if (Mathf.Approximately(newGunDamage, unifiedDamage)) return;

        unifiedDamage = newGunDamage;
        StatsManager.Instance.AddExplosionDamage(unifiedDamage - StatsManager.Instance.RuntimeStats.explosionDamage);
    }

    private void OnExplosionDamageChanged(float newExplosionDamage)
    {
        if (Mathf.Approximately(newExplosionDamage, unifiedDamage)) return;

        unifiedDamage = newExplosionDamage;
        StatsManager.Instance.AddGunDamage(unifiedDamage - StatsManager.Instance.RuntimeStats.gunDamage);
    }

    // Opcional: limpieza si quieres que deje de sincronizarse en algún momento
    public void RemoveEffect()
    {
        if (StatsManager.Instance != null)
        {
            StatsManager.Instance.OnGunDamageChanged -= OnGunDamageChanged;
            StatsManager.Instance.OnExplosionDamageChanged -= OnExplosionDamageChanged;
        }
    }
}
