using UnityEngine;

[CreateAssetMenu(fileName = "KnockbackIncreaser", menuName = "Roguelike/Upgrade/Knockback")]
public class Knockback : Upgrade
{
    public float extraKnockback;

    public override void Apply(StatsManager statsManager)
    {
        statsManager.AddKnockback(extraKnockback);
    }
}
