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
        { "insufficient_funds", "Nie mo�na zagra� karty, poniewa� brakuje bud�etu." },
        { "card_limit", "Nie mo�na zagra� wi�cej ni� dwie karty na tur�." },
        { "general_error", "Wyst�pi� nieznany b��d. Spr�buj ponownie." }
    };
}
