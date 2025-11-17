using UnityEngine;

[CreateAssetMenu(fileName = "DamageEqualizer", menuName = "Items/Damage Equalizer")]
public class DamageEqualizer : IObjetos
{
    [Header("Multiplier")]
    public float multiplier = 1f;

    private bool isApplied = false;

    public void ApplyEffect()
    {
        if (isApplied) return; // Evitar aplicar varias veces
        isApplied = true;

        // Suscribirse al evento OnStatChanged si existiera
        // Como StatsManager no tiene eventos de daño, se puede usar un Coroutine o Update
        StatsManager.Instance.StartCoroutine(MaintainDamage());
    }

    private System.Collections.IEnumerator MaintainDamage()
    {
        var stats = StatsManager.Instance.RuntimeStats;

        while (true) // Mientras dure la run
        {
            float highest = Mathf.Max(stats.gunDamage, stats.explosionDamage) * multiplier;
            stats.gunDamage = highest;
            stats.explosionDamage = highest;

            yield return null; // revisa cada frame
        }
    }
}
