using UnityEngine;

public abstract class ProjectileEffect : ScriptableObject, IEffect
{
    // Método obligatorio de IEffect
    public abstract void Execute(Vector2 position, GameObject owner = null);
}
