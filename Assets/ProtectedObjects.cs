using UnityEngine;
using System.Diagnostics; // Para StackTrace
using Debug = UnityEngine.Debug;

public class ProtectedObject : MonoBehaviour
{
    private bool internalDestroy = false;

    public void DestroyInternal()
    {
        internalDestroy = true;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (!internalDestroy)
        {
            StackTrace stack = new StackTrace();
            //Debug.LogWarning($"{name} intento de destruirlo desde fuera bloqueado! Stack:\n{stack}");
        }
    }
}
