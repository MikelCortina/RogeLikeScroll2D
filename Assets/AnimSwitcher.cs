using UnityEngine;

public class AnimSwitcher : MonoBehaviour
{
    public Animator animator;

    public void LanzarSiguienteAnimacion()
    {
        animator.Play("Idle");
    }
}