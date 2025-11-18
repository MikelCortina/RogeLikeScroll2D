using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using System.Collections;

public class CinematicManager : MonoBehaviour
{
    public static CinematicManager Instance;

    [Header("Cinematic Components")]
    public PlayableDirector cinematic;
    public GameObject cinematicPanel;
    public GameObject particleLoop;
    public Button continueButton;

    [Header("Accept / Reject Buttons")]
    public Button acceptButton;
    public Button rejectButton;

    [Header("Upgrade Object")]
    public GameObject upgradeObject;
    private SpriteRenderer upgradeRenderer;

    private bool cinematicPlaying = false;
    private bool cinematicFinished = false;

    private IObjetos currentUpgrade;

    public AudioSource audioSource;
    public AudioClip audioClip3;

    private void Awake()
    {
        Instance = this;

        // botón continuar
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnPressButton);
        }

        // botones aceptar / rechazar
        if (acceptButton != null)
        {
            acceptButton.onClick.RemoveAllListeners();
            acceptButton.onClick.AddListener(OnAccept);
            acceptButton.gameObject.SetActive(false);
        }

        if (rejectButton != null)
        {
            rejectButton.onClick.RemoveAllListeners();
            rejectButton.onClick.AddListener(OnReject);
            rejectButton.gameObject.SetActive(false);
        }

        if (upgradeObject != null)
        {
            upgradeRenderer = upgradeObject.GetComponent<SpriteRenderer>();
            upgradeObject.SetActive(false);
        }
    }

    public void StartCinematic()
    {
        cinematicPlaying = true;
        cinematicFinished = false;

        cinematicPanel.SetActive(true);
        particleLoop.SetActive(false);

        // Obtenemos objeto aleatorio
        currentUpgrade = ObjectManager.Instance.GetRandomObject();

        continueButton.gameObject.SetActive(true);

        // ocultar botones al inicio
        acceptButton.gameObject.SetActive(false);
        rejectButton.gameObject.SetActive(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        cinematic.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
        cinematic.Play();
        StartCoroutine(PauseNextFrame());

        cinematic.stopped += OnCinematicFinished;
    }

    private void OnCinematicFinished(PlayableDirector dir)
    {
        cinematicPlaying = false;
        cinematicFinished = true;

        particleLoop.SetActive(true);

        // Mostrar icono del objeto
        if (upgradeRenderer != null && currentUpgrade != null)
        {
            upgradeRenderer.sprite = currentUpgrade.icon;
            upgradeObject.SetActive(true);
        }

        // Activar botones aceptar/rechazar
        acceptButton.gameObject.SetActive(true);
        rejectButton.gameObject.SetActive(true);
        continueButton.gameObject.SetActive(false);
    }

    private IEnumerator PauseNextFrame()
    {
        yield return null;
        Time.timeScale = 0f;
    }

    public void PlaySoundExplosion()
    {
        audioSource.PlayOneShot(audioClip3);
    }

    // Botón continuar (SE USA SOLO PARA SALTAR LA CINEMÁTICA)
    public void OnPressButton()
    {
        if (cinematicPlaying)
        {
            PlaySoundExplosion();
            cinematic.time = cinematic.duration;
            cinematic.Evaluate();
            cinematic.Stop();
            cinematicPlaying = false;
            cinematicFinished = true;
            return;
        }
    }

    // -----------------------------
    //     BOTÓN ACEPTAR OBJETO
    // -----------------------------
    private void OnAccept()
    {
        if (currentUpgrade != null)
        {
            currentUpgrade.ApplyEffect();

            // ✔ Solo eliminar si NO es repetible
            if (!currentUpgrade.repetible)
                ObjectManager.Instance.allObjects.Remove(currentUpgrade);
        }

        CloseCinematic();
    }

    // -----------------------------
    //     BOTÓN RECHAZAR OBJETO
    // -----------------------------
    private void OnReject()
    {
        // No aplicamos la mejora
        CloseCinematic();
    }

    // -----------------------------
    //     SALIDA FINAL
    // -----------------------------
    private void CloseCinematic()
    {
        cinematicPanel.SetActive(false);
        particleLoop.SetActive(true);

        upgradeObject.SetActive(false);

        Time.timeScale = 1f;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        cinematicFinished = false;
        currentUpgrade = null;
    }
}
