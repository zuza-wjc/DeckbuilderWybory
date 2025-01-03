using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioClip buttonClickSound;
    public AudioClip cardDroppedSound;
    public AudioClip endTurnSound;
    public AudioClip startTurnSound;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayButtonClickSound()
    {
        audioSource.PlayOneShot(buttonClickSound);
    }

    public void PlayCardDroppedSound()
    {
        audioSource.PlayOneShot(cardDroppedSound);
    }

    public void PlayEndTurnSound()
    {
        Debug.Log("end sound will play");
        audioSource.PlayOneShot(endTurnSound);
        Debug.Log("end sound played");
    }

    public void PlayStartTurnSound()
    {
        Debug.Log("start sound will play");
        audioSource.PlayOneShot(startTurnSound);
        Debug.Log("start sound played");
    }
}
