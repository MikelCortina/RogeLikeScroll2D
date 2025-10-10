using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileSpeedIncreaser", menuName = "Roguelike/Upgrade/ProjectileSpeed")]
public class ProjectileSpeedUpgrade : Upgrade
{
    public float extraSpeed;

    public override void Apply(StatsManager statsManager)
    {
        statsManager.AddProjectileSpeed(extraSpeed);
    }
}
