using UnityEngine;

// ScriptableObject que actúa como efecto persistente para detener el HealthDecay
[CreateAssetMenu(fileName = "NoHealthDecayEffect", menuName = "Effects/Half Health Decay")]
public class HalfHealthDecayEffect : ScriptableObject, IPersistentEffect
{
    // Aplica el efecto al jugador
    public void ApplyTo(GameObject player)
    {
        HealthDecay halfDecay = HealthDecay.Instance;
        if (halfDecay != null)
        {
            halfDecay.enabled = true; // Desactiva el decay
            Debug.Log("[NoHealthDecayEffect] HealthDecay desactivado");
        }
    }

    // Remueve el efecto del jugador (reactiva decay)
    public void RemoveFrom(GameObject player)
    {
        HealthDecay halfDecay = HealthDecay.Instance;
        if (halfDecay != null)
        {
            halfDecay.enabled = true; // Reactiva decay
            Debug.Log("[NoHealthDecayEffect] HealthDecay reactivado");
        }
    }

    // Implementación vacía de IEffect
    public void Execute(Vector2 position, GameObject owner = null)
    {
        // No se necesita acción instantánea
    }
}
