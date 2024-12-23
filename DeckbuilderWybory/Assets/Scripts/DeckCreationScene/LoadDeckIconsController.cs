using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadDeckIconsController : MonoBehaviour
{
    public GameObject ChooseDeckIconPrefab; // Prefab obiektu ikony talii
    public Transform Panel; // Referencja do obiektu Panel, który bêdzie rodzicem ikon

    void Start()
    {
        // Pobierz listê nazw talii z PlayerPrefs
        List<string> deckNames = LoadDeckNames();

        // Utwórz ikony na podstawie listy talii
        CreateDeckIcons(deckNames);
    }

    public void CreateDeckIcons(List<string> deckNames)
    {
        foreach (string deckName in deckNames)
        {
            // Obliczamy liczbê dzieci przed dodaniem nowego obiektu
            int siblingCount = Panel.childCount;

            // Tworzymy obiekt prefabrykatu i przypisujemy go jako dziecko obiektu Panel
            GameObject icon = Instantiate(ChooseDeckIconPrefab, Panel);

            // Ustawiamy nazwê obiektu
            icon.name = deckName;

            // Znajdujemy komponent tekstowy dziecka i ustawiamy jego tekst na nazwê talii
            Text deckNameText = icon.GetComponentInChildren<Text>();
            if (deckNameText != null)
            {
                deckNameText.text = deckName;
            }

            // Znajdujemy komponent Button i przypisujemy zdarzenie klikniêcia
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

            // Zwiêkszamy szerokoœæ i skalê
            Vector3 newScale = icon.transform.localScale;
            newScale.x *= 1.1f; // Zwiêkszamy szerokoœæ o 20%
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
        // Pobieramy JSON z PlayerPrefs, domyœlnie jest to pusty JSON []
        string decksJson = PlayerPrefs.GetString("decks", "{\"items\":[]}");

        // Deserializujemy JSON do obiektu ListWrapper
        ListWrapper listWrapper = JsonUtility.FromJson<ListWrapper>(decksJson);

        // Zwracamy listê decków
        return listWrapper.items;
    }

    // Klasa pomocnicza do konwersji List<string> na JSON
    [System.Serializable]
    public class ListWrapper
    {
        public List<string> items; // Lista decków
    }
}
