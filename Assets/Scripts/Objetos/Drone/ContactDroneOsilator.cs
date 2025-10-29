using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Effects/ScreenLooperPersistentOscilator")]
public class ScreenLooperPersistentOscilator : ScriptableObject, IPersistentEffect
{
    [Header("Prefab y control")]
    public GameObject prefab;
    public float speed = 5f;
    public float respawnDelay = 1f;

    [Header("Offsets relativos a cámara")]
    public float leftOffset = -10f;
    public float rightOffset = 10f;
    public float yPosition = 0f;

    [Header("Oscilación vertical")]
    public float oscillationAmplitude = 0.5f;   // amplitud en unidades
    public float oscillationFrequency = 1f;     // frecuencia en Hz

    // Runtime
    private Coroutine activeCoroutine;
    private Camera mainCam;
    private GameObject instance;
    private float instancePhase = 0f; // fase aleatoria por instancia

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

        // asignar fase aleatoria para la oscilación (evita sincronía entre instancias)
        instancePhase = Random.Range(0f, Mathf.PI * 2f);

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
                // avanzar en X
                float newX = instance.transform.position.x + speed * Time.deltaTime;

                // calcular oscilación en Y
                float osc = 0f;
                if (oscillationAmplitude != 0f && oscillationFrequency != 0f)
                {
                    osc = oscillationAmplitude * Mathf.Sin(Time.time * (Mathf.PI * 2f) * oscillationFrequency + instancePhase);
                }

                instance.transform.position = new Vector3(newX, yPosition + osc, 0f);
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
