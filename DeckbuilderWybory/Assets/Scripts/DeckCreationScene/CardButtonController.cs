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
        Debug.Log($"Klikniêto przycisk z ID karty: {cardId}");

        FetchCardData(cardId);
    }

    async void FetchCardData(string cardId)
    {
        string path = $"id/addRemove/{cardId}";
        try
        {
            // Await the asynchronous Firebase call and get the data
            DataSnapshot snapshot = await dbRef.Child(path).GetValueAsync();

            if (snapshot.Exists)
            {
                string type = snapshot.Child("type").Value.ToString();
                string cardName = snapshot.Child("name").Value.ToString();
                int maxDeckNumber = int.Parse(snapshot.Child("maxDeckNumber").Value.ToString());

                Debug.Log($"ID Karty: {cardId}");
                Debug.Log($"Typ: {type}");
                Debug.Log($"Nazwa: {cardName}");
                Debug.Log($"Max Deck Number: {maxDeckNumber}");

                addCardPanelController.ShowPanel(cardId, type, maxDeckNumber, cardName);
            }
            else
            {
                Debug.LogWarning($"Nie znaleziono karty o ID: {cardId}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"B³¹d podczas pobierania danych: {ex.Message}");
        }
    }


}

