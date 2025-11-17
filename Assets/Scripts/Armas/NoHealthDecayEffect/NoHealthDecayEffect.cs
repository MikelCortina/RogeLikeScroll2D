using UnityEngine;

// ScriptableObject que actúa como efecto persistente para detener el HealthDecay
[CreateAssetMenu(fileName = "NoHealthDecayEffect", menuName = "Effects/No Health Decay")]
public class NoHealthDecayEffect : ScriptableObject, IPersistentEffect
{
    // Aplica el efecto al jugador
    public void ApplyTo(GameObject player)
    {
        HealthDecay decay = HealthDecay.Instance;
        if (decay != null)
        {
            decay.enabled = false; // Desactiva el decay
            Debug.Log("[NoHealthDecayEffect] HealthDecay desactivado");
        }
    }

    // Remueve el efecto del jugador (reactiva decay)
    public void RemoveFrom(GameObject player)
    {
        HealthDecay decay = HealthDecay.Instance;
        if (decay != null)
        {
            decay.enabled = true; // Reactiva decay
            Debug.Log("[NoHealthDecayEffect] HealthDecay reactivado");
        }
    }

    // Implementación vacía de IEffect
    public void Execute(Vector2 position, GameObject owner = null)
    {
        // No se necesita acción instantánea
    }
}
