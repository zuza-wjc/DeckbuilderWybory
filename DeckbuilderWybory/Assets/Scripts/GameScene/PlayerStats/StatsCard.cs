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

    public void SetPlayerData(string playerName, string playerSupport, string playerMoney, string playerIncome, int playerCardNumber, string regionSupport)
    {
        playerNameText.text = playerName;
        playerSupportText.text = playerSupport + "%";
        playerMoneyText.text = playerMoney + "k";
        playerIncomeText.text = "+" + playerIncome + "k";
        playerCardNumberText.text = playerCardNumber.ToString();
        playerRegionSupportText.text = regionSupport;
    }
}
