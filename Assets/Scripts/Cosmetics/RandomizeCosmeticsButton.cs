using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class RandomizeCosmeticsButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShipShopUI shipShop;
    [SerializeField] private TrailShopUI trailShop;
    [SerializeField] private Button randomizeButton;

    void Reset()
    {
        randomizeButton = GetComponent<Button>();
    }

    void Awake()
    {
        if (randomizeButton)
            randomizeButton.onClick.AddListener(RandomizeBoth);
    }

    /// <summary>
    /// Randomly selects one unlocked ship and one unlocked trail.
    /// </summary>
    public void RandomizeBoth()
    {
        bool shipDone = TryRandomizeShip();
        bool trailDone = TryRandomizeTrail();

        if (!shipDone && !trailDone)
            Debug.Log("[RandomizeCosmetics] No unlocked cosmetics found!");
    }

    private bool TryRandomizeShip()
    {
        if (shipShop == null || shipShop.cards == null) return false;

        var unlockedShips = shipShop.cards
            .Where(c => c && c.data != null && c.data.IsUnlocked())
            .ToList();

        if (unlockedShips.Count <= 1)
            return unlockedShips.Count > 0; // skip reselecting if only one

        var current = PlayerPrefs.GetString("SelectedShip", "");
        ShipCard randomCard;

        // ensure we pick a new one if possible
        do
        {
            randomCard = unlockedShips[Random.Range(0, unlockedShips.Count)];
        } while (randomCard.data != null &&
                 randomCard.data.playerPrefKey == current &&
                 unlockedShips.Count > 1);

        // trigger normal select flow
        randomCard.selectButton.onClick.Invoke();
        return true;
    }

    private bool TryRandomizeTrail()
    {
        if (trailShop == null || trailShop.cards == null) return false;

        var unlockedTrails = trailShop.cards
            .Where(c => c && c.IsUnlocked())
            .ToList();

        if (unlockedTrails.Count <= 1)
            return unlockedTrails.Count > 0; // skip reselecting if only one

        var current = PlayerPrefs.GetString("SelectedTrail", "");
        TrailCard randomCard;

        // ensure we pick a new one if possible
        do
        {
            randomCard = unlockedTrails[Random.Range(0, unlockedTrails.Count)];
        } while (randomCard.trailKey == current && unlockedTrails.Count > 1);

        randomCard.selectButton.onClick.Invoke();
        return true;
    }
}
