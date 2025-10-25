using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target; // el objetivo al que quieres seguir
    public Vector3 offset; // desplazamiento respecto al objetivo
    public float targetSize = 5f; // tamaño de cámara final
    public float duration = 2f; // tiempo que tarda en llegar
    private float elapsed = 0f;
    private Camera cam;
    private Vector3 startPos;
    private float startSize;

    void Start()
    {
        cam = GetComponent<Camera>();
        startPos = transform.position;
        startSize = cam.orthographicSize;
    }

    void Update()
    {
        if (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Interpolación de posición
            Vector3 desiredPos = target.position + offset;
            transform.position = Vector3.Lerp(startPos, desiredPos, t);

            // Interpolación de tamaño
            cam.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
        }
    }
}
