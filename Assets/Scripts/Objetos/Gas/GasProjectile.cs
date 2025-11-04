using System.Collections;
using UnityEngine;

/// <summary>
/// Controla el movimiento parabólico desde startPos hasta targetPos en travelTime segundos.
/// Mientras se mueve genera un trail de humo (ParticleSystem).
/// Al llegar, espera explosionDelay y genera el gas (gasCloudPrefab), el cual escala de 0 a 1 rápidamente.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BombGasProjectile : MonoBehaviour
{
    private GameObject owner;
    private Vector3 startPos;
    private Vector3 targetPos;
    private float travelTime = 5f;
    private float arcHeight = 1f;
    private float explosionDelay = 0f;

    private GameObject gasCloudPrefab;
    private float gasRadius;
    private float gasDuration;
    private float gasDamagePerSecond;

    private bool initialized = false;

    [Header("Humo / Trail")]
    [Tooltip("Prefab de ParticleSystem para el trail de humo. Si dejas null, busca un ParticleSystem hijo.")]
    public ParticleSystem smokePrefab; // opcional: asigna en inspector
    [Tooltip("Punto local desde el que saldrá el humo (si no usas smokeSpawnTransform).")]
    public Vector3 smokeLocalOffset = Vector3.zero;
    [Tooltip("Si asignas un Transform, el humo se posicionará en ese transform local y seguirá al proyectil.")]
    public Transform smokeSpawnTransform;

    // instancia en runtime del sistema de partículas
    private ParticleSystem smokeInstance;

    private Coroutine moveCoroutine;

    [Header("Escalado de la nube al impactar")]
    [Tooltip("Tiempo en segundos que tarda la nube en escalar de 0 a 1 al impactar.")]
    public float cloudScaleUpTime = 0.12f; // ajustable

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

        // iniciar movimiento
        moveCoroutine = StartCoroutine(MoveParabola());

        // activar Rigidbody2D si existe y aplicarle torque seguro
        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.simulated = true;
            rb.AddTorque(Random.Range(-1f, 1f));
        }

        // crear/activar sistema de partículas de humo
        SetupSmoke();
    }

    private void SetupSmoke()
    {
        // si ya hay una instancia (por si ya la creaste antes), no hacer nada
        if (smokeInstance != null) return;

        // Preferencia: usar smokePrefab si está asignado, si no, buscar un ParticleSystem hijo.
        if (smokePrefab != null)
        {
            smokeInstance = Instantiate(smokePrefab, transform);
        }
        else
        {
            // buscar un ParticleSystem en hijos
            smokeInstance = GetComponentInChildren<ParticleSystem>();
            if (smokeInstance != null)
            {
                // re-parentear por claridad
                smokeInstance.transform.SetParent(transform, false);
            }
        }

        if (smokeInstance != null)
        {
            // posicionarlo en el punto deseado (transform o offset)
            if (smokeSpawnTransform != null)
            {
                smokeInstance.transform.position = smokeSpawnTransform.position;
                smokeInstance.transform.SetParent(transform, true); // que siga al proyectil
            }
            else
            {
                smokeInstance.transform.localPosition = smokeLocalOffset;
                smokeInstance.transform.localRotation = Quaternion.identity;
            }

            // asegurarse de que esté emitiendo
            if (!smokeInstance.isPlaying)
                smokeInstance.Play();
        }
        else
        {
            Debug.LogWarning("[BombGasProjectile] No se encontró ni smokePrefab ni ParticleSystem hijo para el trail de humo.");
        }
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

            // si hay un spawnTransform para el humo, mantenerlo actualizado (no siempre necesario si está parentado)
            if (smokeSpawnTransform != null && smokeInstance != null)
            {
                smokeInstance.transform.position = smokeSpawnTransform.position;
            }

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
        // detener/soltar el trail para que las partículas terminen su vida y luego se destruyan
        if (smokeInstance != null)
        {
            // dejar que las partículas actuales sigan y que no emita nuevas
            float maxLifetime = GetMaxParticleLifetime(smokeInstance);

            // Parar emisión pero permitir que las partículas vivan hasta su lifetime
            smokeInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            // desapegarlo para que no se destruya con este gameObject inmediatamente
            smokeInstance.transform.SetParent(null, true);

            // destruir la instancia de partículas pasado su lifetime máximo (+ un pequeño margen)
            Destroy(smokeInstance.gameObject, maxLifetime + 0.25f);
            smokeInstance = null;
        }

        // crear la nube de gas
        if (gasCloudPrefab != null)
        {
            GameObject cloud = Instantiate(gasCloudPrefab, transform.position, Quaternion.identity);

            // empezar escalado desde 0:
            cloud.transform.localScale = Vector3.zero;

            // añadimos un helper que hará el scale-up y llamará a Initialize en el GasCloud (si existe)
            var helper = cloud.AddComponent<CloudScaleAndInit>();
            helper.Setup(cloudScaleUpTime, gasRadius, gasDuration, gasDamagePerSecond, owner);
        }

        // opcional: efectos visuales/sonido aquí

        // destruir la bomba/proyectil
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

    private void OnDestroy()
    {
        // limpieza por si se destruye sin explotar
        if (smokeInstance != null)
        {
            float maxLifetime = GetMaxParticleLifetime(smokeInstance);
            smokeInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            smokeInstance.transform.SetParent(null, true);
            Destroy(smokeInstance.gameObject, maxLifetime + 0.25f);
            smokeInstance = null;
        }

        // parar coroutine si aún está corriendo
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
    }

    /// <summary>
    /// Calcula una cota segura del lifetime máximo de las partículas del ParticleSystem.
    /// Considera main.startLifetime y la variación si es un MinMaxCurve.
    /// </summary>
    private float GetMaxParticleLifetime(ParticleSystem ps)
    {
        var main = ps.main;
        // main.startLifetime puede ser constante o curva (MinMaxCurve). Sacamos el modo y valor máximo aproximado.
        if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
        {
            return main.startLifetime.constant;
        }
        else if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
        {
            return Mathf.Max(main.startLifetime.constantMin, main.startLifetime.constantMax);
        }
        else
        {
            // en curvas, coger un valor razonable (por ejemplo constantMax) - simplificamos devolviendo constantMax si existe
            return main.startLifetime.constantMax;
        }
    }

    /// <summary>
    /// Helper component que vive en la nube instanciada. Escala la nube de 0 a 1 y luego llama a GasCloud.Initialize si existe.
    /// Se elimina automáticamente cuando termina su trabajo (no afecta al resto del prefab).
    /// </summary>
    private class CloudScaleAndInit : MonoBehaviour
    {
        private float scaleTime = 0.12f;
        private float radius;
        private float duration;
        private float dps;
        private GameObject owner;

        public void Setup(float scaleUpTime, float radius, float duration, float dps, GameObject owner)
        {
            this.scaleTime = Mathf.Max(0.001f, scaleUpTime);
            this.radius = radius;
            this.duration = duration;
            this.dps = dps;
            this.owner = owner;

            // arrancar coroutine de scaling
            StartCoroutine(ScaleUpCoroutine());
        }

        private IEnumerator ScaleUpCoroutine()
        {
            float t = 0f;
            while (t < scaleTime)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / scaleTime);
                // easing: SmoothStep (suave). Cambia si quieres otro easing.
                float eased = Mathf.SmoothStep(0f, 1f, p);
                transform.localScale = Vector3.one * eased;
                yield return null;
            }

            transform.localScale = Vector3.one;

            // intentar inicializar GasCloud si está presente
            var gc = GetComponent<GasCloud>();
            if (gc != null)
            {
                gc.Initialize(radius, duration, dps, owner);
            }

            // fin del helper: darse a sí mismo la opción de destruirse (se deja el prefab de la nube)
            Destroy(this); // destruye sólo este componente, la nube permanece
        }
    }
}
