using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileDamageIncreaser", menuName = "Roguelike/Upgrade/ProjectileDamage")]
public class ProjectileDamageUpgrade : Upgrade
{
    public float extraDamage;

    public override void Apply(StatsManager statsManager)
    {
        statsManager.AddbaseDamage(extraDamage);
    }
}
