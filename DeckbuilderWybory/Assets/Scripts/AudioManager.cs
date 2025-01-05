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
        if (!DataTransfer.IsMuted)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }

    public void PlayCardDroppedSound()
    {
        if (!DataTransfer.IsMuted)
        {
            audioSource.PlayOneShot(cardDroppedSound);
        }
    }

    public void PlayEndTurnSound()
    {
        if (!DataTransfer.IsMuted)
        {
            audioSource.PlayOneShot(endTurnSound);
        }
    }

    public void PlayStartTurnSound()
    {
        if (!DataTransfer.IsMuted)
        {
            audioSource.PlayOneShot(startTurnSound);
        }
    }

    public void PlaySoundForSceneChange(AudioClip clip)
    {
        if (!DataTransfer.IsMuted)
        {
            GameObject soundPlayer = new GameObject("SceneChangeSoundPlayer");
            AudioSource tempAudioSource = soundPlayer.AddComponent<AudioSource>();
            tempAudioSource.clip = clip;
            tempAudioSource.Play();

            DontDestroyOnLoad(soundPlayer);
            Destroy(soundPlayer, clip.length);
        }
    }
}
