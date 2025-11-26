using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class CinematicManager : MonoBehaviour
{
    public static CinematicManager Instance;

    [Header("Cinematic Components")]
    public PlayableDirector cinematic;
    public GameObject cinematicPanel;
    public GameObject particleLoop;
    public Button continueButton;

    [Header("Descripción del objeto")]
    public TextMeshProUGUI nameText;
    public GameObject descriptionPanel;      // Panel que contiene la descripción
    public TextMeshProUGUI descriptionText;
    public UnityEngine.UI.Image[] panelImage; // ← referencia al Image del panel

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

    public event Action onCinematicEnd;

    [Header("Target for Cinematic")]
    public GameObject cinematicTarget; // El objeto que se moverá a la posición del trigger


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

    public void MoveCinematicToTarget(Vector3 position)
    {
        if (cinematicTarget != null)
        {
            cinematicTarget.SetActive(true);
            cinematicTarget.transform.position = position;
        }
    }   

    private void OnCinematicFinished(PlayableDirector dir)
    {
        // Llamamos al evento al terminar
     
        cinematicPlaying = false;
        cinematicFinished = true;

        particleLoop.SetActive(true);

        // Mostrar icono del objeto
        if (upgradeRenderer != null && currentUpgrade != null)
        {
            upgradeRenderer.sprite = currentUpgrade.icon;
            upgradeObject.SetActive(true);
        }

        // Mostrar descripción del objeto
        if (descriptionPanel != null)
        {
            descriptionPanel.SetActive(true);

          foreach(var img in panelImage)
            {
                if (img != null && currentUpgrade != null)
                    img.color = GetColorByRarity(currentUpgrade.quality);
            }

            if (nameText != null)
                nameText.text = currentUpgrade.name;  // Nombre del ScriptableObject


            if (descriptionText != null && currentUpgrade != null)
                descriptionText.text = currentUpgrade.description;   // 👈 campo del objeto
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
        cinematicTarget.SetActive(false);
        onCinematicEnd?.Invoke();
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
        cinematicTarget.SetActive(false);
        onCinematicEnd?.Invoke();
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
        if (descriptionPanel != null)
            descriptionPanel.SetActive(false);
    }

    private Color GetColorByRarity(ObjectQuality quality)
    {
        switch (quality)
        {
            case ObjectQuality.Rare:
                return new Color(173f / 255f, 216f / 255f, 230f / 255f); // #ADD8E6
            case ObjectQuality.Epic:
                return new Color(216f / 255f, 191f / 255f, 216f / 255f); // #D8BFD8
            case ObjectQuality.Legendary:
                return new Color(255f / 255f, 160f / 255f, 122f / 255f); // #FFA07A
            default:
                return Color.white;
        }
    }

}
