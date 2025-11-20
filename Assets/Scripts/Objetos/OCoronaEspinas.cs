using UnityEngine;

[CreateAssetMenu(fileName = "CritAndKnockbackBoost", menuName = "Items/CritAndKnockbackBoost")]
public class CritAndKnockbackBoost : IObjetos
{
    private bool effectApplied = false;

    public override void ApplyEffect()
    {
        base.ApplyEffect();

        if (StatsManager.Instance == null)
        {
            Debug.LogWarning("StatsManager no encontrado. No se puede aplicar CritAndKnockbackBoost.");
            return;
        }

        var stats = StatsManager.Instance.RuntimeStats;

        float addedCrit = stats.criticalChance * 0.5f;
        float addedKnockback = stats.knockback * 0.5f;

        stats.criticalChance += addedCrit;
        StatsManager.Instance.AddKnockback(addedKnockback);


        Debug.Log($"CritAndKnockbackBoost aplicado: +50% CritChance (+{addedCrit}), +50% Knockback (+{addedKnockback}).");
    }   
}
