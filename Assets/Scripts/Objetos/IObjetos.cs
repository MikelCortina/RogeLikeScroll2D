using UnityEngine;
public class IObjetos : ScriptableObject
{
    public bool repetible;
    public ObjectQuality quality;
    public Sprite icon;
    public void ApplyEffect()
    {
    }
}
public enum ObjectQuality
{
    Rare,       // rara
    Epic,       // épica
    Legendary   // legendaria
}
