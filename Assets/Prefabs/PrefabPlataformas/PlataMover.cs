using UnityEngine;

public class PlataMover : MonoBehaviour
{
    public float speed = -2f;
    public Transform player; // Arrastra el jugador desde el inspector

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    private void Update()
    {
        float dx = speed * Time.deltaTime;
        transform.position += new Vector3(dx, 0f, 0f);

     
    }
}
