using UnityEngine;
public abstract class IObjetos : ScriptableObject
{
    public bool repetible;
    public ObjectQuality quality;
    public Sprite icon;
    [TextArea] public string description;

    public virtual void ApplyEffect()
    {

        // Puede quedar vacío, o agregar un log
    }

    public void ActivarPickup(ScriptableObject efecto)
    {
        RunInventory.Instance.AddItem(efecto, 1);
    }
}
public enum ObjectQuality
{
    Rare,       // rara
    Epic,       // épica
    Legendary   // legendaria
}
