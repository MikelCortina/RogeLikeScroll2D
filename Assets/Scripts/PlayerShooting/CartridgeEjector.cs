using UnityEngine;

public class CartridgeEjector2D : MonoBehaviour
{
    public Transform ejectPoint;
    public float upwardSpeed = 3f;
    public float sideSpeed = 0.5f;
    public float randomSpread = 0.3f;
    public float maxAngularSpeed = 360f;

    public void Eject()
    {
        GameObject go = CartridgePool2D.Instance.Get();
        go.transform.position = ejectPoint.position;
        go.transform.rotation = Quaternion.identity;

        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
        Cartridge2D cartridge = go.GetComponent<Cartridge2D>();

        Vector2 initialVel = new Vector2(
            Random.Range(-randomSpread, randomSpread) + sideSpeed,
            upwardSpeed + Random.Range(0f, randomSpread)
        );

        float initialAngVel = Random.Range(-maxAngularSpeed, maxAngularSpeed);

        cartridge.Init(initialVel, initialAngVel);
    }
}
