using UnityEngine;
using UnityEngine.UI;

public class StatsCard : MonoBehaviour
{
    public Text playerNameText;
    public Text playerSupportText;
    public Text playerMoneyText;
    public Text playerIncomeText;
    public Text playerDeckCardNumber;

    public void SetPlayerData(string playerName, string playerSupport, string playerMoney, string playerIncome, int playerCardNumber)
    {
        playerNameText.text = playerName;
        playerSupportText.text = playerSupport + "%";
        playerMoneyText.text = playerMoney + "k";
        playerIncomeText.text = "+" + playerIncome + "k";
        playerDeckCardNumber.text = playerCardNumber.ToString();
    }
}
