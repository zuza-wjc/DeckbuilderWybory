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
            audioManager.PlaySoundForSceneChange(audioManager.buttonClickSound);
        }

        SceneManager.LoadScene("Deck Building");
        saveAndExitPanel.SetActive(false);
    }

    public void OnSave()
    {
        AudioManager audioManager = FindObjectOfType<AudioManager>();

        if (audioManager != null)
        {
            audioManager.PlaySoundForSceneChange(audioManager.buttonClickSound);
        }

        SceneManager.LoadScene("Deck Building");
        addCardsPanelController.SaveDeck();
    }
}
