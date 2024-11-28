using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Database;
using System;
using Firebase.Extensions;
using System.Linq;
using UnityEngine.SceneManagement;

public class DeckCardCounter : MonoBehaviour
{
    public Text cardCountText;
    public Button viewCardsButton;
    public CardSelectionUI cardSelectionUI;
    private DatabaseReference playerDeckRef;
    private string playerId;

    void Start()
    {
        playerId = DataTransfer.PlayerId;
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
        List<DataSnapshot> availableCards = new List<DataSnapshot>();

        foreach (var cardSnapshot in snapshot.Children)
        {
            bool onHand = cardSnapshot.Child("onHand").Value as bool? ?? false;
            bool played = cardSnapshot.Child("played").Value as bool? ?? false;

            if (!onHand && !played)
            {
                cardCount++;
                availableCards.Add(cardSnapshot);
            }
        }

        cardCountText.text = cardCount.ToString();

        // Jeœli gracz ma mniej ni¿ 4 karty, dobierz nowe
        if (cardCount < 4)
        {
            DrawNewCardsFromDeck(availableCards);
        }
    }

    private void DrawNewCardsFromDeck(List<DataSnapshot> availableCards)
    {
        if (availableCards.Count > 0)
        {
            // Losowanie jednej z dostêpnych kart
            int randomIndex = UnityEngine.Random.Range(0, availableCards.Count);
            DataSnapshot selectedCard = availableCards[randomIndex];

            // Aktualizacja w³aœciwoœci karty: ustawienie `onHand` na true
            string cardId = selectedCard.Key;
            playerDeckRef.Child(cardId).Child("onHand").SetValueAsync(true).ContinueWithOnMainThread(updateTask =>
            {
                if (updateTask.IsCompleted)
                {
                    Debug.Log($"Dodano kartê {cardId} na rêkê gracza.");
                }
                else
                {
                    Debug.LogError($"B³¹d podczas dodawania karty {cardId} na rêkê: {updateTask.Exception}");
                }
            });
        }
        else
        {
            Debug.LogWarning("Brak dostêpnych kart do dobrania.");
        }
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
