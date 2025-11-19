using UnityEngine;

[CreateAssetMenu(fileName = "RangeArmorChanceIncreaser", menuName = "Roguelike/Upgrade/Increase  Range Armor Percentage")]

public class RangeArmorChanceIncreaser : Upgrade
{


    public float armorIncrease;

    public override void Apply(StatsManager statsManager)
    {
        statsManager.AddRangeArmor(armorIncrease);

    }
}
