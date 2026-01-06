using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Effects/PersistentMisilEffect")]
public class PersistentMisilEffect : ScriptableObject, IPersistentEffect
{
    [Header("Prefab y control")]
    public GameObject prefab;
    public int droneCount = 3;
    public float speed = 6f;
    public float timeBetweenWaves = 4f;

    [Header("Offsets relativos a cámara")]
    public float leftOffset = -2f;
    public float rightOffset = 2f;

    [Header("Rango Y")]
    public float minY = -3f;
    public float maxY = 3f;

    [Header("Variación")]
    public float spawnXVariation = 1f;
    public float initialStaggerDistance = 2.5f;

    // ===== Runtime (NO serializado) =====
    private readonly List<GameObject> activeDrones = new();
    private Coroutine activeCoroutine;
    private Camera mainCam;
    private GameObject runtimeOwner;

    #region IPersistentEffect

    public void ApplyTo(GameObject owner)
    {
        if (owner == null)
        {
            Debug.LogWarning("[PersistentMisilEffect] ApplyTo: owner null.");
            return;
        }

        if (prefab == null)
        {
            Debug.LogWarning("[PersistentMisilEffect] Prefab no asignado.");
            return;
        }

        if (activeCoroutine != null)
        {
            // ya activo
            return;
        }

        runtimeOwner = owner;
        mainCam = Camera.main;

        if (mainCam == null)
        {
            Debug.LogWarning("[PersistentMisilEffect] Cámara principal no encontrada.");
            runtimeOwner = null;
            return;
        }

        ClearDrones();

        // Crear misiles escalonados
        for (int i = 0; i < droneCount; i++)
        {
            GameObject drone = Object.Instantiate(
                prefab,
                GetLeftSpawnPosition(i),
                Quaternion.identity
            );

            activeDrones.Add(drone);

            MisilProjectile misil = drone.GetComponent<MisilProjectile>();
            if (misil != null)
            {
                misil.enabled = true;
            }
        }

        activeCoroutine = CoroutineRunner.Instance.StartCoroutine(MoveLoop());
    }

    public void RemoveFrom(GameObject owner)
    {
        StopRuntime();
    }

    public void Execute(Vector2 position, GameObject owner = null)
    {
        ApplyTo(owner);
    }

    #endregion

    #region Loop

    private IEnumerator MoveLoop()
    {
        while (runtimeOwner != null)
        {
            for (int i = 0; i < activeDrones.Count; i++)
            {
                GameObject drone = activeDrones[i];

                if (drone == null)
                {
                    drone = Object.Instantiate(prefab, GetLeftSpawnPosition(i), Quaternion.identity);
                    activeDrones[i] = drone;
                }

                drone.transform.position += Vector3.right * speed * Time.deltaTime;

                if (drone.transform.position.x > GetRightExitX())
                {
                    drone.transform.position = GetLeftSpawnPosition(i);
                }
            }

            yield return null;
        }

        // owner muerto → limpieza automática
        StopRuntime();
    }

    #endregion

    #region Helpers

    private Vector3 GetLeftSpawnPosition(int index)
    {
        float randomY = Random.Range(minY, maxY);
        float randomXOffset = Random.Range(-spawnXVariation, spawnXVariation);
        float stagger = index * initialStaggerDistance;

        float spawnX = mainCam.transform.position.x + leftOffset - stagger + randomXOffset;
        return new Vector3(spawnX, randomY, 0f);
    }

    private float GetRightExitX()
    {
        return mainCam.transform.position.x + rightOffset;
    }

    private void ClearDrones()
    {
        for (int i = 0; i < activeDrones.Count; i++)
        {
            if (activeDrones[i] != null)
            {
                Object.Destroy(activeDrones[i]);
            }
        }

        activeDrones.Clear();
    }

    private void StopRuntime()
    {
        if (activeCoroutine != null)
        {
            CoroutineRunner.Instance.StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        ClearDrones();
        runtimeOwner = null;
        mainCam = null;
    }

    public void ResetRuntime()
    {
        StopRuntime();
    }

    #endregion
}
