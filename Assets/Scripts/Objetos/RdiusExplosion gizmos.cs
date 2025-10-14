using UnityEngine;

public class ExplosionGizmo : MonoBehaviour
{
    public ExplosionEffect explosionEffect;

    private void OnDrawGizmosSelected()
    {
        if (explosionEffect == null) return;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f); // naranja semi-transparente
        Gizmos.DrawSphere(transform.position, explosionEffect.radius);
    }
}
