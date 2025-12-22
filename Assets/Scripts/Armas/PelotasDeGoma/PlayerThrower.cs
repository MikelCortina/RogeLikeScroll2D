using UnityEngine;

[RequireComponent(typeof(Transform))]
public class PlayerThrower : MonoBehaviour
{
    [Header("Referencias")]
    public EffectSpawner effectSpawner; // Opcional, asignar en inspector

    [Header("Control de inicio")]
    public bool startOnStart = true;
    public float startDelay = 0f;

    private void Start()
    {
        if (startOnStart)
        {
            Invoke(nameof(StartAutomaticEffects), startDelay);
        }
    }

    /// <summary>
    /// Ejecuta todos los efectos activos automáticamente
    /// </summary>
    private void StartAutomaticEffects()
    {
        foreach (var effectSO in RunEffectManager.Instance.GetActiveEffects())
        {
            if (effectSO == null) continue;

            if (effectSO is IPersistentEffect persistentEffect)
            {
                persistentEffect.ApplyTo(this.gameObject);
            }
            else if (effectSO is IEffect effect)
            {
                // Para efectos normales, podemos pasar la posición del mouse como Vector2
                Vector2 mousePos = Input.mousePosition;
                effect.Execute(mousePos, this.gameObject);
            }
        }
    }

    /// <summary>
    /// Método para activar un efecto nuevo en tiempo real
    /// </summary>
    public void TriggerNewEffect(ScriptableObject newEffect)
    {
        if (newEffect == null) return;

        RunEffectManager.Instance.ActivateEffect(newEffect);

        if (newEffect is IPersistentEffect persistentEffect)
        {
            persistentEffect.ApplyTo(this.gameObject);
        }
        else if (newEffect is IEffect effect)
        {
            Vector2 mousePos = Input.mousePosition;
            effect.Execute(mousePos, this.gameObject);
        }
        else
        {
            Debug.LogWarning($"[PlayerThrower] Effect {newEffect.name} no implementa IEffect ni IPersistentEffect");
        }
    }
}

