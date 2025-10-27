using UnityEngine;

[CreateAssetMenu(fileName = "RangeDodgeChanceIncreaser", menuName = "Roguelike/Upgrade/Increase  Range Dodge Chance")]

public class RangeDodgeChanceIncreaser : Upgrade
{


    public float dodgeIncrease;

    public override void Apply(StatsManager statsManager)
    {
        statsManager.rangeDodgeChanceIncreaser(dodgeIncrease);

    }
}
