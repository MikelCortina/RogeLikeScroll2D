using UnityEngine;

[CreateAssetMenu(fileName = "MeleArmorIncreaser", menuName = "Roguelike/Upgrade/MeleArmorIncreaser")]
public class MeleArmorIncreaser : Upgrade
{
    public float extraArmor;

    public override void Apply(StatsManager statsManager)
    {
        statsManager.AddMeleArmor(extraArmor);
    }
}
