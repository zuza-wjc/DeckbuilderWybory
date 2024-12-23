using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlusButtonController : MonoBehaviour
{
    public LoadDeckIconsController loadDeckIconsController;
    public Transform scrollContent;
    public GameObject tooMuchDecksPanel;
    private List<string> deckNames = new List<string>();

    // Funkcja wywo³ywana przy klikniêciu przycisku
    public void OnPlusButtonClicked()
    {
        Debug.Log($"Klikniêto przycisk +");

        // Sprawdzamy, czy scrollContent ma ju¿ 9 dzieci
        if (scrollContent.childCount >= 9)
        {
            Debug.LogError("Nie mo¿na dodaæ wiêcej ni¿ 8 talii. Limit osi¹gniêty.");
            tooMuchDecksPanel.SetActive(true);
            return;
        }
        // Tworzymy now¹ nazwê talii
        string newDeckName = "Nowa Talia";
        newDeckName = GetUniqueDeckName(newDeckName);


        // Dodajemy now¹ nazwê do listy
        deckNames.Add(newDeckName);

        // Sprawdzamy, czy LoadDeckIconsController jest przypisany, a potem przekazujemy listê
        if (loadDeckIconsController != null)
        {
            loadDeckIconsController.CreateDeckIcons(deckNames); // Przekazujemy listê nazw talii
            deckNames.Clear();
        }
        else
        {
            Debug.LogError("LoadDeckIconsController is not assigned!");
        }
    }
    private string GetUniqueDeckName(string baseName)
    {
        string uniqueName = baseName;
        int counter = 1;

        // Iterujemy przez wszystkie dzieci w scrollContent
        foreach (Transform child in scrollContent)
        {
            // Sprawdzamy, czy nazwa dziecka jest taka sama jak nasza nowa nazwa
            if (child.name == uniqueName)
            {
                // Jeœli nazwa ju¿ istnieje, dodajemy numer do nazwy
                uniqueName = baseName + " " + counter;
                counter++;
            }
        }

        return uniqueName;
    }
    public void CloseWarningPanel()
    {
        tooMuchDecksPanel.SetActive(false);
    }
}
