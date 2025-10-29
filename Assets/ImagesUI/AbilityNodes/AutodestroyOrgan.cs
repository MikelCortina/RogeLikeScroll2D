using UnityEngine;

public class AutoDestroyOrgan : MonoBehaviour
{
    public float lifetime = 5f;
    void Start() => Destroy(gameObject, lifetime);
}
