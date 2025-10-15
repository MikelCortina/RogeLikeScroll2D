using UnityEngine;

[CreateAssetMenu(fileName = "XpGainMultiplier", menuName = "Roguelike/Upgrade/GainMultiplier")]
public class XpGainMultiplier : Upgrade
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
   
    public float xpIncrease ;

    public override void Apply(StatsManager statsManager)
    {
        statsManager.XpGainMultiplier(xpIncrease);
        
    }
}
