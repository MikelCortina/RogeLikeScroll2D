using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Effects/FlyingBombDropper")]
public class FlyingBombDropper : ScriptableObject, IPersistentEffect
{
    [Header("Flying")]
    public GameObject flyingPrefab;
    public float speed = 5f;
    public float respawnDelay = 4f;
    public float yPosition = 5f;
    public float leftOffset = -10f;
    public float rightOffset = 10f;

    [Header("Oscillation")]
    public float oscillationAmplitude = 0.5f;
    public float oscillationFrequency = 1f;

    [Header("Bombs")]
    public GameObject bombPrefab;
    public int bombPoolSize = 10;
    public float bombDropInterval = 1f;

    // runtime
    private GameObject instance;
    private Camera mainCam;
    private Coroutine moveCoroutine;
    private Coroutine bombCoroutine;
    private Queue<GameObject> bombPool = new Queue<GameObject>();
    private bool runtimeActive;

    // ================= IPersistentEffect =================

    public void ApplyTo(GameObject owner)
    {
        if (runtimeActive) return;
        if (flyingPrefab == null || bombPrefab == null) return;

        mainCam = Camera.main;
        if (mainCam == null) return;

        runtimeActive = true;

        instance = Instantiate(flyingPrefab, GetLeftSpawnPos(), Quaternion.identity);
        InitializeBombPool();

        moveCoroutine = CoroutineRunner.Instance.StartCoroutine(MoveLoop());
        bombCoroutine = CoroutineRunner.Instance.StartCoroutine(DropBombsLoop());
    }

    public void RemoveFrom(GameObject owner)
    {
        ResetRuntime();
    }

    public void ResetRuntime()
    {
        runtimeActive = false;

        if (moveCoroutine != null)
        {
            CoroutineRunner.Instance.StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        if (bombCoroutine != null)
        {
            CoroutineRunner.Instance.StopCoroutine(bombCoroutine);
            bombCoroutine = null;
        }

        if (instance != null)
        {
            Destroy(instance);
            instance = null;
        }

        while (bombPool.Count > 0)
        {
            var b = bombPool.Dequeue();
            if (b != null) Destroy(b);
        }

        mainCam = null;
    }

    // ================= Helpers =================

    private Vector3 GetLeftSpawnPos()
        => new Vector3(mainCam.transform.position.x + leftOffset, yPosition, 0);

    private Vector3 GetRightBound()
        => new Vector3(mainCam.transform.position.x + rightOffset, yPosition, 0);

    private void InitializeBombPool()
    {
        for (int i = 0; i < bombPoolSize; i++)
        {
            var bomb = Instantiate(bombPrefab);
            bomb.SetActive(false);
            bombPool.Enqueue(bomb);
        }
    }

    private GameObject GetBombFromPool()
    {
        while (bombPool.Count > 0)
        {
            var bomb = bombPool.Dequeue();
            if (bomb != null)
            {
                bomb.SetActive(true);
                return bomb;
            }
        }

        var extra = Instantiate(bombPrefab);
        extra.SetActive(true);
        return extra;
    }

    private void ReturnBomb(GameObject bomb)
    {
        if (!runtimeActive || bomb == null) return;
        bomb.SetActive(false);
        bombPool.Enqueue(bomb);
    }

    // ================= Coroutines =================

    private IEnumerator MoveLoop()
    {
        while (runtimeActive && instance != null)
        {
            Vector3 right = GetRightBound();

            while (runtimeActive && instance != null && instance.transform.position.x < right.x)
            {
                float x = instance.transform.position.x + speed * Time.deltaTime;
                float osc = oscillationAmplitude *
                            Mathf.Sin(Time.time * oscillationFrequency * Mathf.PI * 2f);

                instance.transform.position = new Vector3(x, yPosition + osc, 0);
                yield return null;
            }

            if (instance != null)
                instance.transform.position = GetLeftSpawnPos();

            yield return new WaitForSeconds(respawnDelay);
        }
    }

    private IEnumerator DropBombsLoop()
    {
        while (runtimeActive && instance != null)
        {
            var bomb = GetBombFromPool();
            bomb.transform.position = instance.transform.position;

            if (bomb.TryGetComponent<BombProjectile>(out var bp))
            {
                bp.owner = instance;
                bp.OnExplode += () => ReturnBomb(bomb);
            }

            yield return new WaitForSeconds(bombDropInterval);
        }
    }

    // ================= IEffect =================
    public void Execute(Vector2 position, GameObject owner = null)
    {
        if (owner != null)
            ApplyTo(owner);
    }
}
