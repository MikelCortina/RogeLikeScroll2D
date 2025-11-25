using UnityEngine;

public enum StatType
{
    MaxHP, ProjectileSpeed, MoveForce, JumpForce, MaxSpeed, Friction,
    FireRate, Radius, CriticalChance, GunDamage, MeleArmorPercentage,
    RangedArmorPercentage, XpGainMultiplier, DodgeChance, Knockback,
    Currency, OrganValue
}

[System.Serializable]
public class Upgrade : ScriptableObject
{
    public string upgradeName;
    public string description;
    public UpgradeQuality quality;

    [Header("Mejoras que esta Upgrade mostrará (3 máximo)")]
    public StatType[] shownStats = new StatType[3];
}
