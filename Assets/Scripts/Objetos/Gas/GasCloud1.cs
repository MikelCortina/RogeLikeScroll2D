using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class GasCloud : MonoBehaviour
{
    [Header("Ajustes (si se inicializa, algunos campos serán sobreescritos)")]
    public float radius = 2.5f;            // fallback si no hay sprite
    public float duration = 5f;

    [Header("Daño por ticks (se mantiene la lógica clásica)")]
    [Tooltip("Cada cuánto tiempo (segundos) se aplica un 'tick' de daño.")]
    public float tickInterval = 0.5f;
    [Tooltip("Cantidad de daño por tick. Se calcula desde DPS si se usa Initialize.")]
    public float damagePerTick = 2f;

    [Header("Comportamiento")]
    [Tooltip("Si true, los waits usarán real time (ignoran timeScale).")]
    public bool useRealtimeForTicks = false;
    [Tooltip("Filtra por capas para optimizar los triggers (opcional).")]
    public LayerMask enemyLayerMask = ~0;

    private CircleCollider2D circle;
    private HashSet<GameObject> inside = new HashSet<GameObject>();
    private GameObject owner;

    private Coroutine lifeCoroutine;
    private Coroutine tickCoroutine;

    private void Awake()
    {
        circle = GetComponent<CircleCollider2D>();
        if (circle == null) circle = gameObject.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = radius;
    }

    /// <summary>
    /// Inicializa la nube.
    /// gasRadius: valor por defecto (puede ser sobreescrito por sprite del prefab)
    /// damagePerSecond: DPS que será convertido a damagePerTick = DPS * tickInterval
    /// </summary>
    public void Initialize(float gasRadius, float duration, float damagePerSecond, GameObject owner)
    {
        this.owner = owner;
        this.duration = duration;

        // calcular damagePerTick desde DPS (si el usuario pasó DPS)
        this.damagePerTick = damagePerSecond * tickInterval;

        // si hay SpriteRenderer en este GameObject o en hijos, tomar bounds para radius
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();

        if (sr != null && sr.sprite != null)
        {
            Vector3 extents = sr.bounds.extents; // world-space semi-ejes del sprite
            float spriteRadius = Mathf.Max(extents.x, extents.y);
            // si spriteRadius es 0 (por alguna razón), usar gasRadius fallback
            this.radius = spriteRadius > 0f ? spriteRadius : (gasRadius > 0f ? gasRadius : this.radius);
        }
        else
        {
            this.radius = gasRadius > 0f ? gasRadius : this.radius;
        }

        // aplicar al collider
        if (circle == null) circle = GetComponent<CircleCollider2D>();
        if (circle != null) circle.radius = this.radius;

        // iniciar vida y ticks
        lifeCoroutine = StartCoroutine(CloudLife());
        tickCoroutine = StartCoroutine(TickLoop());
    }

    private IEnumerator CloudLife()
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        StopTickingAndDestroy();
    }

    private IEnumerator TickLoop()
    {
        if (useRealtimeForTicks)
        {
            var wait = new WaitForSecondsRealtime(tickInterval);
            while (true)
            {
                ApplyTickDamage();
                yield return wait;
            }
        }
        else
        {
            var wait = new WaitForSeconds(tickInterval);
            while (true)
            {
                ApplyTickDamage();
                yield return wait;
            }
        }
    }

    private void ApplyTickDamage()
    {
        // MANTENER comportamiento clásico: recorrer "inside" (llenado por triggers)
        if (inside.Count == 0) return;

        // crear snapshot para evitar InvalidOperationException si inside se modifica
        var snapshot = new GameObject[inside.Count];
        inside.CopyTo(snapshot);

        for (int i = 0; i < snapshot.Length; i++)
        {
            var go = snapshot[i];
            if (go == null)
            {
                // limpiar referencia nula de la colección original
                inside.Remove(go);
                continue;
            }

            if (owner != null && go == owner) continue;

            // Aplicar daño como en la versión anterior: intentar TakeContactDamage(float)
            ApplyDamageTo(go, damagePerTick);
        }
    }

    private void ApplyDamageTo(GameObject target, float damage)
    {
        if (damage <= 0f || target == null) return;

        // Intentar invocar TakeContactDamage(float) en cualquiera de sus componentes (como antes)
        var monos = target.GetComponents<MonoBehaviour>();
        foreach (var mb in monos)
        {
            if (mb == null) continue;
            var mi = mb.GetType().GetMethod("TakeContactDamage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (mi != null)
            {
                try
                {
                    mi.Invoke(mb, new object[] { damage });
                }
                catch
                {
                    // ignorar errores de invocación
                }
                return; // invocado en el primer componente que lo contenga
            }
        }

        // Si usas otra firma/interfaz, podemos añadirla aquí más tarde.
    }

    private void StopTickingAndDestroy()
    {
        if (tickCoroutine != null) StopCoroutine(tickCoroutine);
        if (lifeCoroutine != null) StopCoroutine(lifeCoroutine);
        inside.Clear();
        Destroy(gameObject);
    }

    #region Triggers
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (owner != null && other.gameObject == owner) return;
        if (((1 << other.gameObject.layer) & enemyLayerMask) == 0) return;
        inside.Add(other.gameObject);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == null) return;
        inside.Remove(other.gameObject);
    }
    #endregion

    private void OnDisable()
    {
        inside.Clear();
        if (tickCoroutine != null) StopCoroutine(tickCoroutine);
        if (lifeCoroutine != null) StopCoroutine(lifeCoroutine);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.8f, 1f, 0.3f);
        Gizmos.DrawSphere(transform.position, radius);
    }
}
