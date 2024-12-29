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
using Firebase.Extensions;

public class CardButtonController : MonoBehaviour
{
    public Button[] cardButtons;
    public AddCardsPanelController addCardPanelController;

    public string cardId { get; private set; }
    public string type { get; private set; }
    public string cardName { get; private set; }
    public int maxDeckNumber { get; private set; }

    DatabaseReference dbRef;  // Zmienna, która przechowuje referencjê do bazy danych Firebase

    void Start()
    {
        // INICJALIZACJA FIREBASE
        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        // U¿ywamy ju¿ zainicjalizowanej referencji z FirebaseInitializer
        dbRef = FirebaseInitializer.DatabaseReference.Child("cards");

        // Sprawdzenie, czy tablica cardButtons jest przypisana
        if (cardButtons != null && cardButtons.Length > 0)
        {
            foreach (Button button in cardButtons)
            {
                // Dodaj listener z parametrem przycisku
                button.onClick.AddListener(() => OnCardButtonClick(button));
            }
        }
        else
        {
            Debug.LogError("Brak przypisanych przycisków do CardButtonControler!");
        }
    }

    // Metoda obs³uguj¹ca klikniêcie przycisku
    void OnCardButtonClick(Button button)
    {
        // Pobierz nazwê klikniêtego przycisku
        string cardId = button.gameObject.name;

        // Wywo³aj funkcjê przetwarzaj¹c¹ dane
      //  Debug.Log($"Klikniêto przycisk z ID karty: {cardId}");

        FetchCardData(cardId);
    }

    async void FetchCardData(string cardId)
    {
        try
        {
            // Pobierz wszystkie dostêpne grupy w "cards/id"
            DataSnapshot snapshot = await dbRef.Child("id").GetValueAsync();

            if (snapshot.Exists)
            {
                foreach (DataSnapshot groupSnapshot in snapshot.Children)
                {
                    if (groupSnapshot.HasChild(cardId))
                    {
                        string path = $"id/{groupSnapshot.Key}/{cardId}";
                        DataSnapshot cardDataSnapshot = await dbRef.Child(path).GetValueAsync();

                        if (cardDataSnapshot.Exists)
                        {
                            string type = cardDataSnapshot.Child("type").Value.ToString();
                            string cardName = cardDataSnapshot.Child("name").Value.ToString();
                            int maxDeckNumber = int.Parse(cardDataSnapshot.Child("maxDeckNumber").Value.ToString());

                            addCardPanelController.ShowPanel(cardId, type, maxDeckNumber, cardName);
                            return;
                        }
                        else
                        {
                            Debug.LogWarning($"Nie znaleziono danych karty w œcie¿ce: {path}");
                        }
                    }
                    else
                    {
                      //  Debug.LogWarning($"Karta {cardId} nie jest potomkiem grupy {groupSnapshot.Key}");
                    }
                }

                Debug.LogWarning($"Nie znaleziono karty o ID: {cardId}");
            }
            else
            {
                Debug.LogWarning("Brak danych w bazie.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"B³¹d podczas pobierania danych: {ex.Message}");
        }
    }




}

