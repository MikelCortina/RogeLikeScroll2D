using UnityEngine;

[CreateAssetMenu(fileName = "CurrencyToExplosionDamage", menuName = "Items/CurrencyToExplosionDamage")]
public class CurrencyToExplosionDamage : IObjetos
{
    public override void ApplyEffect()
    {
        base.ApplyEffect();

        if (StatsManager.Instance == null)
        {
            Debug.LogWarning("StatsManager no encontrado. No se puede aplicar CurrencyToExplosionDamage.");
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
        float bonusExplosionDamage = currentCurrency / 2f;

        // Aplicamos el bonus a explosionDamage
        statsManager.AddExplosionDamage(bonusExplosionDamage);

        // Eliminamos todo el currency
        statsManager.SetCurrency(0);

        Debug.Log($"CurrencyToExplosionDamage aplicado: currency sacrificado = {currentCurrency}, explosionDamage += {bonusExplosionDamage}.");
    }
}
