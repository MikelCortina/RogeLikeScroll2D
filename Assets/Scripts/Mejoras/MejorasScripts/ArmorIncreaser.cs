using UnityEngine;

[CreateAssetMenu(fileName = "ArmorIncreaser", menuName = "Roguelike/Upgrade/ArmorIncreaser")]
public class ArmorIncreaser : Upgrade
{
    public float extraArmor;

    public override void Apply(StatsManager statsManager)
    {
        statsManager.AddArmor(extraArmor);
    }
}
