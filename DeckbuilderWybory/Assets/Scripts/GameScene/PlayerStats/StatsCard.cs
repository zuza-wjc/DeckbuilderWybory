using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatsCard : MonoBehaviour
{
    public Text playerNameText;
    public Text playerSupportText;
    public Text playerMoneyText;

    public void SetPlayerData(string playerName, string playerSupport, string playerMoney)
    {
        playerNameText.text = playerName;
        playerSupportText.text = playerSupport + "%";
        playerMoneyText.text = playerMoney + "k";
    }
}
