using UnityEngine;

[CreateAssetMenu(fileName = "HealPlayer", menuName = "Roguelike/Upgrade/Heal Player")]
public class HealPlayerUpgrade : Upgrade
{
    [Header("Cantidad a curar")]
    public float healAmount = 25f;

    public override void Apply(StatsManager statsManager)
    {
        statsManager.HealPlayer(healAmount);
        Debug.Log($"Jugador curado {healAmount} puntos de vida.");
    }
}
