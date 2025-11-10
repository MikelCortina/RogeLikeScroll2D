using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public float defaultDuration = 0.2f;
    public float defaultMagnitude = 0.3f;

    private Coroutine shakeCoroutine;
    private CameraZoomAndBounds cameraZoomAndBounds;

    void Awake()
    {
        cameraZoomAndBounds = GetComponent<CameraZoomAndBounds>();
        if (cameraZoomAndBounds == null)
        {
            Debug.LogError("CameraShake necesita CameraZoomAndBounds en el mismo objeto.");
        }
    }

    public void Shake(float duration = -1f, float magnitude = -1f)
    {
        if (duration <= 0f) duration = defaultDuration;
        if (magnitude <= 0f) magnitude = defaultMagnitude;

        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Obtener la posición “base” de la cámara calculada por CameraZoomAndBounds
            Vector3 basePos = new Vector3(
                transform.position.x,
                transform.position.y,
                transform.position.z
            );

            // Añadir desplazamiento aleatorio
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;
            transform.position = basePos + new Vector3(offsetX, offsetY, 0f);

            yield return null;
        }

        // Al final, dejar la cámara en la posición calculada por CameraZoomAndBounds
        transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
    }
}
