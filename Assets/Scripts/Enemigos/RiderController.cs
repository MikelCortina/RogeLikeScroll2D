using System.Collections;
using UnityEngine;

public class RiderController : MonoBehaviour
{
    private Rigidbody2D rb;
    public bool isAttached = true;
    public Transform horseTransform;
    private bool jumpRequested = false;
    public float jumpForce = 7f;
    public PlayerMovement movement;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogWarning("[Rider] No Rigidbody2D encontrado. Se añade uno automáticamente.");
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
    }

    public void Init(Transform horse)
    {
        horseTransform = horse;
        transform.position = horse.position;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        isAttached = true;
        transform.SetParent(horseTransform); // opcional
        Debug.Log("[Rider] Init completo.");
    }

    void Start()
    {
        if (horseTransform != null)
        {
            Init(horseTransform);
        }
        else
        {
            Debug.LogWarning("[Rider] horseTransform no asignado.");
        }
    }

    void Update()
    {
        if (isAttached) 
        {
            transform.position = new Vector2(horseTransform.transform.position.x, transform.position.y);
        }
        if (!isAttached)
        {
            transform.position = new Vector2(horseTransform.transform.position.x, transform.position.y);
        }

        if (isAttached && Input.GetButtonDown("Jump") && movement.dobleSalto)
        {
            jumpRequested = true;
        }
    }

    void LateUpdate()
    {
        if (isAttached && horseTransform != null)
        {
            // Seguir al caballo mientras está enganchado
            Vector3 pos = transform.position;
            pos.x = horseTransform.position.x;
            transform.position = pos;
        }
    }

    void FixedUpdate()
    {
        if (jumpRequested)
        {
            jumpRequested = false;
            isAttached = false;
            transform.SetParent(null);
            rb.gravityScale = 1f;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            Debug.Log("[Rider] Salto ejecutado.");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isAttached) return;

        if (other.CompareTag("Horse") || other.CompareTag("Player"))
        {
            isAttached = true;
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
            transform.SetParent(other.transform.root);
            transform.position = new Vector2(other.transform.position.x, other.transform.position.y);
            Debug.Log("[Rider] Re-enganchado al caballo.");
        }
    }
    
}

