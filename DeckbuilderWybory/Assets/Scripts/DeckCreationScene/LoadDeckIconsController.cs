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

public class LoadDeckIconsController : MonoBehaviour
{
    public GameObject ChooseDeckIconPrefab; // Prefab obiektu ikony talii
    public Transform Panel; // Referencja do obiektu Panel, kt�ry b�dzie rodzicem ikon
    public GameObject deleteDeckPanel;
    public string deckNameToDelete = ""; // Zmienna przechowuj�ca nazw� decka do usuni�cia

    void Start()
    {
        // Pobierz list� nazw talii z PlayerPrefs
        List<string> deckNames = LoadDeckNames();

        // Utw�rz ikony na podstawie listy talii
        CreateDeckIcons(deckNames);
    }

    public void CreateDeckIcons(List<string> deckNames)
    {
        foreach (string deckName in deckNames)
        {
            // Obliczamy liczb� dzieci przed dodaniem nowego obiektu
            int siblingCount = Panel.childCount;

            // Tworzymy obiekt prefabrykatu i przypisujemy go jako dziecko obiektu Panel
            GameObject icon = Instantiate(ChooseDeckIconPrefab, Panel);

            // Ustawiamy nazw� obiektu
            icon.name = deckName;

            // Znajdujemy komponent tekstowy dziecka i ustawiamy jego tekst na nazw� talii
            Text deckNameText = icon.GetComponentInChildren<Text>();
            if (deckNameText != null)
            {
                deckNameText.text = deckName;
            }

            // Znajdujemy komponent Button i przypisujemy zdarzenie klikni�cia
            Button button = icon.GetComponent<Button>();
            if (button != null)
            {
                DeckTextSavingManager manager = FindObjectOfType<DeckTextSavingManager>();
                if (manager != null)
                {
                    button.onClick.AddListener(() => manager.SaveDeckName(button));
                }
                else
                {
                    Debug.LogWarning("DeckTextSavingManager not found in the scene!");
                }
            }
            else
            {
                Debug.LogWarning("No Button component found in the prefab!");
            }

            // Obs�uga przycisku DeleteButton
            Transform deleteButtonTransform = icon.transform.Find("DeleteButton");
            if (deleteButtonTransform != null)
            {
                Button deleteButton = deleteButtonTransform.GetComponent<Button>();
                if (deleteButton != null)
                {
                    deleteButton.onClick.AddListener(() =>
                    {
                        //Debug.Log($"Delete button clicked for deck: {deckName}");
                        deckNameToDelete = deckName;
                        OpenDeletePanel();
                    });
                }
                else
                {
                    Debug.LogWarning("DeleteButton does not have a Button component!");
                }
            }
            else
            {
                Debug.LogWarning("DeleteButton child not found in the prefab!");
            }

            // Zwi�kszamy szeroko�� i skal�
            Vector3 newScale = icon.transform.localScale;
            newScale.x *= 1.1f; // Zwi�kszamy szeroko�� o 20%
            icon.transform.localScale = newScale;

            icon.transform.localScale *= 100.0f;
            if (siblingCount > 0)
            {
                icon.transform.SetSiblingIndex(siblingCount - 1); // Indeks na przedostatnie dziecko
            }
        }
    }

    public void DeleteDeck()
    {
        if (!string.IsNullOrEmpty(deckNameToDelete))
        {
            if (PlayerPrefs.HasKey(deckNameToDelete))
            {
                PlayerPrefs.DeleteKey(deckNameToDelete);
                //Debug.Log($"Deck '{deckNameToDelete}' has been deleted from PlayerPrefs.");
            }
            else
            {
                Debug.LogWarning($"Deck '{deckNameToDelete}' does not exist in PlayerPrefs.");
            }

            List<string> deckNames = LoadDeckNames();
            for (int i = 0; i < deckNames.Count; i++)
            {
                

                if (deckNames[i] == deckNameToDelete)
                {
                   // Debug.Log("Usuwanie klucza:" + deckNameToDelete);
                    deckNames.RemoveAt(i);
                    SaveDeckNames(deckNames);
                    Transform iconToDelete = Panel.Find(deckNameToDelete);
                    if (iconToDelete != null)
                    {
                        Destroy(iconToDelete.gameObject); 
                       // Debug.Log($"Icon for deck '{deckNameToDelete}' has been destroyed.");
                    }
                    else
                    {
                        Debug.LogWarning($"Icon for deck '{deckNameToDelete}' not found in the panel.");
                    }
                    CloseDeletePanel();

                    return;
                }
            }
        }
        else
        {
            Debug.LogWarning("Deck name is not set or is empty.");
        }
    }

    public void CloseDeletePanel()
    {
        deleteDeckPanel.SetActive(false);
    }
    
    private void OpenDeletePanel()
    {
        // Aktywuj panel
        deleteDeckPanel.SetActive(true);
    }
    private void SaveDeckNames(List<string> deckNames)
    {
        ListWrapper listWrapper = new ListWrapper { items = deckNames };
        string decksJson = JsonUtility.ToJson(listWrapper);
        PlayerPrefs.SetString("decks", decksJson);
        PlayerPrefs.Save();

        //Debug.Log("Lista talii zapisana: " + decksJson);
        
    }

    private List<string> LoadDeckNames()
    {
        // Pobieramy JSON z PlayerPrefs, domy�lnie jest to pusty JSON []
        string decksJson = PlayerPrefs.GetString("decks", "{\"items\":[]}");

        // Deserializujemy JSON do obiektu ListWrapper
        ListWrapper listWrapper = JsonUtility.FromJson<ListWrapper>(decksJson);

        // Zwracamy list� deck�w
        return listWrapper.items;
    }

    // Klasa pomocnicza do konwersji List<string> na JSON
    [System.Serializable]
    public class ListWrapper
    {
        public List<string> items; // Lista deck�w
    }
}
