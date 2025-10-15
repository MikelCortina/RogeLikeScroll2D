using UnityEngine;

[CreateAssetMenu(fileName = "DodgeChanceIncreaser", menuName = "Roguelike/Upgrade/Increase Dodge Chance")]

public class DodgeChanceIncreaser :Upgrade
{
    
  
    public float dodgeIncrease ;

    public override void Apply(StatsManager statsManager)
    {
        statsManager.dodgeChanceIncreaser(dodgeIncrease);
        
    }
}
