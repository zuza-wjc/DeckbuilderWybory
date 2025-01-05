using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    public string sceneName;

    public void ChangeToScene()
    {
        AudioManager audioManager = FindObjectOfType<AudioManager>();

        if (audioManager != null)
        {
            audioManager.PlaySoundForSceneChange(audioManager.buttonClickSound);
        }

        SceneManager.LoadScene(sceneName);
    }

    public void ChangeToSceneOnTop()
    {
        AudioManager audioManager = FindObjectOfType<AudioManager>();

        if (audioManager != null)
        {
            audioManager.PlaySoundForSceneChange(audioManager.buttonClickSound);
        }

        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }
}
