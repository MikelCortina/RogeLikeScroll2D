using System.Collections;
using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
public class EnemyHelicopter : EnemyBase
{
    [Header("Helicopter - Movement")]
    [SerializeField] private float flyingSpeed = 3.5f;
    [SerializeField] private float hoverAmplitude = 0.25f;
    [SerializeField] private float hoverFrequency = 1.2f;
    [SerializeField] private float desiredHorizontalDistance = 2f;
    [SerializeField] private float movementSmoothTime = 0.15f;

    private float baseY;
    private float hoverOffset;
    private Vector2 moveVelocityRef = Vector2.zero;

    [Header("Helicopter - Shooting")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private int projectilesPerShot = 5;
    [SerializeField] private float spawnDelay = 0.2f;
    [SerializeField] private float horizontalSpacing = 1f;
    [SerializeField] private float arcHeight = 2f;

    [Header("Camera Clamp")]
    [SerializeField] private float cameraClampPadding = 0.3f;
    private bool hasEnteredCamera = false;
    private Camera mainCam;

    protected override void Awake()
    {
        base.Awake();

        if (rb != null)
            rb.gravityScale = 0f;

        baseY = transform.position.y;
        mainCam = Camera.main;

        if (firePoint == null)
        {
            var fp = transform.Find("FirePoint");
            if (fp != null)
                firePoint = fp;
        }

        if (firePoint == null)
            Debug.LogWarning($"{name}: firePoint no asignado.");

        if (projectilePrefab == null)
            Debug.LogWarning($"{name}: projectilePrefab no asignado.");
    }

    private void Update()
    {
        if (target == null) return;

        if (IsPlayerInRange())
        {
            canMove = false;
            StopMovement();
            TryAttack();
        }
        else
        {
            canMove = true;
        }

        CheckIfEnteredCamera();
    }

    private void FixedUpdate()
    {
        hoverOffset = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        float targetY = baseY + hoverOffset;

        if (canMove)
        {
            FlyTowardsTarget(targetY);
        }
        else
        {
            Vector2 desiredPos = new Vector2(transform.position.x, targetY);
            Vector2 newPos = Vector2.SmoothDamp(rb.position, desiredPos, ref moveVelocityRef, movementSmoothTime, Mathf.Infinity, Time.fixedDeltaTime);
            rb.MovePosition(newPos);
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void LateUpdate()
    {
        if (hasEnteredCamera)
            ClampToCameraBounds();
    }

    private void FlyTowardsTarget(float targetY)
    {
        if (target == null) return;

        float dirX = Mathf.Sign(target.position.x - transform.position.x);
        float desiredX = target.position.x - dirX * desiredHorizontalDistance;
        Vector2 desiredPos = new Vector2(desiredX, targetY);

        FlipIfNeeded(target.position.x - transform.position.x);

        Vector2 newPos = Vector2.SmoothDamp(rb.position, desiredPos, ref moveVelocityRef, movementSmoothTime, Mathf.Infinity, Time.fixedDeltaTime);
        rb.MovePosition(newPos);
    }

    protected override void PerformAttack()
    {
        if (projectilePrefab == null || firePoint == null) return;
        StartCoroutine(ShootParabolicFan());
    }

    private IEnumerator ShootParabolicFan()
    {
        Vector2 origin = firePoint.position;
        Vector2 playerPositionAtShot = target != null ? (Vector2)target.position : origin;

        for (int i = 0; i < projectilesPerShot; i++)
        {
            Vector2 targetPos = (i == 0)
                ? playerPositionAtShot
                : playerPositionAtShot + new Vector2(i * horizontalSpacing, 0f);

            SpawnParabolicProjectile(origin, targetPos, arcHeight);

            if (spawnDelay > 0f)
                yield return new WaitForSeconds(spawnDelay);
            else
                yield return null;
        }
    }

    private void SpawnParabolicProjectile(Vector2 origin, Vector2 targetPos, float arcHeightAboveOrigin)
    {
        GameObject proj = Instantiate(projectilePrefab, origin, Quaternion.identity);
        ProjectileHelicopter projScript = proj.GetComponent<ProjectileHelicopter>();
        if (projScript != null)
        {
            projScript.owner = this.gameObject;
            projScript.isHoming = false;
        }

        Rigidbody2D prb = proj.GetComponent<Rigidbody2D>();
        if (prb == null) return;

        float g = Mathf.Abs(Physics2D.gravity.y) * Mathf.Max(0.001f, prb.gravityScale);

        float dx = targetPos.x - origin.x;
        float dy = targetPos.y - origin.y;
        float apexAboveOrigin = Mathf.Max(arcHeightAboveOrigin, dy + 0.5f);

        float vy = Mathf.Sqrt(2f * g * apexAboveOrigin);
        float tUp = vy / g;
        float apexToTarget = apexAboveOrigin - dy;
        float tDown = Mathf.Sqrt(Mathf.Max(0.0001f, 2f * apexToTarget / g));
        float totalTime = tUp + tDown;

        float vx = dx / totalTime;

        prb.linearVelocity = new Vector2(vx, vy);

        float angle = Mathf.Atan2(vy, vx) * Mathf.Rad2Deg;
        proj.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void CheckIfEnteredCamera()
    {
        if (hasEnteredCamera || mainCam == null) return;

        Vector3 viewPos = mainCam.WorldToViewportPoint(transform.position);
        if (viewPos.x >= 0 && viewPos.x <= 1 && viewPos.y >= 0 && viewPos.y <= 1 && viewPos.z > 0)
        {
            hasEnteredCamera = true;
        }
    }

    private void ClampToCameraBounds()
    {
        if (mainCam == null) return;

        Vector3 pos = transform.position;
        Vector3 min = mainCam.ViewportToWorldPoint(new Vector3(0, 0, mainCam.nearClipPlane));
        Vector3 max = mainCam.ViewportToWorldPoint(new Vector3(1, 1, mainCam.nearClipPlane));

        pos.x = Mathf.Clamp(pos.x, min.x + cameraClampPadding, max.x - cameraClampPadding);
        pos.y = Mathf.Clamp(pos.y, min.y + cameraClampPadding, max.y - cameraClampPadding);

        transform.position = pos;
    }
}
