using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Roguelike/Upgrade")]
public class Upgrade : ScriptableObject
{

    public string upgradeName;
    [TextArea] public string description;

    public UpgradeQuality quality = UpgradeQuality.Rare;

    [Header("Estadísticas que muestra esta mejora (1 a 3)")]
    [Tooltip("Estadísticas que se mostrarán en el UI.")]
    public StatTypeToShow[] displayedStats = new StatTypeToShow[3];

    [Range(1, 3)]
    [Tooltip("Cuántas estadísticas mostrar en el panel.")]
    public int statsToShow = 3;  // <-- Ahora configurable por cada ScriptableObject

    // Convierte las stats a texto para el UI
    public string GetStatsPreview()
    {
        string result = "";
        for (int i = 0; i < Mathf.Min(statsToShow, displayedStats.Length); i++)
        {
            result += "- " + displayedStats[i].ToString() + "\n";
        }
        return result;
    }

    public virtual void Apply(StatsManager statsManager)
    {
        // La lógica concreta va en cada mejora
    }
}

public enum StatTypeToShow
{
    MaxHP,
    MoveForce,
    JumpForce,
    MaxSpeed,
    FireRate,
    GunDamage,
    ExplosionDamage,
    MeleeArmor,
    RangedArmor,
    CriticalChance,
    DodgeChance,
    Knockback,
    ProjectileSpeed,
    XP_Gain,
    Luck,
    Radius,
    CurrencyGain,
    OrganValue,
    Harvester,
    SpawnRate
}

public enum UpgradeQuality
{
    Rare,
    Epic,
    Legendary
}
