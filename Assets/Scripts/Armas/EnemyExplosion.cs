using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Persistent Effects/Enemy Death Explosion")]
public class EnemyDeathExplosionEffect : ScriptableObject, IPersistentEffect, IEffect
{
    [Header("Explosion")]
    public ExplosionEffect explosionEffect;

    [Header("Radius Modifiers")]
    public float radiusMultiplier = 1f;
    public float flatRadiusBonus = 0f;

    [Header("Chain Settings")]
    public float chainDelay = 0.25f;        // tiempo entre explosiones

    private GameObject owner;
    private MonoBehaviour coroutineRunner;

    private Queue<Vector2> pendingExplosions = new Queue<Vector2>();
    private bool isProcessingChain;

    // ================================
    // IPersistentEffect
    // ================================

    public void ApplyTo(GameObject player)
    {
        owner = player;

        // Inicializa el runner usando tu CoroutineRunner
        coroutineRunner = CoroutineRunner.Instance;

        EnemyEvents.OnEnemyDied += OnEnemyDied;
    }

    public void RemoveFrom(GameObject player)
    {
        EnemyEvents.OnEnemyDied -= OnEnemyDied;
        pendingExplosions.Clear();
        isProcessingChain = false;
        owner = null;
    }

    public void ResetRuntime()
    {
        pendingExplosions.Clear();
        isProcessingChain = false;
    }

    // ================================
    // IEffect
    // ================================

    public void Execute(Vector2 position, GameObject owner = null)
    {
        pendingExplosions.Enqueue(position);

        // 🔹 Usa CoroutineRunner global si no se inicializó
        if (coroutineRunner == null)
            coroutineRunner = CoroutineRunner.Instance;

        if (!isProcessingChain)
            coroutineRunner.StartCoroutine(ProcessChain());
    }

    // ================================
    // Internal
    // ================================

    private void OnEnemyDied(EnemyBase enemy)
    {
        if (enemy == null) return;

        Execute(enemy.transform.position, owner);
    }

    private IEnumerator ProcessChain()
    {
        isProcessingChain = true;

        while (pendingExplosions.Count > 0)
        {
            Vector2 pos = pendingExplosions.Dequeue();

            // Ajuste de radio temporal
            float originalRadius = explosionEffect.radius;
            explosionEffect.radius =
                (originalRadius * radiusMultiplier) + flatRadiusBonus;

            explosionEffect.Execute(pos, owner);

            explosionEffect.radius = originalRadius;

            yield return new WaitForSeconds(chainDelay);
        }

        isProcessingChain = false;
    }
}
