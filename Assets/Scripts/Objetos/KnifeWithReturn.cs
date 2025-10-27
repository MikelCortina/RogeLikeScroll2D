using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Spawn Two Projectiles Returning Persistent", fileName = "SpawnTwoProjectilesReturningEffect")]
public class SpawnTwoProjectilesReturningEffect : ScriptableObject, IPersistentEffect
{
    [Header("Configuración del proyectil")]
    public GameObject projectilePrefab;           // Prefab que idealmente tiene ProjectileReturn, Rigidbody2D y Collider2D
    public float launchSpeed = 8f;                // velocidad al lanzar
    public float returnSpeed = 10f;               // velocidad cuando vuelve al owner
    public float collectDistance = 0.5f;          // distancia para considerar "recogido" al llegar al owner

    [Header("Control de spawn")]
    public float spawnInterval = 2f;              // tiempo entre relanzamientos una vez recogidos ambos
    public Vector2 localSpawnOffset = Vector2.zero;
    public Vector2 localForward = Vector2.right;

    [Header("Auto-return")]
    public float maxDistance = 8f;                // distancia máxima desde el owner; si se supera, el proyectil vuelve

    // Runtime
    private Coroutine activeCoroutine;
    private GameObject runtimeOwner;

    // instancias reusables
    private GameObject[] instances = new GameObject[2];
    private ProjectileReturn[] projScripts = new ProjectileReturn[2];
    private bool[] collected = new bool[2];

    #region IPersistentEffect / IEffect

