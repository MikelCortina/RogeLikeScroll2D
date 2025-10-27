using UnityEngine;

[CreateAssetMenu(fileName = "BaseDamageIncreaser", menuName = "Roguelike/Upgrade/BaseDamage")]
public class BaseDamageIncreaser : Upgrade
{
    public float extraDamage;

    public override void Apply(StatsManager statsManager)
    {
        statsManager.AddGunDamage(extraDamage);
    }
}
