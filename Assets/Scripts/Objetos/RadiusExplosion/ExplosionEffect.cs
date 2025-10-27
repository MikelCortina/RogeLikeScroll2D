using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Projectile Effects/Explosion")]
public class ExplosionEffect : ProjectileEffect
{
    public float radius = 2f;
    public float knockback;
    public LayerMask hittableLayers = ~0;
    public GameObject vfxPrefab;
    public bool destroyVfxAfterSec = true;
    public float vfxLifetime = 2f;
    public bool ignoreOwner = true;

    [Header("Debug Visualizer")]
    public bool drawDebugCircleInGame = true;
    public float debugDuration = 0.5f;
    public int debugSegments = 60;
    public float debugLineWidth = 0.05f;
    public Color debugColor = Color.red;

    public override void Execute(Vector2 position, GameObject owner = null)
    {
        float damage = StatsCommunicator.Instance.CalculateDamage();

        // VFX
        if (vfxPrefab != null)
        {
            GameObject vfx = Object.Instantiate(vfxPrefab, position, Quaternion.identity);
            if (destroyVfxAfterSec) Object.Destroy(vfx, vfxLifetime);
        }

        // Área de daño
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, radius, hittableLayers);
        foreach (var col in hits)
        {
            if (col == null) continue;
            if (ignoreOwner && owner != null && col.gameObject == owner) continue;

            EnemyBase enemy = col.GetComponentInParent<EnemyBase>();
            Rigidbody2D rb = col.attachedRigidbody;
            Vector2 dir = Vector2.zero;
            if (rb != null) dir = ((Vector2)rb.position - position).normalized;
            // Aplica daño a EnemyBase si existe
            if (enemy != null)
            {
                enemy.TakeContactDamage(damage);
                // Llamamos a ApplyKnockback para que la propia IA gestione el estado de knockback
                enemy.ApplyKnockback(dir * knockback);
            }
            else if (rb != null)
            {
                // Si no hay EnemyBase pero sí Rigidbody (por ejemplo objetos físicos), aplica impulso directo
                rb.AddForce(dir * knockback, ForceMode2D.Impulse);
            }

            Debug.Log("Explosion hit: " + col.name);
        }

        // Debug: dibujar el radio en Game View (temporal)
        if (drawDebugCircleInGame)
            DrawExplosionDebug(position, radius, debugDuration, debugSegments, debugLineWidth, debugColor);
    }

    private void DrawExplosionDebug(Vector2 position, float radius, float duration, int segments, float lineWidth, Color color)
    {
        GameObject temp = new GameObject("ExplosionDebug");
        temp.transform.position = position;
        // opcional: para que no aparezca en builds si lo decides, podrías usar #if UNITY_EDITOR, pero lo dejo simple.
        LineRenderer lr = temp.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = segments + 1;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
        lr.numCapVertices = 0;

        for (int i = 0; i <= segments; i++)
        {
            float angle = i * 360f / segments * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            lr.SetPosition(i, new Vector3(position.x + x, position.y + y, 0f));
        }

        Object.Destroy(temp, duration);
    }
}
