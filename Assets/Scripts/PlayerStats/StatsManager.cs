using UnityEngine;
using System;

[System.Serializable]
public class StatsData
{
    [Header("HP")]
    public float maxHP; public float currentHP;
    [Header("Projectile")]
    public float projectileDamage;public float projectileSpeed;
    [Header("Movimiento")]
    public float moveForce;public float jumpForce;public float maxSpeed;public float friction;
    [Header("FireArm")]
    public float fireRate;
    public float radius;

    public StatsData Clone()
    {
        return new StatsData
        {
            maxHP = this.maxHP,
            currentHP = this.currentHP,
            projectileDamage = this.projectileDamage,
            projectileSpeed = this.projectileSpeed,
            moveForce = this.moveForce,
            jumpForce = this.jumpForce,
            maxSpeed = this.maxSpeed,
            friction = this.friction,
            fireRate = this.fireRate,
            radius = this.radius
        };
    }
}

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance { get; private set; }

    [Header("Template Stats")]
    [SerializeField] private StatsData templateStats = new StatsData();

    [Header("Player Invulnerability")]
    [SerializeField] public float iFrameDuration = 0.8f;
    [SerializeField] public float flashfloaterval = 0.08f;

    [Header("Player Renderers")]
    [SerializeField] public SpriteRenderer[] renderersToFlash;

    public StatsData RuntimeStats { get; private set; }

    public event Action<float, float> OnHealthChanged;
    public event Action OnPlayerDied;

    private bool isInvulnerable = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        RuntimeStats = templateStats.Clone();
        OnHealthChanged?.Invoke(RuntimeStats.currentHP, RuntimeStats.maxHP);

        if (renderersToFlash == null || renderersToFlash.Length == 0)
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
                renderersToFlash = new SpriteRenderer[] { sr };
        }
    }

    // --- Player Health Methods ---
    public void DamagePlayer(float amount)
    {
        if (amount <= 0 || isInvulnerable) return;

        RuntimeStats.currentHP = Mathf.Max(0, RuntimeStats.currentHP - amount);
        Debug.Log($"Player took {amount} damage. Current HP: {RuntimeStats.currentHP}/{RuntimeStats.maxHP}");
        OnHealthChanged?.Invoke(RuntimeStats.currentHP, RuntimeStats.maxHP);

        if (iFrameDuration > 0f) StartCoroutine(InvulnerabilityCoroutine());

        if (RuntimeStats.currentHP <= 0)
            PlayerDeath();
    }

    public void HealPlayer(float amount)
    {
        if (amount <= 0) return;
        RuntimeStats.currentHP = Mathf.Min(RuntimeStats.maxHP, RuntimeStats.currentHP + amount);
        OnHealthChanged?.Invoke(RuntimeStats.currentHP, RuntimeStats.maxHP);
    }

    public void ModifyMaxHP(float delta)
    {
        RuntimeStats.maxHP = Mathf.Max(1, RuntimeStats.maxHP + delta);
        RuntimeStats.currentHP = Mathf.Min(RuntimeStats.currentHP, RuntimeStats.maxHP);
        OnHealthChanged?.Invoke(RuntimeStats.currentHP, RuntimeStats.maxHP);
    }

    public void ModifyProjectileDamage(float delta)
    {
        RuntimeStats.projectileDamage = Mathf.Max(0, RuntimeStats.projectileDamage + delta);
    }

    public void ModifyProjectileSpeed(float delta)
    {
        RuntimeStats.projectileSpeed = Mathf.Max(0, RuntimeStats.projectileSpeed + delta);
    }


    // --- Invulnerability Coroutine ---
    private System.Collections.IEnumerator InvulnerabilityCoroutine()
    {
        isInvulnerable = true;
        if (renderersToFlash != null && renderersToFlash.Length > 0)
        {
            float elapsed = 0f;
            bool visible = true;
            while (elapsed < iFrameDuration)
            {
                foreach (var r in renderersToFlash)
                    if (r != null) r.enabled = visible;

                visible = !visible;
                yield return new WaitForSeconds(flashfloaterval);
                elapsed += flashfloaterval;
            }
            foreach (var r in renderersToFlash)
                if (r != null) r.enabled = true;
        }
        else
            yield return new WaitForSeconds(iFrameDuration);

        isInvulnerable = false;
    }

    private void PlayerDeath()
    {
        OnPlayerDied?.Invoke();
        Debug.Log("Jugador murió");
    }

    // --- Reset para nueva run ---
    public void ResetStats()
    {
        RuntimeStats = templateStats.Clone();
        OnHealthChanged?.Invoke(RuntimeStats.currentHP, RuntimeStats.maxHP);
    }
}
