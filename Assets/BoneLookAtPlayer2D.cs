using UnityEngine;

public class BoneLookAtPlayer : MonoBehaviour
{
    public string targetTag = "Player";
    public float rotationOffset = 0f;



    private Transform target;
    private Transform root;

    void Start()
    {
        GameObject obj = GameObject.FindGameObjectWithTag(targetTag);
        if (obj != null)
            target = obj.transform;

        // El transform que hace el flip. Ajusta si no es root.
        root = transform.root;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector2 direction = target.position - transform.position;

        bool flippedX = root.lossyScale.x < 0f;
        if (flippedX)
            direction.x = -direction.x;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        angle += rotationOffset;
        angle = Mathf.DeltaAngle(0, angle);

        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

}
