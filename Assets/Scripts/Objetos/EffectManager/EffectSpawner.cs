using System.Collections.Generic;
using UnityEngine;

public class EffectSpawner : MonoBehaviour
{
    [Tooltip("Lista de efectos a ejecutar.")]
    public List<ScriptableObject> effects = new List<ScriptableObject>();

    public void TriggerEffect(ScriptableObject effect, Vector2 position, GameObject owner = null)
    {
        if (effect is IEffect e)
        {
            e.Execute(position, owner);
        }
    }
}
