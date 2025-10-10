using UnityEngine;

[CreateAssetMenu(fileName = "DamagePercentageIncreaser", menuName = "Roguelike/Upgrade/DamagePercentage")]
public class DamagePercentage : Upgrade
{
    public float extraDamage;

    public override void Apply(StatsManager statsManager)
    {
        statsManager.AddDamagePercentage(extraDamage);
    }
}
