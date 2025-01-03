using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveAndExitManager : MonoBehaviour
{
    public GameObject saveAndExitPanel;
    public AddCardsPanelController addCardsPanelController;

    public void OnLeave()
    {
        saveAndExitPanel.SetActive(true);
    }

    public void OnExit()
    {
        AudioManager audioManager = FindObjectOfType<AudioManager>();

        if (audioManager != null)
        {
            audioManager.PlayButtonClickSound();
            StartCoroutine(LoadSceneAfterSound("Deck Building", audioManager.buttonClickSound.length / 2));
        }
        else
        {
            SceneManager.LoadScene("Deck Building");
        }

        saveAndExitPanel.SetActive(false);
    }

    public void OnSave()
    {
        AudioManager audioManager = FindObjectOfType<AudioManager>();

        if (audioManager != null)
        {
            audioManager.PlayButtonClickSound();
            StartCoroutine(LoadSceneAfterSound("Deck Building", audioManager.buttonClickSound.length / 2));
        }
        else
        {
            SceneManager.LoadScene("Deck Building");
        }

        addCardsPanelController.SaveDeck();
    }

    private IEnumerator LoadSceneAfterSound(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }
}
