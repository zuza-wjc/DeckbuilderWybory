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
        string message = ErrorMessages.Messages.ContainsKey(errorKey) ? ErrorMessages.Messages[errorKey] : "Nieznany b��d.";

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
        { "region_protected", "Nie mo�na zagra� karty, poniewa� region jest chroniony." },
        { "player_protected", "Nie mo�na zagra� karty, poniewa� gracz jest chroniony." },
        { "no_budget", "Nie mo�na zagra� karty, poniewa� brakuje bud�etu." },
        { "no_income", "Nie mo�na zagra� karty, poniewa� brakuje przychodu." },
        { "card_limit", "Nie mo�na zagra� wi�cej kart w tej turze." },
        { "general_error", "Wyst�pi� b�ad w przetwarzaniu karty." },
        { "action_blocked", "Nie mo�na zagra� karty, akcja jest blokowana" },
        { "no_support_available", "Brak dost�pnego miejsca na poparcie w tym regionie." },
        { "no_support","Nie mo�na zagra� karty ze wzgl�du na niewystarczaj�ce poparcie."},
        { "no_player", "Nie znaleziono gracza kt�ry spe�nia wymagania karty." },
        { "no_selection", "Nie wybrano �adnej karty." },
        { "not_first", "Karta ta mo�e by� zagrana tylko jako pierwsza w turze." },
        { "no_cards", "Brak dost�pnych kart." },
        { "cards_lack", "Za ma�o kart dost�pnych aby zagra� kart�." },
        { "turn_over", "Twoja tura si� sko�czy�a." }
    };
}
