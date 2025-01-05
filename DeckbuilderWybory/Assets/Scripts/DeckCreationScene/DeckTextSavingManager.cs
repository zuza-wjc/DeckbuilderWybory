using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DeckTextSavingManager : MonoBehaviour
{
    [SerializeField]
    private Text DeckLabel;

    public void SaveDeckName(Button clickedButton)
    {
        if (clickedButton != null)
        {
            Text buttonText = clickedButton.GetComponentInChildren<Text>();

            if (buttonText != null)
            {
                DataManager.Instance.deckName = buttonText.text;

                AudioManager audioManager = FindObjectOfType<AudioManager>();

                if (audioManager != null)
                {
                    audioManager.PlaySoundForSceneChange(audioManager.buttonClickSound);
                }

                SceneManager.LoadScene("Deck Creation");
            }
            else
            {
                Debug.LogWarning("No Text component found in the clicked button's child!");
            }
        }
        else
        {
            Debug.LogWarning("Clicked button is null!");
        }
    }
}
