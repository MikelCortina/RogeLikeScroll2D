using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Effects/FlyingBombDropper")]
public class FlyingBombDropper : ScriptableObject, IPersistentEffect
{
    [Header("Prefab y control de avión/objeto volador")]
    public GameObject flyingPrefab;
    public float speed = 5f;
    public float respawnDelay = 1f;
    public float yPosition = 5f;
    public float leftOffset = -10f;
    public float rightOffset = 10f;

    [Header("Oscilación vertical")]
    public float oscillationAmplitude = 0.5f;
    public float oscillationFrequency = 1f;

    [Header("Prefab y control de bombas")]
    public GameObject bombPrefab;
    public int bombPoolSize = 10;      // tamaño del pool
    public float bombDropInterval = 1f;
    public Transform bombSpawnPoint;

    // Runtime
    private Coroutine moveCoroutine;
    private Coroutine bombCoroutine;
    private GameObject instance;
    private Camera mainCam;

    private Queue<GameObject> bombPool = new Queue<GameObject>();
    private float instancePhase = 0f;

    #region IPersistentEffect

    public void ApplyTo(GameObject owner)
    {
        if (flyingPrefab == null || bombPrefab == null)
        {
            Debug.LogWarning("[FlyingBombDropper] Prefabs no asignados.");
            return;
        }

        if (moveCoroutine != null) return;

        mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogWarning("[FlyingBombDropper] Cámara principal no encontrada.");
            return;
        }

        // asignar fase aleatoria para la oscilación (evita sincronía entre instancias)
        instancePhase = Random.Range(0f, Mathf.PI * 2f);

        // Instancia inicial del avión
        if (instance == null)
        {
            instance = Object.Instantiate(flyingPrefab, GetLeftSpawnPos(), Quaternion.identity);
        }
        else
        {
            instance.transform.position = GetLeftSpawnPos();
            instance.SetActive(true);
        }

        // Inicializar pool de bombas
        InitializeBombPool();

        moveCoroutine = CoroutineRunner.Instance.StartCoroutine(MoveLoop(owner));
        bombCoroutine = CoroutineRunner.Instance.StartCoroutine(DropBombsLoop(owner));
    }

    public void RemoveFrom(GameObject owner)
    {
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
            instance.SetActive(false);
        }

        // Devolver todas las bombas al pool
        foreach (var bomb in bombPool)
        {
            bomb.SetActive(false);
        }
    }

    public void Execute(Vector2 position, GameObject owner = null)
    {
        ApplyTo(owner);
    }

    #endregion

    #region Helpers

    private Vector3 GetLeftSpawnPos() => new Vector3(mainCam.transform.position.x + leftOffset, yPosition, 0f);
    private Vector3 GetRightBound() => new Vector3(mainCam.transform.position.x + rightOffset, yPosition, 0f);

    private void InitializeBombPool()
    {
        if (bombPool.Count > 0) return;

        for (int i = 0; i < bombPoolSize; i++)
        {
            GameObject bomb = Object.Instantiate(bombPrefab, Vector3.zero, Quaternion.identity);
            bomb.SetActive(false);
            bombPool.Enqueue(bomb);
        }
    }

    private GameObject GetBombFromPool()
    {
        if (bombPool.Count == 0)
        {
            // si el pool se acaba, podemos instanciar más (opcional)
            GameObject bomb = Object.Instantiate(bombPrefab, Vector3.zero, Quaternion.identity);
            bomb.SetActive(false);
            return bomb;
        }

        GameObject pooledBomb = bombPool.Dequeue();
        pooledBomb.SetActive(true);
        return pooledBomb;
    }

    private void ReturnBombToPool(GameObject bomb)
    {
        bomb.SetActive(false);
        bombPool.Enqueue(bomb);
    }

    #endregion

    #region Coroutines

    private IEnumerator MoveLoop(GameObject owner)
    {
        while (instance != null)
        {
            Vector3 rightBound = GetRightBound();

            while (instance.transform.position.x < rightBound.x)
            {
                // avanzar en X
                float newX = instance.transform.position.x + speed * Time.deltaTime;

                // calcular oscilación en Y
                float osc = 0f;
                if (oscillationAmplitude != 0f && oscillationFrequency != 0f)
                {
                    osc = oscillationAmplitude * Mathf.Sin(Time.time * (Mathf.PI * 2f) * oscillationFrequency + instancePhase);
                }

                instance.transform.position = new Vector3(newX, yPosition + osc, 0f);
                yield return null;
            }

            instance.transform.position = GetLeftSpawnPos();
            yield return new WaitForSeconds(respawnDelay);
        }

        moveCoroutine = null;
    }

    private IEnumerator DropBombsLoop(GameObject owner)
    {
        Transform spawnPoint = instance.transform.Find("BombSpawnPoint");
        if (spawnPoint == null) spawnPoint = instance.transform;

        while (instance != null)
        {
            Vector3 spawnPos = spawnPoint.position;

            GameObject bomb = GetBombFromPool();
            bomb.transform.position = spawnPos;

            BombProjectile bombScript = bomb.GetComponent<BombProjectile>();
            if (bombScript != null)
            {
                bombScript.owner = instance;
                bombScript.OnExplode += () => ReturnBombToPool(bomb); // cuando explote, volver al pool
            }

            yield return new WaitForSeconds(bombDropInterval);
        }
    }

    #endregion
}
