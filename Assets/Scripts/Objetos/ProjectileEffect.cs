using UnityEngine;

public abstract class ProjectileEffect : ScriptableObject, IEffect
{
    // M�todo obligatorio de IEffect
    public abstract void Execute(Vector2 position, GameObject owner = null);
}
