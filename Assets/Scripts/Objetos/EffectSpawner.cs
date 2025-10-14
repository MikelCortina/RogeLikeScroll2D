using System.Collections.Generic;
using UnityEngine;

public class EffectSpawner : MonoBehaviour
{
    [Tooltip("Lista de efectos a ejecutar.")]
    public List<ScriptableObject> effects = new List<ScriptableObject>();

    public void TriggerEffects(Vector2 position, GameObject owner = null)
    {
        foreach (var effectObj in effects)
        {    
            if (effectObj is IEffect effect)
            {
                effect.Execute(position, owner);
            }
        }
    }
}
