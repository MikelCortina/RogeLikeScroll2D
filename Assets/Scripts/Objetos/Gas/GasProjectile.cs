using System.Collections;
using UnityEngine;

/// <summary>
/// Controla el movimiento parabólico desde startPos hasta targetPos en travelTime segundos.
/// Al llegar, espera explosionDelay y genera el gas (gasCloudPrefab).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BombGasProjectile : MonoBehaviour
{
    private GameObject owner;
    private Vector3 startPos;
    private Vector3 targetPos;
    private float travelTime = 1f;
    private float arcHeight = 1f;
    private float explosionDelay = 0f;

    private GameObject gasCloudPrefab;
    private float gasRadius;
    private float gasDuration;
    private float gasDamagePerSecond;

    private bool initialized = false;

    public void Initialize(GameObject owner,
                           Vector3 startPos,
                           Vector3 targetPos,
                           float travelTime,
                           float arcHeight,
                           float explosionDelay,
                           GameObject gasCloudPrefab,
                           float gasRadius,
                           float gasDuration,
                           float gasDamagePerSecond)
    {
        this.owner = owner;
        this.startPos = startPos;
        this.targetPos = targetPos;
        this.travelTime = Mathf.Max(0.01f, travelTime);
        this.arcHeight = arcHeight;
        this.explosionDelay = explosionDelay;
        this.gasCloudPrefab = gasCloudPrefab;
        this.gasRadius = gasRadius;
        this.gasDuration = gasDuration;
        this.gasDamagePerSecond = gasDamagePerSecond;

        transform.position = startPos;
        initialized = true;

        // empezar movimiento
        StartCoroutine(MoveParabola());
    }

    private IEnumerator MoveParabola()
    {
        float elapsed = 0f;
        while (elapsed < travelTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelTime);

            // interpolación lineal + subida parabólica (seno) para pico en mitad del recorrido
            Vector3 basePos = Vector3.Lerp(startPos, targetPos, t);
            float height = Mathf.Sin(t * Mathf.PI) * arcHeight;
            transform.position = basePos + Vector3.up * height;

            yield return null;
        }

        // asegurar posición final exacta
        transform.position = targetPos;

        if (explosionDelay > 0f)
            yield return new WaitForSeconds(explosionDelay);

        Explode();
    }

    private void Explode()
    {
        // crear la nube de gas
        if (gasCloudPrefab != null)
        {
            GameObject cloud = Instantiate(gasCloudPrefab, transform.position, Quaternion.identity);
            GasCloud gc = cloud.GetComponent<GasCloud>();
            if (gc != null)
            {
                gc.Initialize(gasRadius, gasDuration, gasDamagePerSecond, owner);
            }
            else
            {
                Debug.LogWarning("[BombProjectile] gasCloudPrefab no contiene GasCloud.");
            }
        }

        // opcional: efectos visuales/sonido aquí

        Destroy(gameObject);
    }

    // si colisiona con algo antes de llegar (pared), explota
    private void OnTriggerEnter2D(Collider2D other)
    {
        // ignora al owner (si está presente)
        if (owner != null && other.gameObject == owner) return;

        // puedes añadir tags para ignorar trigger con plataformas, etc.
        Explode();
    }
}
