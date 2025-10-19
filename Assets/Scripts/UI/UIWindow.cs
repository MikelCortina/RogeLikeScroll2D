using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

public class UIWindow : MonoBehaviour
{
    #region properties
    [Header("Settings")] 
    [SerializeField] private string windowID;
    [SerializeField] private Canvas windowCanvas;
    [SerializeField] private CanvasGroup windowCanvasGroup;
    
    [Header("Options")]
    [SerializeField] private bool hideOnStart = true;
    [SerializeField] private float animationTime = 0.5f;
    [SerializeField] private Ease easeShow = Ease.InBack;
    [SerializeField] private Ease easeHide = Ease.OutBack;
    
    public UnityEvent OnStartShowingUI { get; private set; } = new UnityEvent();
    public UnityEvent OnFinishShowingUI { get; private set; } = new UnityEvent();
    public UnityEvent OnStartHidingUI { get; private set; } = new UnityEvent();
    public UnityEvent OnFinishHidingUI { get; private set; } = new UnityEvent();
    
    public bool IsShowing { get; private set; } = false;
    public string WindowID => windowID;
    #endregion

    public void Start()
    {
        Initialize();
    }

    public virtual void Initialize()
    {
        if (hideOnStart) Hide(instant: true);
    }

    [Button]
    public virtual void Show(bool instant = false)
    {
        if (IsShowing) return;
        windowCanvas.gameObject.SetActive(true);
        
        if (instant)
        {
            windowCanvasGroup.transform.DOScale(Vector3.one, 0f);
        }
        else
        {
            windowCanvasGroup.transform.DOScale(Vector3.one, animationTime).SetEase(easeShow);
        }
    }

    [Button]
    public virtual void Hide(bool instant = false)
    {
        if (instant)
        {
            windowCanvasGroup.transform.DOScale(Vector3.zero, 0f);
        }
        else
        {
            windowCanvasGroup.transform.DOScale(Vector3.zero, animationTime).SetEase(easeHide).OnComplete(()=>
            {
                windowCanvas.gameObject.SetActive(false);
                IsShowing = false;
            });
        }
    }
}
