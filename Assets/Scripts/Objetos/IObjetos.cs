using UnityEngine;
public abstract class IObjetos : ScriptableObject
{
    public bool repetible;
    public ObjectQuality quality;
    public Sprite icon;

    public virtual void ApplyEffect()
    {
        // Puede quedar vacío, o agregar un log
    }
}
public enum ObjectQuality
{
    Rare,       // rara
    Epic,       // épica
    Legendary   // legendaria
}
