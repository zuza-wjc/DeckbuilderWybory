using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public class StatsCard : MonoBehaviour
{
    public Text playerNameText;
    public Text playerSupportText;
    public Text playerMoneyText;
    public Text playerIncomeText;

    public void SetPlayerData(string playerName, string playerSupport, string playerMoney, string playerIncome)
    {
        playerNameText.text = playerName;
        playerSupportText.text = playerSupport + "%";
        playerMoneyText.text = playerMoney + "k";
        playerIncomeText.text = "+" + playerIncome + "k";
    }
}
