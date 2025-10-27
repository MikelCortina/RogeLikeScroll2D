using UnityEngine;

[CreateAssetMenu(fileName = "NoDecayOnDamageEffect", menuName = "Effects/No Decay On Damage")]
public class NoDecayOnDamageEffect : ScriptableObject, IPersistentEffect
{
    public void ApplyTo(GameObject player)
    {
        HealthDecay decay = HealthDecay.Instance;
        if (decay != null)
        {
            decay.ResetDecay();
            decay.aceleracion = false;
        }
    }

    public void RemoveFrom(GameObject player)
    {
        HealthDecay decay = HealthDecay.Instance;
        if (decay != null)
        {
            decay.aceleracion = true;
        }
    }

    public void Execute(Vector2 position, GameObject owner = null) { }
}
