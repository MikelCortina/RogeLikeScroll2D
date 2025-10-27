using UnityEngine;

[RequireComponent(typeof(Transform))]
public class HorseThrower : MonoBehaviour
{
    [Header("Referencias")]
    public EffectSpawner effectSpawner; // Asignar en inspector

    [Header("Control de inicio")]
    public bool startOnStart = true;
    public float startDelay = 0f;

    private void Start()
    {
        if (effectSpawner == null)
        {
            Debug.LogWarning("[HorseThrower] EffectSpawner no asignado.");
            return;
        }

        if (startOnStart)
        {
            // Ejecuta los efectos automáticos después de startDelay
            Invoke(nameof(StartAutomaticEffects), startDelay);
        }
    }

    /// <summary>
    /// Ejecuta todos los efectos activos automáticamente.
    /// </summary>
    private void StartAutomaticEffects()
    {


        foreach (var effectSO in RunEffectManager.Instance.GetActiveEffects())
        {
            if (effectSO == null) continue;

            // Si es un efecto persistente
            if (effectSO is IPersistentEffect persistentEffect)
            {
                Debug.Log($"[HorseThrower] Applying persistent effect: {effectSO.name}");
                persistentEffect.ApplyTo(this.gameObject);
            }
            // Si es un efecto normal
            else if (effectSO is IEffect effect)
            {
                Debug.Log($"[HorseThrower] Executing effect: {effectSO.name}");
                effect.Execute(transform.position, this.gameObject);
            }
            else
            {
                Debug.LogWarning($"[HorseThrower] Effect {effectSO.name} no implementa IEffect ni IPersistentEffect");
            }
        }
    }

    /// <summary>
    /// Método para activar un efecto nuevo mid-run, automáticamente.
    /// </summary>
    public void TriggerNewEffect(ScriptableObject newEffect)
    {
        if (effectSpawner == null || newEffect == null) return;

        RunEffectManager.Instance.ActivateEffect(newEffect);

        if (newEffect is IPersistentEffect persistentEffect)
        {
            persistentEffect.ApplyTo(this.gameObject);
        }
        else if (newEffect is IEffect effect)
        {
            effect.Execute(transform.position, this.gameObject);
        }
        else
        {
            Debug.LogWarning($"[HorseThrower] New effect {newEffect.name} no implementa IEffect ni IPersistentEffect");
        }
    }
}
