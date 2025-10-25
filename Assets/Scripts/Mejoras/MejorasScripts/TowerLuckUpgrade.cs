using UnityEngine;

[CreateAssetMenu(fileName = "TowerLuckUpgrade", menuName = "Roguelike/Upgrade/Luck")]
public class TowerLuckUpgrade : Upgrade
{
    public float extraLuck;

    public override void Apply(StatsManager statsManager)
    {
        statsManager.AddTowerLuck(extraLuck);
    }
}

