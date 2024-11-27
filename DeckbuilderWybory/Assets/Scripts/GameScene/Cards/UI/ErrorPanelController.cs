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
        { "insufficient_funds", "Nie mo¿na zagraæ karty, poniewa¿ brakuje bud¿etu." },
        { "card_limit", "Nie mo¿na zagraæ wiêcej ni¿ dwie karty na turê." },
        { "general_error", "Wyst¹pi³ nieznany b³¹d. Spróbuj ponownie." }
    };
}
