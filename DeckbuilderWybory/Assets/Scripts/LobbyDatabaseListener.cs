using Firebase.Database;
using TMPro;
using UnityEngine;

public class LobbyDatabaseListener : MonoBehaviour
{
    DatabaseReference sessionRef;

    public TextMeshProUGUI[] playerNameTexts; // Tablica p�l tekstowych do wy�wietlania nazw graczy
    private int currentPlayerIndex = 0; // Indeks aktualnie dost�pnego pola tekstowego

    void Start()
    {
        // Inicjalizacja referencji do bazy danych Firebase
        sessionRef = FirebaseDatabase.DefaultInstance.RootReference.Child("sessions/lobby_1");
        StartListening();
    }

    void StartListening()
    {
        // Nas�uchiwanie zmian w danej lokalizacji
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

        // Obs�uga dodania nowego dziecka
        Debug.Log("New child added: " + args.Snapshot.Key);

        // Sprawdzamy, czy nowy gracz ma ustawion� nazw� i czy istnieje dost�pne pole tekstowe
        if (args.Snapshot.Child("name").Exists && currentPlayerIndex < playerNameTexts.Length)
        {
            string playerName = args.Snapshot.Child("name").GetValue(true).ToString();
            playerNameTexts[currentPlayerIndex].text = playerName; // Aktualizujemy nazw� gracza na scenie
            currentPlayerIndex++; // Przechodzimy do nast�pnego pola tekstowego
        }
    }

    void HandleChildRemoved(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        // Obs�uga usuni�cia dziecka
        Debug.Log("Child removed: " + args.Snapshot.Key);

        // Czyszczenie pola tekstowego, gdy gracz zostanie usuni�ty
        if (currentPlayerIndex > 0)
        {
            currentPlayerIndex--; // Wr�� do poprzedniego pola tekstowego
            playerNameTexts[currentPlayerIndex].text = ""; // Wyczy�� nazw� gracza
        }
    }
}
