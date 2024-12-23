using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ErrorPanelController : MonoBehaviour
{
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private Text errorMessageText;
    [SerializeField] private Button okButton;

    private void Start()
    {
        errorPanel.SetActive(false);
        okButton.onClick.AddListener(HideError);
    }

    public void ShowError(string errorKey)
    {
        string message = ErrorMessages.Messages.ContainsKey(errorKey) ? ErrorMessages.Messages[errorKey] : "Nieznany b³¹d.";

        errorMessageText.text = message;
        errorPanel.SetActive(true);
    }

    private void HideError()
    {
        errorPanel.SetActive(false);
    }
}

public static class ErrorMessages
{
    public static readonly Dictionary<string, string> Messages = new()
    {
        { "region_protected", "Nie mo¿na zagraæ karty, poniewa¿ region jest chroniony." },
        { "player_protected", "Nie mo¿na zagraæ karty, poniewa¿ gracz jest chroniony." },
        { "no_budget", "Nie mo¿na zagraæ karty, poniewa¿ brakuje bud¿etu." },
        { "no_income", "Nie mo¿na zagraæ karty, poniewa¿ brakuje przychodu." },
        { "card_limit", "Nie mo¿na zagraæ wiêcej kart w tej turze." },
        { "general_error", "Wyst¹pi³ b³ad w przetwarzaniu karty." },
        { "action_blocked", "Nie mo¿na zagraæ karty, akcja jest blokowana" },
        { "no_support_available", "Brak dostêpnego miejsca na poparcie w tym regionie." },
        { "no_support","Nie mo¿na zagraæ karty ze wzglêdu na niewystarczaj¹ce poparcie."},
        { "no_player", "Nie znaleziono gracza który spe³nia wymagania karty." },
        { "no_selection", "Nie wybrano ¿adnej karty." },
        { "not_first", "Karta ta mo¿e byæ zagrana tylko jako pierwsza w turze." },
        { "no_cards", "Brak dostêpnych kart." },
        { "cards_lack", "Za ma³o kart dostêpnych aby zagraæ kartê." },
        { "turn_over", "Twoja tura siê skoñczy³a." }
    };
}
