using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Roguelike/Upgrade")]
public class Upgrade : ScriptableObject
{
    public string upgradeName;
    [TextArea] public string description;

    // Nueva variable para la calidad de la mejora
    public UpgradeQuality quality = UpgradeQuality.Rare;

    // Método que aplica la mejora al StatsManager
    public virtual void Apply(StatsManager statsManager)
    {
        // Por defecto no hace nada. Lo puedes sobreescribir
    }
}

// Enum para determinar la calidad
public enum UpgradeQuality
{
    Rare,       // rara
    Epic,       // épica
    Legendary   // legendaria
}
