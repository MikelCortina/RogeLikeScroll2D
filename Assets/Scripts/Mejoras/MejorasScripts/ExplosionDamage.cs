using UnityEngine;

[CreateAssetMenu(fileName = "DamageExplosionIncreaser", menuName = "Roguelike/Upgrade/DamageExplosion")]
public class DamageExplosion : Upgrade
{
    public float extraDamage;

    public override void Apply(StatsManager statsManager)
    {
        statsManager.AddExplosionDamage(extraDamage);
    }
}
