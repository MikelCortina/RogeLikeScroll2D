using UnityEngine;


[CreateAssetMenu(fileName = "GunRadiusIncrease", menuName = "Roguelike/Upgrade/GunRadiusIncrease")]
public class GunRadiusIncrease : Upgrade
{
    public float extraRadius;

    public override void Apply(StatsManager statsManager)
    {
        statsManager.AddRadiusToGun(extraRadius);
    }
}
