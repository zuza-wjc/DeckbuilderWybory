using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadDeckIconsController : MonoBehaviour
{
    public GameObject ChooseDeckIconPrefab; // Prefab obiektu ikony talii
    public Transform Panel; // Referencja do obiektu Panel, kt�ry b�dzie rodzicem ikon

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

            // Zwi�kszamy szeroko�� i skal�
            Vector3 newScale = icon.transform.localScale;
            newScale.x *= 1.1f; // Zwi�kszamy szeroko�� o 20%
            icon.transform.localScale = newScale;

            icon.transform.localScale *= 100.0f;
            Debug.Log($"Aktualny siblingCount {siblingCount}.");
            if (siblingCount > 0)
            {
                icon.transform.SetSiblingIndex(siblingCount - 1); // Indeks na przedostatnie dziecko
            }
        }
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
