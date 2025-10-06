using UnityEngine;
using TMPro;       // remove if you use UnityEngine.UI.Text instead

public class CurrencyDisplayManager : MonoBehaviour
{
    [Header("Currency Texts (any you want synced)")]
    [SerializeField] private TMP_Text[] currencyTexts;

    // optional: call this manually if you ever dynamically create new UI elements
    public void RegisterText(TMP_Text text)
    {
        if (text == null) return;
        var list = new System.Collections.Generic.List<TMP_Text>(currencyTexts);
        if (!list.Contains(text))
        {
            list.Add(text);
            currencyTexts = list.ToArray();
            UpdateTexts();
        }
    }

    void OnEnable()
    {
        UpdateTexts();
        CurrencyBank.OnCurrencyChanged += UpdateTexts;
    }

    void OnDisable()
    {
        CurrencyBank.OnCurrencyChanged -= UpdateTexts;
    }

    private void UpdateTexts()
    {
        int amount = CurrencyBank.Get();
        foreach (var t in currencyTexts)
            if (t) t.text = amount.ToString("N0");   // e.g., "1,234"
    }
}
