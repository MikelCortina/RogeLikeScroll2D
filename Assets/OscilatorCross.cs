using UnityEngine;
using System.Collections;

public class OrbitalMover : MonoBehaviour
{
    [Header("Oscilación alrededor de este objeto")]
    public Transform centerObject;

    [Header("Configuración de oscilación")]
    public float orbitRadius = 2f;
    public float orbitSpeed = 2f; // vueltas por segundo
    public float orbitDuration = 3f; // duración en segundos

    [Header("Aceleración final")]
    public float finalSpeed = 5f;

    [Header("Opciones")]
    public bool useLocalPositions = false;
    public bool useUnscaledTime = false;

    private Coroutine movementCoroutine;

    void OnEnable()
    {
        // Detener corrutina previa si existe
        if (movementCoroutine != null)
            StopCoroutine(movementCoroutine);

        // Iniciar de nuevo al activarse
        movementCoroutine = StartCoroutine(OscillateThenMoveToOrigin());
    }

    void OnDisable()
    {
        // Opcional: detener la corrutina al desactivar
        if (movementCoroutine != null)
            StopCoroutine(movementCoroutine);
    }

    private IEnumerator OscillateThenMoveToOrigin()
    {
        if (centerObject == null)
        {
            Debug.LogWarning("OrbitalMover: centerObject no asignado.");
            yield break;
        }

        float elapsed = 0f;
        float dt;
        Vector3 centerPos = useLocalPositions ? centerObject.localPosition : centerObject.position;

        float initialAngle = Random.Range(0f, Mathf.PI * 2f);

        while (elapsed < orbitDuration)
        {
            dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += dt;

            float angle = initialAngle + elapsed * orbitSpeed * 2f * Mathf.PI;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * orbitRadius;

            if (useLocalPositions)
                transform.localPosition = centerPos + offset;
            else
                transform.position = centerPos + offset;

            yield return null;
        }

        Vector3 target = Vector3.zero;

        while (true)
        {
            dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            Vector3 currentPos = useLocalPositions ? transform.localPosition : transform.position;
            Vector3 newPos = Vector3.MoveTowards(currentPos, target, finalSpeed * dt);

            if (useLocalPositions)
                transform.localPosition = newPos;
            else
                transform.position = newPos;

            if (Vector3.Distance(newPos, target) < 0.01f)
                break;

            yield return null;
        }

        if (useLocalPositions)
            transform.localPosition = target;
        else
            transform.position = target;
    }
}
