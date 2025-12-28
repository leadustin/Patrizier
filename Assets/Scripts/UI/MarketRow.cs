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

        // Name übersetzen oder Fallback
        if (LocalizationManager.Instance != null)
        {
            nameText.text = LocalizationManager.Instance.GetWareName(wareName);
        }
        else
        {
            nameText.text = wareName;
        }

        leftStockText.text = leftAmt.ToString();
        rightStockText.text = rightAmt.ToString();

        if (isTransfer)
        {
            // Modus: Verschieben (Transfer)
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

            leftToRightBtn.image.color = new Color(1f, 0.8f, 0.8f); // Rot (Kaufen)
            rightToLeftBtn.image.color = new Color(0.8f, 1f, 0.8f); // Grün (Verkaufen)
        }
    }

    // --- BUTTON EVENTS ---

    // AKTION: Von LINKS (Markt) nach RECHTS (Schiff/Kontor)
    public void OnLeftToRightClicked()
    {
        int amount = UIManager.Instance.currentTradeAmount;
        if (amount == int.MaxValue) amount = 100;

        if (isTransferMode)
        {
            // Transfer: Kontor -> Schiff (oder umgekehrt, je nach Logik)
            // Hier Annahme: Markt-Transfer gibt es nicht, also ist das Kontor <-> Schiff
            City city = UIManager.Instance.currentCity;
            if (PlayerManager.Instance.TransferToShip(city, myWareName, amount))
                UIManager.Instance.RefreshMarketList();
        }
        else
        {
            // Handel: Kaufen
            int price = int.Parse(leftToRightBtn.GetComponentInChildren<TextMeshProUGUI>().text);
            var mode = UIManager.Instance.currentMarketMode;

            if (mode == UIManager.MarketMode.CityToShip)
            {
                // Stadt -> Schiff (Kauf)
                City city = UIManager.Instance.currentCity;
                if (PlayerManager.Instance.TryBuyFromCity(city, myWareName, amount, price))
                {
                    UIManager.Instance.UpdateGoldDisplay();
                    UIManager.Instance.RefreshMarketList();
                }
            }
            else if (mode == UIManager.MarketMode.CityToKontor)
            {
                // Stadt -> Kontor (Kauf)
                City city = UIManager.Instance.currentCity;
                int totalCost = amount * price;

                // Manuelle Prüfung, da PlayerManager hierfür keine Methode hat
                if (PlayerManager.Instance.currentGold >= totalCost)
                {
                    if (city.GetMarketStock(myWareName) >= amount)
                    {
                        PlayerManager.Instance.currentGold -= totalCost;
                        city.RemoveMarketStock(myWareName, amount);
                        city.AddToKontor(myWareName, amount); // Ins Kontor legen

                        UIManager.Instance.UpdateGoldDisplay();
                        UIManager.Instance.RefreshMarketList();
                    }
                }
            }
        }
    }

    // AKTION: Von RECHTS (Schiff/Kontor) nach LINKS (Markt)
    public void OnRightToLeftClicked()
    {
        int amount = UIManager.Instance.currentTradeAmount;
        // Verfügbarkeit prüfen (steht im rechten Textfeld)
        int available = int.Parse(rightStockText.text);

        if (amount == int.MaxValue || amount > available) amount = available;
        if (amount <= 0) return;

        if (isTransferMode)
        {
            // Transfer: Schiff -> Kontor
            City city = UIManager.Instance.currentCity;
            if (PlayerManager.Instance.TransferToKontor(city, myWareName, amount))
                UIManager.Instance.RefreshMarketList();
        }
        else
        {
            // Handel: Verkaufen
            int price = int.Parse(rightToLeftBtn.GetComponentInChildren<TextMeshProUGUI>().text);
            var mode = UIManager.Instance.currentMarketMode;

            if (mode == UIManager.MarketMode.CityToShip)
            {
                // Schiff -> Stadt (Verkauf)
                City city = UIManager.Instance.currentCity;
                if (PlayerManager.Instance.TrySellToCity(city, myWareName, amount, price))
                {
                    UIManager.Instance.UpdateGoldDisplay();
                    UIManager.Instance.RefreshMarketList();
                }
            }
            else if (mode == UIManager.MarketMode.CityToKontor)
            {
                // HIER FEHLTE DIE LOGIK: Kontor -> Stadt (Verkauf)
                City city = UIManager.Instance.currentCity;

                // Wir haben schon geprüft, ob 'amount <= available' (via rightStockText), 
                // also ist genug im Kontor.

                city.AddToKontor(myWareName, -amount); // Aus Kontor entfernen

                // Zurück in den Markt (negative Remove = Add, oder spezifische Add Methode nutzen)
                city.RemoveMarketStock(myWareName, -amount);

                int revenue = amount * price;
                PlayerManager.Instance.currentGold += revenue;

                UIManager.Instance.UpdateGoldDisplay();
                UIManager.Instance.RefreshMarketList();
            }
        }
    }
}