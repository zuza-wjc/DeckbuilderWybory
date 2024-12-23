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

public class ChangeDeckNameController : MonoBehaviour
{
    public InputField inputField;
    public Text deckNameText;
    public Text placeholder;
    public GameObject changeDeckNamePanel;

    private string newDeckName;
    private string oldDeckName;

    void Start()
    {
        if (changeDeckNamePanel != null)
        {
            changeDeckNamePanel.SetActive(false);
        }
    }
    public void ChangeDeckName(Button clickedButton)
    {
        if (clickedButton != null)
        {
            // Pobieramy komponent Text z dziecka przycisku
            Text buttonText = clickedButton.GetComponentInChildren<Text>();
            oldDeckName = buttonText.text;

            if (buttonText != null)
            {
                // Otwórz panel zmiany nazwy
                if (changeDeckNamePanel != null)
                {
                    changeDeckNamePanel.SetActive(true); // Pokazuje panel
                }

                // Ustaw tekst placeholdera na nazwê z przycisku
                if (placeholder != null)
                {
                    placeholder.text = buttonText.text; // Ustawia placeholder na nazwê z przycisku
                }
                else
                {
                    Debug.LogWarning("Placeholder Text component is not assigned!");
                }
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
    public void GetInputText()
    {
        if (inputField != null)
        {
            newDeckName = inputField.text;

            if (string.IsNullOrWhiteSpace(newDeckName))
            {
                Debug.LogWarning("Nowa nazwa talii nie mo¿e byæ pusta!");
                return;
            }

            List<string> deckNames = LoadDeckNames();
            ChangeDeckKeyList(deckNames);

            changeDeckNamePanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("InputField is not assigned in the Inspector!");
        }
    }
    public void ChangeDeckKeyList(List<string> deckNames)
    {
        for (int i = 0; i < deckNames.Count; i++)
        {
            if (deckNames[i] == newDeckName)
            {
                Debug.LogWarning("Ta nazwa ju¿ istnieje: " + newDeckName);
                return;
            }

            if (deckNames[i] == oldDeckName)
            {
                Debug.Log("Zamienianie nazwy talii z " + oldDeckName + " na " + newDeckName);

                // Aktualizacja listy decków
                deckNames[i] = newDeckName;

                // Zapis listy do PlayerPrefs
                SaveDeckNames(deckNames);

                // Aktualizacja tekstu na now¹ nazwê
                if (deckNameText != null)
                {
                    deckNameText.text = newDeckName;
                }

                return;
            }
        }

        Debug.LogWarning("Nie znaleziono talii o nazwie: " + oldDeckName);
    }
    private List<string> LoadDeckNames()
    {
        // Pobieramy JSON z PlayerPrefs, domyœlnie jest to pusty JSON []
        string decksJson = PlayerPrefs.GetString("decks", "{\"items\":[]}");

        // Deserializujemy JSON do obiektu ListWrapper
        ListWrapper listWrapper = JsonUtility.FromJson<ListWrapper>(decksJson);

        // Zwracamy listê decków
        return listWrapper.items;
    }

    private void SaveDeckNames(List<string> deckNames)
    {
        ListWrapper listWrapper = new ListWrapper { items = deckNames };
        string decksJson = JsonUtility.ToJson(listWrapper);
        PlayerPrefs.SetString("decks", decksJson);
        PlayerPrefs.Save();

        Debug.Log("Lista talii zapisana: " + decksJson);
    }

    // Klasa pomocnicza do konwersji List<string> na JSON
    [System.Serializable]
    public class ListWrapper
    {
        public List<string> items; // Lista decków
    }
    public void ClosePanel()
    {
        changeDeckNamePanel.SetActive(false);
    }
}
