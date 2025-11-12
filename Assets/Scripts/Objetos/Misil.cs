using System.Collections;
using UnityEngine;

public class PersistentMisilEffect : MonoBehaviour, IPersistentEffect
{
    [Header("Target (screen)")]
    public bool useViewportCoordinates = true;
    public Vector2 targetViewport = new Vector2(0.5f, 0.5f);
    public Vector2 targetWorldPoint = Vector2.zero;

    [Header("Timing & visual")]
    public float warningDuration = 1.0f;
    public float missileSpawnHeight = 6f;

    [Header("Prefabs & settings")]
    public GameObject bombPrefab;
    public GameObject explosionPrefab;
    public float lineWidth = 0.05f;
    public AnimationCurve trailProgress = AnimationCurve.Linear(0, 0, 1, 1);

    private Coroutine activeCoroutine;

    // ---------------- IPersistentEffect ----------------
    public void ApplyTo(GameObject player)
    {
        // Al aplicarse, lanzamos la rutina de spawn
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(SpawnBombRoutine(GetTargetWorldPoint()));
    }

    public void RemoveFrom(GameObject player)
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }
    }

    // ---------------- IEffect ----------------
    public void Execute(Vector2 position, GameObject owner = null)
    {
        // Ignoramos owner en este efecto
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(SpawnBombRoutine(position));
    }

    // ---------------- Internals ----------------
    private Vector3 GetTargetWorldPoint()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[PersistentBombEffect] No main camera found.");
            return Vector3.zero;
        }

        Vector3 targetWorld;
        if (useViewportCoordinates)
            targetWorld = cam.ViewportToWorldPoint(new Vector3(targetViewport.x, targetViewport.y, Mathf.Abs(cam.transform.position.z)));
        else
            targetWorld = (Vector3)targetWorldPoint;

        targetWorld.z = 0f;
        return targetWorld;
    }

    private IEnumerator SpawnBombRoutine(Vector3 targetWorld)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[PersistentBombEffect] No main camera found.");
            yield break;
        }

        Vector3 spawnPos = targetWorld + Vector3.up * missileSpawnHeight;
        if (missileSpawnHeight <= 0f)
        {
            Vector3 topViewport = new Vector3(targetViewport.x, 1.05f, Mathf.Abs(cam.transform.position.z));
            spawnPos = cam.ViewportToWorldPoint(topViewport);
            spawnPos.z = 0f;
        }

        // Crear l�nea de advertencia
        GameObject lrGO = new GameObject("MissileWarning_Line");
        lrGO.transform.position = spawnPos;
        LineRenderer lr = lrGO.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.numCapVertices = 4;
        lr.material = new Material(Shader.Find("Sprites/Default"));

        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.red, 0f), new GradientColorKey(Color.yellow, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(0f, 1f) }
        );
        lr.colorGradient = g;

        float timer = 0f;
        while (timer < warningDuration)
        {
            float t = timer / warningDuration;
            float prog = trailProgress.Evaluate(t);
            Vector3 tailPos = Vector3.Lerp(spawnPos, targetWorld, prog * 0.95f);
            lr.SetPosition(0, tailPos);
            lr.SetPosition(1, targetWorld);

            timer += Time.deltaTime;
            yield return null;
        }

        lr.SetPosition(0, targetWorld);
        lr.SetPosition(1, targetWorld);

        if (bombPrefab != null)
        {
            GameObject bomb = Instantiate(bombPrefab, spawnPos, Quaternion.identity);
            Rigidbody2D rb = bomb.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 toTarget = (targetWorld - spawnPos).normalized;
                rb.linearVelocity = new Vector2(toTarget.x * 0.5f, toTarget.y * 0.5f);
            }

            MisilProjectile bp = bomb.GetComponent<MisilProjectile>();
            if (bp != null)
            {
                bp.SetTarget(targetWorld);
                bp.explosionPrefab = explosionPrefab;
            }
        }
        else
        {
            Debug.LogWarning("[PersistentBombEffect] bombPrefab no asignado.");
        }

        Destroy(lrGO, 1.5f);

        activeCoroutine = null;
    }
}
