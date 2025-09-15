// CurrencyBank.cs
using UnityEngine;

public static class CurrencyBank
{
    private const string CurrencyKey = "currency";

    public static int Get() => PlayerPrefs.GetInt(CurrencyKey, 0);

    public static void Set(int amount)
    {
        PlayerPrefs.SetInt(CurrencyKey, Mathf.Max(0, amount));
        PlayerPrefs.Save();
    }

    public static bool CanAfford(int cost) => Get() >= cost;

    public static bool TrySpend(int cost)
    {
        if (!CanAfford(cost)) return false;
        Set(Get() - Mathf.Max(0, cost));
        return true;
    }

    public static void Add(int amount) => Set(Get() + Mathf.Max(0, amount));
}
