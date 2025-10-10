using UnityEngine;

[CreateAssetMenu(fileName = "MaxHPIncreaser", menuName = "Roguelike/Upgrade/Increase Max HP")]
public class MaxHPIncreaser : Upgrade
{

    [Header("Aumento de vida máxima")]
    public float hpIncrease = 20f;

    public override void Apply(StatsManager statsManager)
    {
        statsManager.AddMaxHP(hpIncrease);
        Debug.Log($"Aumentada la vida máxima en {hpIncrease} puntos.");
    }
}
