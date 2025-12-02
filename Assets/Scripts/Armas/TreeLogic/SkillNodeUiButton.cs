using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class SkillNodeButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Components")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public GameObject lockedOverlay;
    public Button button;

    [Header("Node Data")]
    public ItemNode node;

    private SkillTreeUI treeUI;
    private bool pointerOver = false;

    [Header("Audio")]
    public AudioSource sfxSource; // ← Este será el que usemos siempre       
    public AudioClip releaseSound;

    private void Awake()
    {
        EnsureAudioSource();
    }
    public void Initialize(SkillTreeUI ui)
    {
        treeUI = ui;
        EnsureAudioSource();  // ¡Importantísimo volver a buscar aquí también!

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }

        if (node != null)
            RefreshVisuals();

        UpdateState();
    }

    public void SetNode(ItemNode newNode)
    {
        node = newNode;
        RefreshVisuals();
        UpdateState();
    }

    private void RefreshVisuals()
    {
        if (node == null)
        {
            if (iconImage != null) iconImage.sprite = null;
            if (nameText != null) nameText.text = "";
            if (costText != null) costText.text = "";
            return;
        }

        if (iconImage != null) iconImage.sprite = node.icon;

        var mainImage = GetComponent<Image>();
        if (mainImage != null) mainImage.sprite = node.icon;

        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(node.displayName) ? node.nodeId : node.displayName;

        if (costText != null && treeUI != null)
        {
            int cost = treeUI.GetNodeEffectiveCost(node);
            costText.text = cost > 0 ? cost.ToString() : "";
        }
    }

    public void UpdateState()
    {
        if (node == null || treeUI == null)
        {
            if (button != null) button.interactable = false;
            if (lockedOverlay != null) lockedOverlay.SetActive(true);
            return;
        }

        bool unlocked = treeUI.IsUnlocked(node.nodeId);
        bool canUnlock = treeUI.CanUnlock(node);

        if (lockedOverlay != null)
            lockedOverlay.SetActive(!unlocked && !canUnlock);

        if (button != null)
            button.interactable = !unlocked && canUnlock;
    }
    private void EnsureAudioSource()
    {
        if (sfxSource == null)
        {
            // 1. Busca en el propio GameObject
            sfxSource = GetComponent<AudioSource>();

            // 2. Si no lo encuentra, busca en TODOS los hijos (incluso desactivados)
            if (sfxSource == null)
                sfxSource = GetComponentInChildren<AudioSource>(true); // ← el "true" es la clave

            // 3. Si sigue sin encontrarlo, lo crea automáticamente (así nunca falla)
            if (sfxSource == null)
            {
                GameObject sfxObj = new GameObject("SFX_AutoCreated");
                sfxObj.transform.SetParent(transform);
                sfxObj.transform.localPosition = Vector3.zero;
                sfxSource = sfxObj.AddComponent<AudioSource>();
                Debug.Log("[SkillNodeButton] AudioSource creado automáticamente", this);
            }
        }

        // Configuración recomendada para UI
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.volume = 0.5f;
    }

    private void OnClick()
    {
        if (treeUI == null || node == null) return;

        if (!treeUI.CanUnlock(node))
        {
            Debug.Log($"[SkillTree] No puedes desbloquear {node.nodeId}: faltan requisitos o puntos.");
            return;
        }

        // ← Seguridad extra
        if (sfxSource != null && releaseSound != null)
            sfxSource.PlayOneShot(releaseSound);
        else if (releaseSound != null)
            Debug.LogWarning("[SkillNodeButton] AudioSource es null, no se reproduce sonido", this);

        treeUI.TryUnlock(node);
    }

    // ================== TOOLTIP FIJO ==================

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerOver = true;
        TooltipController.Instance?.Show(node, Vector2.zero, treeUI);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerOver = false;
        TooltipController.Instance?.Hide();
    }

    // Ya no necesitas OnPointerMove → eliminado

    private void OnDisable()
    {
        if (pointerOver)
            TooltipController.Instance?.Hide();
    }

    private void OnDestroy()
    {
        if (pointerOver)
            TooltipController.Instance?.Hide();
    }

}