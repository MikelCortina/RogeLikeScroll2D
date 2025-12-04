using UnityEngine;

[CreateAssetMenu(menuName = "Projectile Effects/Penetration")]
public class PenetrationEffect : ProjectileEffect
{
    [Header("Número de enemigos que puede atravesar")]
    public int penetrationCount = 1;

    // Puede quedarse vacío si solo es un efecto de datos
    public override void Execute(Vector2 position, GameObject owner = null)
    {
    }
}
