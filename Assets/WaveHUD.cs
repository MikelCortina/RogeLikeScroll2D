using UnityEngine;
using TMPro;
using System.Collections;

public class WaveHUD : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI waveSpaceText;

    [Header("Options")]
    [Tooltip("Si está activado, actualizará el Wave Space cada frame (útil si cambia durante la ola).")]
    public bool realtimeUpdateWaveSpace = false;

    private WaveManager waveManager;

    private void Awake()
    {
        // Solo comprobaciones tempranas
        if (waveText == null) Debug.LogWarning("[WaveHUD] waveText no asignado en el Inspector.");
        if (waveSpaceText == null) Debug.LogWarning("[WaveHUD] waveSpaceText no asignado en el Inspector.");
    }

    private void OnEnable()
    {
        // Intentamos suscribirnos si ya hay instancia
        TrySubscribeToWaveManager();
    }

    private void Start()
    {
        // Si aún no tenemos WaveManager, intentamos encontrarlo en la escena y suscribirnos.
        if (waveManager == null)
        {
            waveManager = WaveManager.Instance;
            if (waveManager == null)
            {
                waveManager = FindObjectOfType<WaveManager>();
            }
        }

        if (waveManager == null)
        {
            // Si todavía no existe, arrancamos una coroutine que esperará unos frames a que aparezca.
            StartCoroutine(WaitForWaveManagerAndSubscribe(2f)); // espera hasta 2s como máximo
        }
        else
        {
            Subscribe();
            // Mostrar estado inicial si es posible
            UpdateWaveTextInitial();
        }
    }

    private IEnumerator WaitForWaveManagerAndSubscribe(float timeoutSeconds)
    {
        float timer = 0f;
        while (timer < timeoutSeconds)
        {
            waveManager = WaveManager.Instance ?? FindObjectOfType<WaveManager>();
            if (waveManager != null)
            {
                Subscribe();
                UpdateWaveTextInitial();
                yield break;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        Debug.LogError("[WaveHUD] No se encontró WaveManager en la escena. Asegúrate de que WaveManager esté presente y activo.");
    }

    private void TrySubscribeToWaveManager()
    {
        if (waveManager == null)
            waveManager = WaveManager.Instance;

        if (waveManager != null)
            Subscribe();
    }

    private void Subscribe()
    {
        // Evitar doble suscripción
        waveManager.OnWaveStarted -= UpdateWaveHUD;
        waveManager.OnWaveStarted += UpdateWaveHUD;
    }

    private void Unsubscribe()
    {
        if (waveManager != null)
            waveManager.OnWaveStarted -= UpdateWaveHUD;
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (realtimeUpdateWaveSpace && waveSpaceText != null)
        {
            if (waveManager == null) waveManager = WaveManager.Instance;
            if (waveManager != null)
            {
                waveSpaceText.text = "Wave Space: " + waveManager.currentWaveSpace.ToString("0.0");
            }
        }
    }

    private void UpdateWaveHUD(int waveNumber, int enemiesToSpawn)
    {
        if (waveText != null)
            waveText.text = "Wave: " + waveNumber;

        if (waveSpaceText != null && waveManager != null)
            waveSpaceText.text = "Wave Space: " + waveManager.currentWaveSpace.ToString("0.0");
    }

    private void UpdateWaveTextInitial()
    {
        // Mostrar algo al inicio si ya existe información
        if (waveManager == null) return;

        if (waveText != null)
            waveText.text = "Wave: " + waveManager.currentWave.ToString();

        if (waveSpaceText != null)
            waveSpaceText.text = "Wave Space: " + waveManager.currentWaveSpace.ToString("0.0");
    }
}
