using UnityEngine;

public class AudioPlay : MonoBehaviour
{
   public  AudioSource audioSource;
    public AudioClip audioClip;
    public AudioClip audioClip2;
    public AudioClip audioClip3;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlaySoundLatido1()
    {
               audioSource.PlayOneShot(audioClip);  
    }
    public void PlaySoundLatido2()
    {
        audioSource.PlayOneShot(audioClip2);
    }
    public void PlaySoundExplosion()
    {
        audioSource.PlayOneShot(audioClip3);
    }
}