    public void ApplyTo(GameObject owner)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[SpawnTwoProjectilesReturningEffect] projectilePrefab no asignado.");
            return;
        }

        if (owner == null)
        {
            Debug.LogWarning("[SpawnTwoProjectilesReturningEffect] ApplyTo recibió owner = null.");
            return;
        }

        // Si ya está activo para ese owner, no reiniciar
        if (activeCoroutine != null)
        {
            Debug.Log("[SpawnTwoProjectilesReturningEffect] Ya activo, ApplyTo ignorado.");
            return;
        }

        runtimeOwner = owner;

        // Crear instancias si no existen
        EnsureInstancesCreated();

        // iniciar el loop encargado de relanzar cuando ambos recogidos
        activeCoroutine = CoroutineRunner.Instance.StartCoroutine(MainLoop());

        // lanzar por primera vez inmediatamente
        LaunchPair();
    }

    public void RemoveFrom(GameObject owner)
    {
        // detener coroutine principal
        if (activeCoroutine != null)
        {
            CoroutineRunner.Instance.StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        // destruir / desactivar instancias
        for (int i = 0; i < instances.Length; i++)
        {
            if (instances[i] != null)
                Object.Destroy(instances[i]);
            instances[i] = null;
            projScripts[i] = null;
            collected[i] = false;
        }

        runtimeOwner = null;
    }

    // Para compatibilidad IEffect
    public void Execute(Vector2 position, GameObject owner = null)
    {
        if (owner != null)
            ApplyTo(owner);
        else if (runtimeOwner != null)
            ApplyTo(runtimeOwner);
        else
            Debug.LogWarning("[SpawnTwoProjectilesReturningEffect] Execute sin owner. Use ApplyTo(owner).");
    }

    #endregion

    #region Instanciación y control

    private void EnsureInstancesCreated()
    {
        for (int i = 0; i < 2; i++)
        {
            if (instances[i] == null)
            {
                // Instanciamos inicialmente en (0,0)
                instances[i] = Object.Instantiate(projectilePrefab, Vector3.zero, Quaternion.identity);
                instances[i].name = $"ReturningProj_{i}";

                // desactivamos temporalmente (pero no antes de obtener componentes)
                var rb = instances[i].GetComponent<Rigidbody2D>();
                var col = instances[i].GetComponent<Collider2D>();

                if (rb == null)
                {
                    Debug.LogWarning($"[SpawnTwoProjectilesReturningEffect] prefab no tiene Rigidbody2D (instancia {i}).");
                }
                if (col == null)
                {
                    Debug.LogWarning($"[SpawnTwoProjectilesReturningEffect] prefab no tiene Collider2D (instancia {i}).");
                }

                // Aseguramos que tengan ProjectileReturn; si no, lo añadimos
                var pr = instances[i].GetComponent<ProjectileReturn>();
                if (pr == null) pr = instances[i].AddComponent<ProjectileReturn>();

                // Inicializamos el script (pasamos maxDistance)
                pr.Initialize(this, i, returnSpeed, collectDistance, maxDistance);
                projScripts[i] = pr;
                collected[i] = false;

                // Desactivamos hasta lanzar (pero dejamos componente configurado)
                instances[i].SetActive(false);
            }
        }
    }

    // Lanza los dos proyectiles desde la posición actual del owner
    private void LaunchPair()
    {
        if (runtimeOwner == null) return;

        // Spawn exactamente en la posición del owner
        Vector2 spawnPos = runtimeOwner.transform.position;

        // Direcciones: adelante y atrás según la rotación del owner
        Vector2 dirForward = runtimeOwner.transform.right.normalized;
        Vector2 dirBackward = -dirForward;
        Quaternion rotation = runtimeOwner.transform.rotation;

        for (int i = 0; i < 2; i++)
        {
            var go = instances[i];
            var pr = projScripts[i];
            if (go == null || pr == null) continue;

            // Posicionar exactamente sobre el owner
            go.transform.position = spawnPos;
            go.transform.rotation = rotation;
            go.SetActive(true);

            collected[i] = false;
            pr.ResetState(runtimeOwner);

            Vector2 dir = (i == 0) ? dirForward : dirBackward;

            if (go.TryGetComponent<Rigidbody2D>(out var rb))
            {
                rb.simulated = true;
                rb.linearVelocity = dir * launchSpeed;
            }
        }
    }


    // Lógica principal que espera a que ambos estén recogidos y relanza
    private IEnumerator MainLoop()
    {
        while (runtimeOwner != null)
        {
            // esperar hasta que ambos estén recogidos
            while (!(collected[0] && collected[1]))
            {
                yield return null;
                // si el owner deja de existir salimos
                if (runtimeOwner == null) break;
            }

            if (runtimeOwner == null) break;

            Debug.Log("[SpawnTwoProjectilesReturningEffect] Ambos recogidos -> esperando spawnInterval para relanzar.");
            // ambos recogidos -> esperar spawnInterval y relanzar
            float t = 0f;
            while (t < spawnInterval)
            {
                t += Time.deltaTime;
                if (runtimeOwner == null) break;
                yield return null;
            }

            if (runtimeOwner == null) break;

            // relanzar
            LaunchPair();
        }

        // limpieza si salimos
        activeCoroutine = null;
    }

    #endregion

    #region Callbacks desde ProjectileReturn

    // llamado por ProjectileReturn cuando un proyectil colisiona con algo y debe volver
    internal void RequestReturn(int projectileIndex)
    {
        if (projectileIndex < 0 || projectileIndex >= projScripts.Length) return;
        var pr = projScripts[projectileIndex];
        if (pr != null)
        {
            Debug.Log($"[SpawnTwoProjectilesReturningEffect] RequestReturn recibido para {projectileIndex}");
            pr.StartReturn(runtimeOwner);
        }
    }

    // llamado por ProjectileReturn cuando el proyectil llega al owner (recogido)
    internal void NotifyCollected(int projectileIndex)
    {
        if (projectileIndex < 0 || projectileIndex >= collected.Length) return;

        if (instances[projectileIndex] != null)
        {
            // desactivar y limpiar física
            var go = instances[projectileIndex];
            if (go.TryGetComponent<Rigidbody2D>(out var rb))
            {
                rb.linearVelocity = Vector2.zero;
                rb.simulated = false;
            }
            var col = go.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            go.SetActive(false);
        }

        collected[projectileIndex] = true;
        Debug.Log($"[SpawnTwoProjectilesReturningEffect] Projectile {projectileIndex} recogido.");
    }

    #endregion
}
