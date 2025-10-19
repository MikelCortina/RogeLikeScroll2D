using UnityEngine;

public class ResponsiveElement : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private RectTransform rectTransform;
 
    [Header("Mobile Anchors")]
    [SerializeField] private Vector2 mobileAnchorMin = new Vector2(0, 0);
    [SerializeField] private Vector2 mobileAnchorMax = new Vector2(0, 0);
    
    [Header("Tablet Anchors")]
    [SerializeField] private Vector2 tabletAnchorMin = new Vector2(0, 0);
    [SerializeField] private Vector2 tabletAnchorMax = new Vector2(0, 0);
    
    ResponsiveManager _responsiveManager;
    
    void Start()
    {
        _responsiveManager = ResponsiveManager.Instance;
        UpdateAnchors();    
    }
    
    public void UpdateAnchors()
    {
        if(_responsiveManager == null) return;
        
        if(_responsiveManager.CurrentDeviceType == DeviceType.Mobile)
        {
            rectTransform.anchorMin = mobileAnchorMin;
            rectTransform.anchorMax = mobileAnchorMax;
        }
        else if(_responsiveManager.CurrentDeviceType == DeviceType.Tablet)
        {
            rectTransform.anchorMin = tabletAnchorMin;
            rectTransform.anchorMax = tabletAnchorMax;
        }
    }
    
    private void SetMobileAnchors()
    {
        Vector2 maxAnchors = rectTransform.anchorMax;
        Vector2 minAnchors = rectTransform.anchorMin;
        
        mobileAnchorMax = maxAnchors;
        mobileAnchorMin = minAnchors;
        
        UpdateAnchors();
    }
    
    private void SetTabletAnchors()
    {
        Vector2 maxAnchors = rectTransform.anchorMax;
        Vector2 minAnchors = rectTransform.anchorMin;
        
        tabletAnchorMax = maxAnchors;
        tabletAnchorMin = minAnchors;
        
        UpdateAnchors();
    }

}