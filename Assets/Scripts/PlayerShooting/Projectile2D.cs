using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile2D : MonoBehaviour
{
    [Header("Visual / Travel")]
    [Tooltip("Si <=0 se calculará travelTime como distance/speed")]
    public float travelTime = 0f;

    [Header("Explosivo (solo para datos en prefab)")]
    public bool isExplosive = false;
    public float explosionRadius = 1.5f;

    [Header("Referencias de efectos")]
    public EffectSpawner effectSpawner;

    [Header("Ignorar capas (solo para VFX si las usas)")]
    public List<string> ignoreLayerNames = new List<string>();

    Coroutine travelCoroutine;
    Rigidbody2D cachedRb;

    private void Awake()
    {
        cachedRb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        // asegurar que el visual está listo
        if (cachedRb != null)
        {
            // mantenemos isKinematic true mientras el visual viaja
            cachedRb.isKinematic = true;
            cachedRb.linearVelocity = Vector2.zero;
            cachedRb.angularVelocity = 0f;
        }
    }

    /// <summary>
    /// Hace la animación visual desde start hasta end.
    /// </summary>
    public void PlayVisual(Vector2 start, Vector2 end, float speed)
    {
        if (travelCoroutine != null) StopCoroutine(travelCoroutine);
        gameObject.SetActive(true);

        float distance = Vector2.Distance(start, end);
        float t = travelTime;
        if (t <= 0f)
        {
            if (speed > 0f) t = distance / speed;
            else t = 0.12f;
        }

        travelCoroutine = StartCoroutine(TravelRoutine(start, end, t));
    }

    IEnumerator TravelRoutine(Vector2 start, Vector2 end, float duration)
    {
        float elapsed = 0f;
        transform.position = start;

        // rotación hacia la dirección
        Vector2 dir = (end - start).normalized;
        if (dir.sqrMagnitude > 0f)
        {
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, ang);
        }

        // asegurar rb kinematic para que la física no lo ancle
        if (cachedRb != null)
        {
            cachedRb.isKinematic = true;
            cachedRb.linearVelocity = Vector2.zero;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector2.Lerp(start, end, a);
            yield return null;
        }

        transform.position = end;

        // reproducir efectos visuales de impacto si existen (los daños ya se aplicaron por AreaShooter2D)
        if (effectSpawner != null && RunEffectManager.Instance != null)
        {
            foreach (var activeEffect in RunEffectManager.Instance.GetActiveEffects())
            {
                if (effectSpawner.effects.Contains(activeEffect))
                {
                    if (activeEffect is IEffect ie)
                        ie.Execute(end, gameObject);
                }
            }
        }

        ReturnToPool();
    }

    public void ReturnToPool()
    {
        if (travelCoroutine != null) StopCoroutine(travelCoroutine);

        // reset física si existe
        if (cachedRb != null)
        {
            cachedRb.linearVelocity = Vector2.zero;
            cachedRb.angularVelocity = 0f;
            cachedRb.isKinematic = true;
        }

        gameObject.SetActive(false);
    }
}
