using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Roguelike/Upgrade")]
public class Upgrade : ScriptableObject
{
    public string upgradeName;
    [TextArea] public string description;

    // Método que aplica la mejora al StatsManager
    public virtual void Apply(StatsManager statsManager)
    {
        // Por defecto no hace nada. Lo puedes sobreescribir
    }
}
