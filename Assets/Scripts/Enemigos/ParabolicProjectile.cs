using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class ParabolicProjectile : MonoBehaviour
{
    private Vector3 startPos;
    private Vector3 endPos;
    private Vector3 controlPoint; // control para Bezier (apex)
    private float duration = 1f;
    private float elapsed = 0f;
    private float damage = 10f;
    private LayerMask playerLayer;
    private float hitRadius = 0.25f; // radio para OverlapCircle
    private bool initialized = false;

    // Inicializa el proyectil. Llamar justo después de instanciar.
    public void Initialize(Vector3 start, Vector3 end, float arcHeight, float travelDuration, float damageAmount, LayerMask playerLayerMask)
    {
        startPos = start;
        endPos = end;
        duration = Mathf.Max(0.05f, travelDuration);
        damage = damageAmount;
        playerLayer = playerLayerMask;

        // control point en el punto medio, elevado por arcHeight
        Vector3 mid = (start + end) * 0.5f;
        controlPoint = mid + Vector3.up * Mathf.Abs(arcHeight);

        elapsed = 0f;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized) return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        // Bezier cuadrático: B(t) = (1-t)^2 * P0 + 2(1-t)t * P1 + t^2 * P2
        float u = 1f - t;
        Vector3 pos = u * u * startPos + 2f * u * t * controlPoint + t * t * endPos;
        transform.position = pos;

        // chequeo de colisión con jugador (puedes ajustar método según tu estructura)
        Collider2D hit = Physics2D.OverlapCircle(transform.position, hitRadius, playerLayer);
        if (hit != null)
        {
            var playerHealth = hit.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeMeleDamage(damage); // o usa otro método si prefieres
            }
            Explode();
            return;
        }

        // al finalizar el viaje, destruir
        if (t >= 1f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        // aquí podrías reproducir VFX, sonido, partículas, etc.
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        // dibuja la curva en editor si está inicializado (útil para debugging)
        if (!initialized) return;
        Gizmos.color = Color.yellow;
        Vector3 prev = startPos;
        int steps = 12;
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            float u = 1f - t;
            Vector3 p = u * u * startPos + 2f * u * t * controlPoint + t * t * endPos;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }
}
