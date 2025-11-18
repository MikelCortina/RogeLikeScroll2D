using UnityEngine;

[CreateAssetMenu(fileName = "OrganValueBoost", menuName = "Items/Organ Value Boost")]
public class OrganValueBoost : IObjetos
{
    [Header("Configuración del bonus")]
    public int additionalValue = 1;

    public override void ApplyEffect()
    {
        StatsManager.Instance.RuntimeStats.organValue += additionalValue;
        Debug.Log($"[OrganValueBoost] Organo recogido, añadido {additionalValue} currency. Total ahora: {StatsManager.Instance.RuntimeStats.organValue}");
    }
}