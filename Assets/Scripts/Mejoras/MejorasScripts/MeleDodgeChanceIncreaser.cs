using UnityEngine;

[CreateAssetMenu(fileName = "MeleDodgeChanceIncreaser", menuName = "Roguelike/Upgrade/Increase  Mele Dodge Chance")]

public class MeleDodgeChanceIncreaser :Upgrade
{
    
  
    public float dodgeIncrease ;

    public override void Apply(StatsManager statsManager)
    {
        statsManager.meleDodgeChanceIncreaser(dodgeIncrease);
        
    }
}
