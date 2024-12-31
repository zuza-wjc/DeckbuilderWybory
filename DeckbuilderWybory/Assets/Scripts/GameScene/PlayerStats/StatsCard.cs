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

    public int PlayerSupportValue { get; private set; }
    public int PlayerMoneyValue { get; private set; }
    public int RegionsNumberValue { get; private set; }

    public void SetPlayerData(string playerName, string playerSupport, string playerMoney, string playerIncome, int playerCardNumber, string regionSupport, int turnNumber, string deckType, int regionsNumber)
    {
        playerNameText.text = playerName;
        playerSupportText.text = playerSupport + "%";
        playerMoneyText.text = playerMoney + "k";
        playerIncomeText.text = "+" + playerIncome + "k";
        playerCardNumberText.text = playerCardNumber.ToString();
        playerRegionSupportText.text = regionSupport;
        playerTurnNumberText.text = turnNumber.ToString();
        deckTypeText.text = deckType;

        PlayerSupportValue = int.TryParse(playerSupport, NumberStyles.Integer, CultureInfo.InvariantCulture, out int supportValue) ? supportValue : 0;
        PlayerMoneyValue = int.TryParse(playerMoney, NumberStyles.Integer, CultureInfo.InvariantCulture, out int moneyValue) ? moneyValue : 0;
        RegionsNumberValue = regionsNumber;

        SetDeckTypeImage(deckType);
    }

    private void SetDeckTypeImage(string deckType)
    {
        if (deckTypeImage == null)
        {
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
                deckTypeImage.sprite = podstawaSprite;
                break;
        }
    }
}
