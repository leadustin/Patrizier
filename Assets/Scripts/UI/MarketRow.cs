using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MarketRow : MonoBehaviour
{
    [Header("UI Elemente")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI leftStockText;
    public TextMeshProUGUI rightStockText;

    [Header("Buttons")]
    public Button leftToRightBtn;
    public Button rightToLeftBtn;

    private string myWareName;
    private bool isTransferMode;

    public void SetupRow(string wareName, int leftAmt, int rightAmt, int basePrice, bool isTransfer)
    {
        myWareName = wareName;
        isTransferMode = isTransfer;

        // --- ÄNDERUNG: Name übersetzen ---
        if (LocalizationManager.Instance != null)
        {
            nameText.text = LocalizationManager.Instance.GetWareName(wareName);
        }
        else
        {
            nameText.text = wareName; // Fallback
        }
        // --------------------------------

        leftStockText.text = leftAmt.ToString();
        rightStockText.text = rightAmt.ToString();

        if (isTransfer)
        {
            // Modus: Verschieben
            leftToRightBtn.GetComponentInChildren<TextMeshProUGUI>().text = ">>";
            rightToLeftBtn.GetComponentInChildren<TextMeshProUGUI>().text = "<<";
            leftToRightBtn.image.color = Color.white;
            rightToLeftBtn.image.color = Color.white;
        }
        else
        {
            // Modus: Handel (Preise)
            int buyPrice = basePrice + 5;
            int sellPrice = Mathf.Max(1, basePrice - 5);

            leftToRightBtn.GetComponentInChildren<TextMeshProUGUI>().text = buyPrice.ToString();
            rightToLeftBtn.GetComponentInChildren<TextMeshProUGUI>().text = sellPrice.ToString();

            leftToRightBtn.image.color = new Color(1f, 0.8f, 0.8f); // Rot
            rightToLeftBtn.image.color = new Color(0.8f, 1f, 0.8f); // Grün
        }
    }

    // --- BUTTON EVENTS (Bleiben gleich) ---
    public void OnLeftToRightClicked()
    {
        int amount = UIManager.Instance.currentTradeAmount;
        if (amount == int.MaxValue) amount = 100;

        if (isTransferMode)
        {
            City city = UIManager.Instance.currentCity;
            if (PlayerManager.Instance.TransferToShip(city, myWareName, amount))
                UIManager.Instance.RefreshMarketList();
        }
        else
        {
            int price = int.Parse(leftToRightBtn.GetComponentInChildren<TextMeshProUGUI>().text);
            var mode = UIManager.Instance.currentMarketMode;

            if (mode == UIManager.MarketMode.CityToShip)
            {
                City city = UIManager.Instance.currentCity;
                if (PlayerManager.Instance.TryBuyFromCity(city, myWareName, amount, price))
                {
                    UIManager.Instance.UpdateGoldDisplay();
                    UIManager.Instance.RefreshMarketList();
                }
            }
            else if (mode == UIManager.MarketMode.CityToKontor)
            {
                City city = UIManager.Instance.currentCity;
                if (PlayerManager.Instance.currentGold >= amount * price)
                {
                    if (city.GetMarketStock(myWareName) >= amount)
                    {
                        PlayerManager.Instance.currentGold -= amount * price;
                        city.RemoveMarketStock(myWareName, amount);
                        city.AddToKontor(myWareName, amount);
                        UIManager.Instance.UpdateGoldDisplay();
                        UIManager.Instance.RefreshMarketList();
                    }
                }
            }
        }
    }

    public void OnRightToLeftClicked()
    {
        int amount = UIManager.Instance.currentTradeAmount;
        int available = int.Parse(rightStockText.text);
        if (amount == int.MaxValue || amount > available) amount = available;
        if (amount <= 0) return;

        if (isTransferMode)
        {
            City city = UIManager.Instance.currentCity;
            if (PlayerManager.Instance.TransferToKontor(city, myWareName, amount))
                UIManager.Instance.RefreshMarketList();
        }
        else
        {
            int price = int.Parse(rightToLeftBtn.GetComponentInChildren<TextMeshProUGUI>().text);
            var mode = UIManager.Instance.currentMarketMode;

            if (mode == UIManager.MarketMode.CityToShip)
            {
                City city = UIManager.Instance.currentCity;
                if (PlayerManager.Instance.TrySellToCity(city, myWareName, amount, price))
                {
                    UIManager.Instance.UpdateGoldDisplay();
                    UIManager.Instance.RefreshMarketList();
                }
            }
            // Kontor -> Stadt Verkauf (analog)
        }
    }
}