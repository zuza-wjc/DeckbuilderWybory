using System.Collections;
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
            audioManager.PlayButtonClickSound();
            StartCoroutine(LoadSceneAfterSoundNormal(sceneName, audioManager.buttonClickSound.length / 2));
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    public void ChangeToSceneOnTop()
    {
        AudioManager audioManager = FindObjectOfType<AudioManager>();

        if (audioManager != null)
        {
            audioManager.PlayButtonClickSound();
            StartCoroutine(LoadSceneAfterSoundAdditive(sceneName, audioManager.buttonClickSound.length / 2));
        }
        else
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }
    }

    private IEnumerator LoadSceneAfterSoundNormal(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator LoadSceneAfterSoundAdditive(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }

}
