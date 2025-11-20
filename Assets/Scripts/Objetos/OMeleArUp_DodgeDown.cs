using UnityEngine;

[CreateAssetMenu(fileName = "DodgeDown_MeleArmorUp", menuName = "Items/DodgeDown_MeleArmorUp")]
public class DodgeDown_MeleArmorUp : IObjetos
{
    private bool effectApplied = false;

    public override void ApplyEffect()
    {
        base.ApplyEffect();

        if (StatsManager.Instance == null)
        {
            Debug.LogWarning("StatsManager no encontrado. No se puede aplicar DodgeDown_MeleArmorUp.");
            return;
        }

        var stats = StatsManager.Instance.RuntimeStats;

        // Restar 0.2 de dodgeChance (valor absoluto)
        stats.dodgeChance = Mathf.Max(0, stats.dodgeChance - 0.2f);

        // Sumar 10 de meleArmorPercentage usando el método oficial
        StatsManager.Instance.AddMeleArmor(10f);

    
        Debug.Log($"DodgeDown_MeleArmorUp aplicado: -0.2 DodgeChance, +10 Melee Armor.");
    }
}
