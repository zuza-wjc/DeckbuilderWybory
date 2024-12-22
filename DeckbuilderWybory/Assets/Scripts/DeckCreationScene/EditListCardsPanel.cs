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

public class EditListCardsPanel : MonoBehaviour
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
    }

    // Metoda obs³uguj¹ca klikniêcie przycisku
    public void OnCardButtonClick(Button button)
    {
        // Pobierz nazwê klikniêtego przycisku
        string cardId = button.gameObject.name;
        Debug.Log($"Klikniêto przycisk z ID karty: {cardId}");

    }

}
