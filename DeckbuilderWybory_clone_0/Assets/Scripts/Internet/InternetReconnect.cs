using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class InternetReconnect : MonoBehaviour
{
    public Button reconnectButton;
    public Text messageText;
    private InternetChecker internetChecker;

    void Start()
    {
        if(reconnectButton != null)
        {
            reconnectButton.onClick.AddListener(RetryConnection);
        }
        else
        {
            Debug.LogError("Reconnect button not assigned in the inspector.");
        }

        internetChecker = FindObjectOfType<InternetChecker>();
        if(internetChecker == null)
        {
            Debug.LogError("InternetChecker instance not found.");
        }
    }

    public void RetryConnection()
    {
        if(internetChecker != null)
        {
            StartCoroutine(CheckAndHandleConnection());
        }
        else
        {
            Debug.LogError("InternetChecker instance not found.");
        }
    }

    private IEnumerator CheckAndHandleConnection()
    {
        // Sprawdzamy po³¹czenie
        yield return internetChecker.RetryConnection();

        if(internetChecker.IsConnected)
        {
            SceneManager.LoadScene("Main Menu", LoadSceneMode.Single);
            internetChecker.ResumeChecking();
        }
        else
        {
            if (messageText != null)
            {
                messageText.gameObject.SetActive(true);
                yield return new WaitForSeconds(3);
                messageText.gameObject.SetActive(false);
            }
        }
    }

    void OnDestroy()
    {
        if (reconnectButton != null)
        {
            reconnectButton.onClick.RemoveListener(RetryConnection);
        }
    }
}
