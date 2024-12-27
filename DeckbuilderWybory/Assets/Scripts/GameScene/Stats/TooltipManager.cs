using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    [SerializeField] private Text tooltipTextUI;
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private float tooltipDuration = 5f;

    private Coroutine hideCoroutine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void ShowTooltip(string text)
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        tooltipTextUI.text = text;
        tooltipPanel.SetActive(true);

        hideCoroutine = StartCoroutine(HideTooltipAfterDelay());
    }

    public void HideTooltip()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        tooltipPanel.SetActive(false);
    }

    private IEnumerator HideTooltipAfterDelay()
    {
        yield return new WaitForSeconds(tooltipDuration);
        HideTooltip();
    }
}
