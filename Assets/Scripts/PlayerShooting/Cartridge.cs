using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Cartridge2D : MonoBehaviour
{
    private Rigidbody2D rb;
    public float lifeTime = 4f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Init(Vector2 initialVelocity, float initialAngularVelocity)
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        rb.linearVelocity = initialVelocity;
        rb.angularVelocity = initialAngularVelocity;

        CancelInvoke(nameof(ReturnToPool));
        Invoke(nameof(ReturnToPool), lifeTime);
    }

    private void ReturnToPool()
    {
        CartridgePool2D.Instance.Return(gameObject);
    }
}
