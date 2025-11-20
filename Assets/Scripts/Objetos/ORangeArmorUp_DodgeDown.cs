using UnityEngine;

[CreateAssetMenu(fileName = "DodgeDown_RangeArmorUp", menuName = "Items/DodgeDown_RangeArmorUp")]
public class DodgeDown_RangeArmorUp : IObjetos
{
    private bool effectApplied = false;

    public override void ApplyEffect()
    {
        base.ApplyEffect();

        if (StatsManager.Instance == null)
        {
            Debug.LogWarning("StatsManager no encontrado. No se puede aplicar DodgeDown_RangeArmorUp.");
            return;
        }

      
        var stats = StatsManager.Instance.RuntimeStats;

        // Restar 0.2 de dodgeChance (valor absoluto)
        stats.dodgeChance = Mathf.Max(0, stats.dodgeChance - 0.2f);

        // Sumar 10 de rangedArmorPercentage usando el método oficial
        StatsManager.Instance.AddRangeArmor(10f);

     

        Debug.Log($"DodgeDown_RangeArmorUp aplicado: -0.2 DodgeChance, +10 Ranged Armor.");
    }
}
