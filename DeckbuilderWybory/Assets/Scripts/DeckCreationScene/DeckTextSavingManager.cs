using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using System.Collections;
using System.Collections.Generic;
using Firebase;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.Linq;
using System;

public class DeckTextSavingManager : MonoBehaviour
{
    // Prywatna zmienna DeckLabel
    [SerializeField]
    private Text DeckLabel;  

    // Metoda wywo³ywana przy klikniêciu przycisku
    public void SaveDeckName(Button clickedButton)
    {
        if (clickedButton != null)
        {
            // Pobieramy komponent Text z dziecka przycisku
            Text buttonText = clickedButton.GetComponentInChildren<Text>();

            if (buttonText != null)
            {
                // Zapisujemy tekst do DataManager
                DataManager.Instance.deckName = buttonText.text;
                Debug.Log("Saved deckName: " + DataManager.Instance.deckName);
                SceneManager.LoadScene("Deck Creation"); // Zmiana sceny 
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
