using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Effects/ScreenLooperPersistentOptimized")]
public class ScreenLooperPersistentOptimized : ScriptableObject, IPersistentEffect
{
    [Header("Prefab y control")]
    public GameObject prefab;
    public float speed = 5f;
    public float respawnDelay = 1f;

    [Header("Offsets relativos a cámara")]
    public float leftOffset = -10f;
    public float rightOffset = 10f;
    public float yPosition = 0f;

    // Runtime
    private Coroutine activeCoroutine;
    private Camera mainCam;
    private GameObject instance;

    #region IPersistentEffect / IEffect

    public void ApplyTo(GameObject owner)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[ScreenLooperPersistentOptimized] Prefab no asignado.");
            return;
        }

        if (activeCoroutine != null) return;

        mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogWarning("[ScreenLooperPersistentOptimized] Cámara principal no encontrada.");
            return;
        }

        // Instancia inicial
        if (instance == null)
        {
            instance = Object.Instantiate(prefab, GetLeftSpawnPos(), Quaternion.identity);
        }
        else
        {
            instance.transform.position = GetLeftSpawnPos();
            instance.SetActive(true);
        }

        activeCoroutine = CoroutineRunner.Instance.StartCoroutine(MoveLoop());
    }

    public void RemoveFrom(GameObject owner)
    {
        if (activeCoroutine != null)
        {
            CoroutineRunner.Instance.StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        if (instance != null)
        {
            instance.SetActive(false);
        }
    }

    public void Execute(Vector2 position, GameObject owner = null)
    {
        ApplyTo(owner);
    }

    #endregion

    #region Helpers

    private Vector3 GetLeftSpawnPos()
    {
        return new Vector3(mainCam.transform.position.x + leftOffset, yPosition, 0f);
    }

    private Vector3 GetRightBound()
    {
        return new Vector3(mainCam.transform.position.x + rightOffset, yPosition, 0f);
    }

    #endregion

    #region Coroutine de movimiento

    private IEnumerator MoveLoop()
    {
        while (instance != null)
        {
            Vector3 rightBound = GetRightBound();

            // Mover hasta pasar la derecha
            while (instance.transform.position.x < rightBound.x)
            {
                instance.transform.position += Vector3.right * speed * Time.deltaTime;
                yield return null;
            }

            // Teletransportar a la izquierda y esperar respawnDelay
            instance.transform.position = GetLeftSpawnPos();
            yield return new WaitForSeconds(respawnDelay);
        }

        activeCoroutine = null;
    }

    #endregion
}
