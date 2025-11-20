using UnityEngine;

[CreateAssetMenu(fileName = "UnifiedArmorObject", menuName = "Items/UnifiedArmor")]
public class UnifiedArmorObject : IObjetos
{
    private float unifiedArmor = 0f;

    public override void ApplyEffect()
    {
        base.ApplyEffect();

        if (StatsManager.Instance == null)
        {
            Debug.LogWarning("StatsManager no encontrado. No se puede aplicar UnifiedArmorObject.");
            return;
        }

        var stats = StatsManager.Instance.RuntimeStats;

        // Inicializamos con el mayor valor entre meleArmor y rangedArmor
        unifiedArmor = Mathf.Max(stats.meleArmorPercentage, stats.rangedArmorPercentage);

        // Aplicamos a ambos
        StatsManager.Instance.AddMeleArmor(unifiedArmor - stats.meleArmorPercentage);
        StatsManager.Instance.AddRangeArmor(unifiedArmor - stats.rangedArmorPercentage);

        // Suscribimos a los eventos de StatsManager
        StatsManager.Instance.OnMeleArmorChanged += OnMeleArmorChanged;
        StatsManager.Instance.OnRangedArmorChanged += OnRangeArmorChanged;

        Debug.Log($"UnifiedArmorObject aplicado: Armor unificada = {unifiedArmor}");
    }

    private void OnMeleArmorChanged(float newMeleArmor)
    {
        if (Mathf.Approximately(newMeleArmor, unifiedArmor)) return;

        unifiedArmor = newMeleArmor;
        // Ajustamos el ranged armor para que se mantenga unificado
        StatsManager.Instance.AddRangeArmor(unifiedArmor - StatsManager.Instance.RuntimeStats.rangedArmorPercentage);
    }

    private void OnRangeArmorChanged(float newRangeArmor)
    {
        if (Mathf.Approximately(newRangeArmor, unifiedArmor)) return;

        unifiedArmor = newRangeArmor;
        // Ajustamos el melee armor para que se mantenga unificado
        StatsManager.Instance.AddMeleArmor(unifiedArmor - StatsManager.Instance.RuntimeStats.meleArmorPercentage);
    }

    public void RemoveEffect()
    {
        if (StatsManager.Instance != null)
        {
            // Desuscribimos de los eventos para evitar memory leaks o errores
            StatsManager.Instance.OnMeleArmorChanged -= OnMeleArmorChanged;
            StatsManager.Instance.OnRangedArmorChanged -= OnRangeArmorChanged;
        }
    }
}
