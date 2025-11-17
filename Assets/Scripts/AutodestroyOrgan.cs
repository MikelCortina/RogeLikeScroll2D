using UnityEngine;

public class AutoDestroyOrgan : MonoBehaviour
{
    public float lifetime = 5f;

    // Variable de control para permitir destrucción interna
    private bool internalDestroy = false;

    void Start()
    {
        // Programar la destrucción interna después de 'lifetime' segundos
        DestroyInternalAfterTime(lifetime);
    }

    // Método que solo este script debería usar para destruir
    private void DestroyInternalAfterTime(float time)
    {
        internalDestroy = true;
        Destroy(gameObject, time);
    }

    // Método público para destruirlo de forma segura desde otros scripts
    public void DestroySafely()
    {
        internalDestroy = true;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (!internalDestroy)
        {
            //Debug.LogWarning($"{name} intento de destruirlo desde fuera bloqueado!");
            // Si quieres, podrías reinstanciarlo aquí
        }
    }
}
