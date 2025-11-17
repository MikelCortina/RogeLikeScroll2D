using UnityEngine;

[CreateAssetMenu(fileName = "OrganValueBoost", menuName = "Items/Organ Value Boost")]
public class OrganValueBoost : IObjetos
{
    [Header("Configuración del bonus")]
    public int additionalValue = 1; // cuánto se suma al recoger un organo

    // Aplica el efecto al StatsManager
    public void ApplyEffect()
    {
        // Incrementa directamente el value de los organs
        StatsManager.Instance.RuntimeStats.organValue = StatsManager.Instance.RuntimeStats.organValue *additionalValue;
        Debug.Log($"[OrganValueBoost] Organo recogido, añadido {additionalValue} currency. Total ahora: {StatsManager.Instance.RuntimeStats.currency}");
    }
}

