using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public class StatsCard : MonoBehaviour
{
    public Text playerNameText;
    public Text playerSupportText;
    public Text playerMoneyText;
    public Text playerIncomeText;
    public Text playerCardNumberText;
    public Text playerRegionSupportText;
    public Text playerTurnNumberText;
    public Text deckTypeText;

    public Image deckTypeImage;

    public Sprite ambasadaSprite;
    public Sprite metropoliaSprite;
    public Sprite srodowiskoSprite;
    public Sprite przemyslSprite;
    public Sprite podstawaSprite;

    public void SetPlayerData(string playerName, string playerSupport, string playerMoney, string playerIncome, int playerCardNumber, string regionSupport, int turnNumber, string deckType)
    {
        playerNameText.text = playerName;
        playerSupportText.text = playerSupport + "%";
        playerMoneyText.text = playerMoney + "k";
        playerIncomeText.text = "+" + playerIncome + "k";
        playerCardNumberText.text = playerCardNumber.ToString();
        playerRegionSupportText.text = regionSupport;
        playerTurnNumberText.text = turnNumber.ToString();
        deckTypeText.text = deckType;

        SetDeckTypeImage(deckType);
    }

    private void SetDeckTypeImage(string deckType)
    {
        if (deckTypeImage == null)
        {
            Debug.LogError("DeckTypeImage is not assigned!");
            return;
        }

        switch (deckType.ToLower()) // Przekszta³camy na ma³e litery dla bezb³êdnego porównania
        {
            case "ambasada":
                deckTypeImage.sprite = ambasadaSprite;
                break;
            case "metropolia":
                deckTypeImage.sprite = metropoliaSprite;
                break;
            case "œrodowisko":
                deckTypeImage.sprite = srodowiskoSprite;
                break;
            case "przemys³":
                deckTypeImage.sprite = przemyslSprite;
                break;
            default:
                Debug.LogWarning($"Unknown deckType: {deckType}. Default sprite will be used.");
                deckTypeImage.sprite = podstawaSprite;
                break;
        }
    }
}
