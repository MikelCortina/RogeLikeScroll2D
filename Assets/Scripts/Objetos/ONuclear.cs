using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "HalfHP_KillAllEnemies", menuName = "Items/HalfHP_KillAllEnemies")]
public class HalfHP_KillAllEnemies : IObjetos
{
    private bool effectApplied = false;

    public override void ApplyEffect()
    {
        base.ApplyEffect();

        if (StatsManager.Instance == null)
        {
            Debug.LogWarning("StatsManager no encontrado. No se puede aplicar HalfHP_KillAllEnemies.");
            return;
        }


        var stats = StatsManager.Instance.RuntimeStats;

        // Reducir vida a la mitad
        stats.currentHP = Mathf.Max(1, stats.currentHP / 2f);
        StatsManager.Instance.NotifyHealthChanged();

        // Encontrar todos los enemigos activos en la escena y llamar a Die()
        var enemies = GameObject.FindObjectsOfType<EnemyBase>();
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.Die();
            }
        }

   

        Debug.Log($"HalfHP_KillAllEnemies aplicado: vida reducida a {stats.currentHP}, {enemies.Length} enemigos eliminados.");
    }
}
