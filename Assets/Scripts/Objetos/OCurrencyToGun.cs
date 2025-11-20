using UnityEngine;

[CreateAssetMenu(fileName = "CurrencyToGunDamage", menuName = "Items/CurrencyToGunDamage")]
public class CurrencyToGunDamage : IObjetos
{
    public override void ApplyEffect()
    {
        base.ApplyEffect();

        if (StatsManager.Instance == null)
        {
            Debug.LogWarning("StatsManager no encontrado. No se puede aplicar CurrencyToGunDamage.");
            return;
        }

        var statsManager = StatsManager.Instance;
        var stats = statsManager.RuntimeStats;

        int currentCurrency = stats.currency;

        if (currentCurrency <= 0)
        {
            Debug.Log("No tienes currency para sacrificar.");
            return;
        }

        // Calculamos la mitad del currency actual
        float bonusGunDamage = currentCurrency / 2f;

        // Aplicamos el bonus a gunDamage
        statsManager.AddGunDamage(bonusGunDamage);

        // Eliminamos todo el currency
        statsManager.SetCurrency(0);

        Debug.Log($"CurrencyToGunDamage aplicado: currency sacrificado = {currentCurrency}, gunDamage += {bonusGunDamage}.");
    }
}
