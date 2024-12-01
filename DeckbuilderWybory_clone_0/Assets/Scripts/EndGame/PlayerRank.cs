using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public class PlayerRank : MonoBehaviour
{
    public Text rankText;
    public Text playerNameText;
    public Text playerSupportText;
    public Text regionsText;
    public Text playerMoneyText;

    public void SetPlayerData(int rank, string playerName, string playerSupport, int regions, string playerMoney)
    {
        rankText.text = rank + ".";
        playerNameText.text = playerName;
        playerSupportText.text = playerSupport + "%";
        regionsText.text = regions + "";
        playerMoneyText.text = playerMoney + "k";
    }
}