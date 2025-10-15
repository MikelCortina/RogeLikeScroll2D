using UnityEngine;

[CreateAssetMenu(fileName = "CriticalChanceIncreaser", menuName = "Roguelike/Upgrade/CriticalChance")]
public class CriticalChanceIncreaser : Upgrade
{
   public float CriticalChance;
   
   public override void Apply(StatsManager statsManager)
   {
      statsManager.AddCriticalPercentage(CriticalChance);
   }
}
