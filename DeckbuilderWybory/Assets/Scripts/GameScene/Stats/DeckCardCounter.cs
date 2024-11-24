using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;

public class DeckCardCounter : MonoBehaviour
{
    public Text cardCountText;
    public Button viewCardsButton;
    public CardSelectionUI cardSelectionUI;
    private DatabaseReference playerDeckRef;

    void Start()
    {
        string playerId = DataTransfer.PlayerId;
        string lobbyId = DataTransfer.LobbyId;

        playerDeckRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("deck");

        playerDeckRef.ValueChanged += OnDeckDataChanged;

        UpdateCardCountInitial();

        if (viewCardsButton != null)
        {
            viewCardsButton.onClick.AddListener(() => OnViewCardsButtonClicked(playerId));
        }
        else
        {
            Debug.LogError("View Cards Button not assigned in DeckCardCounter!");
        }
    }

    private async void UpdateCardCountInitial()
    {
        var snapshot = await playerDeckRef.GetValueAsync();
        UpdateCardCount(snapshot);
    }

    private void OnDeckDataChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError("Error listening to deck changes: " + args.DatabaseError.Message);
            return;
        }

        UpdateCardCount(args.Snapshot);
    }

    private void UpdateCardCount(DataSnapshot snapshot)
    {
        if (!snapshot.Exists)
        {
            Debug.LogWarning("No deck data found.");
            cardCountText.text = "0";
            return;
        }

        int cardCount = 0;

        foreach (var cardSnapshot in snapshot.Children)
        {
            bool onHand = cardSnapshot.Child("onHand").Value as bool? ?? false;
            bool played = cardSnapshot.Child("played").Value as bool? ?? false;

            if (!onHand && !played)
            {
                cardCount++;
            }
        }

        cardCountText.text = cardCount.ToString();
    }

    private async void OnViewCardsButtonClicked(string playerId)
    {
        if (cardSelectionUI != null)
        {
            await cardSelectionUI.ShowDeckCardsForViewing(playerId);
        }
        else
        {
            Debug.LogError("CardSelectionUI reference is missing!");
        }
    }

    void OnDestroy()
    {
        if (playerDeckRef != null)
        {
            playerDeckRef.ValueChanged -= OnDeckDataChanged;
        }

        if (viewCardsButton != null)
        {
            viewCardsButton.onClick.RemoveAllListeners();
        }
    }
}
