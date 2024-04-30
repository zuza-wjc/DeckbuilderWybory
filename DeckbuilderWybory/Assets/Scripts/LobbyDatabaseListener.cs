using Firebase.Database;
using TMPro;
using UnityEngine;

public class LobbyDatabaseListener : MonoBehaviour
{
    DatabaseReference sessionRef;

    public TextMeshProUGUI[] playerNameTexts; // Tablica pól tekstowych do wyœwietlania nazw graczy
    private int currentPlayerIndex = 0; // Indeks aktualnie dostêpnego pola tekstowego

    void Start()
    {
        // Inicjalizacja referencji do bazy danych Firebase
        sessionRef = FirebaseDatabase.DefaultInstance.RootReference.Child("sessions/lobby_1");
        StartListening();
    }

    void StartListening()
    {
        // Nas³uchiwanie zmian w danej lokalizacji
        sessionRef.ChildAdded += HandleChildAdded;
        sessionRef.ChildRemoved += HandleChildRemoved;
    }

    void HandleChildAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        // Obs³uga dodania nowego dziecka
        Debug.Log("New child added: " + args.Snapshot.Key);

        // Sprawdzamy, czy nowy gracz ma ustawion¹ nazwê i czy istnieje dostêpne pole tekstowe
        if (args.Snapshot.Child("name").Exists && currentPlayerIndex < playerNameTexts.Length)
        {
            string playerName = args.Snapshot.Child("name").GetValue(true).ToString();
            playerNameTexts[currentPlayerIndex].text = playerName; // Aktualizujemy nazwê gracza na scenie
            currentPlayerIndex++; // Przechodzimy do nastêpnego pola tekstowego
        }
    }

    void HandleChildRemoved(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        // Obs³uga usuniêcia dziecka
        Debug.Log("Child removed: " + args.Snapshot.Key);

        // Czyszczenie pola tekstowego, gdy gracz zostanie usuniêty
        if (currentPlayerIndex > 0)
        {
            currentPlayerIndex--; // Wróæ do poprzedniego pola tekstowego
            playerNameTexts[currentPlayerIndex].text = ""; // Wyczyœæ nazwê gracza
        }
    }
}
