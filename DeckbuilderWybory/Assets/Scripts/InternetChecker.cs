using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class InternetChecker : MonoBehaviour
{
    private static InternetChecker instance;

    private bool isCheckingConnection = true;
    public bool IsConnected { get; private set; } = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(CheckConnection());
    }

    private IEnumerator CheckConnection()
    {
        while (true)
        {
            if (isCheckingConnection)
            {
                yield return StartCoroutine(CheckGoogleConnection());
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator CheckGoogleConnection()
    {
        UnityWebRequest request = UnityWebRequest.Get("https://www.google.com");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Brak po³¹czenia z internetem.");
            isCheckingConnection = false;
            IsConnected = false;

            if (SceneManager.GetActiveScene().name != "No Internet")
            {
                SceneManager.LoadScene("No Internet", LoadSceneMode.Single);
                yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "No Internet");
            }
                
        }
        else
        {
            IsConnected = true;
        }
    }

    public IEnumerator RetryConnection()
    {
        yield return CheckGoogleConnection();
    }

    public void ResumeChecking()
    {
        isCheckingConnection = true;
    }
}