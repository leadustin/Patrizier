using UnityEngine;
using System.Collections.Generic;

public static class EconomySystem
{
    // Die Ur-Wahrheit: Was ist eine Ware wert?
    private static Dictionary<string, int> basePrices = new Dictionary<string, int>()
    {
        { "Holz", 40 }, { "Ziegel", 40 }, { "Getreide", 60 }, { "Fisch", 80 },
        { "Bier", 90 }, { "Tuch", 200 }, { "Eisen", 350 }, { "Salz", 50 },
        { "Wein", 250 }, { "Wolle", 100 }, { "Felle", 150 }, { "Honig", 120 }
    };

    public static int GetBasePrice(string ware)
    {
        return basePrices.ContainsKey(ware) ? basePrices[ware] : 10;
    }

    // --- PREIS BERECHNUNG ---
    public static int CalculatePrice(string ware, int currentStock, City city)
    {
        int basePrice = GetBasePrice(ware);
        float price = basePrice;

        // 1. Angebot & Nachfrage
        int targetStock = Mathf.RoundToInt(city.population * 0.1f);
        if (targetStock < 50) targetStock = 50;

        float scarcity = (float)targetStock / (float)(currentStock + 10);
        price = price * scarcity;

        // 2. Lokale Produktion = Billiger
        // HIER WAR DER FEHLER: Wir nutzen jetzt die neue Methode statt der alten Liste
        if (city.DoesProduce(ware))
        {
            price *= 0.8f; // 20% Rabatt wenn vor Ort hergestellt
        }

        // 3. Ereignisse
        if (city.activeEvents.isUnderSiege) price *= 2.0f;

        // Grenzen setzen
        return Mathf.Clamp(Mathf.RoundToInt(price), basePrice / 10, basePrice * 5);
    }

    // --- TÄGLICHER VERBRAUCH ---
    public static int CalculateDailyConsumption(string ware, int population)
    {
        float perCapita = 0;

        switch (ware)
        {
            case "Getreide": perCapita = 0.05f; break;
            case "Fisch": perCapita = 0.03f; break;
            case "Bier": perCapita = 0.04f; break;
            case "Holz": perCapita = 0.02f; break;
            case "Salz": perCapita = 0.01f; break;
            default: perCapita = 0.005f; break;
        }

        return Mathf.CeilToInt(population * perCapita);
    }
}