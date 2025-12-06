using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum GameLanguage
{
    German,
    English
    // Hier später French, Spanish ergänzen...
}

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance;

    public GameLanguage currentLanguage = GameLanguage.German;

    // Event: Alle UI-Texte hören hier zu. Wenn Sprache wechselt -> Text aktualisieren!
    public event Action OnLanguageChanged;

    // Das große Wörterbuch: Key -> (Sprache -> Text)
    private Dictionary<string, Dictionary<GameLanguage, string>> dictionary = new Dictionary<string, Dictionary<GameLanguage, string>>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        LoadTranslations();
    }

    // --- HIER TRAGEN WIR ALLE TEXTE EIN ---
    // In einem echten Projekt würde man das aus einer CSV/JSON Datei laden.
    // Für Vibe Coding schreiben wir es direkt hier rein -> Schnell & Einfach.
    void LoadTranslations()
    {
        // UI Allgemein
        Add("UI_CLOSE", "Schließen", "Close");
        Add("UI_BACK", "Zurück", "Back");
        Add("UI_ENTER_CITY", "Stadt betreten", "Enter City");
        Add("UI_GOLD", "Gold", "Gold");

        // Markt Modi
        Add("MODE_SHIP", "Handel: Schiff", "Trade: Ship");
        Add("MODE_KONTOR", "Handel: Kontor", "Trade: Warehouse");
        Add("MODE_TRANSFER", "Verschieben", "Transfer");

        // Waren (Keys basieren auf den Enum-Namen in Großbuchstaben)
        Add("WARE_HOLZ", "Holz", "Wood");
        Add("WARE_ZIEGEL", "Ziegel", "Bricks");
        Add("WARE_GETREIDE", "Getreide", "Grain");
        Add("WARE_FISCH", "Fisch", "Fish");
        Add("WARE_BIER", "Bier", "Beer");
        Add("WARE_TUCH", "Tuch", "Cloth");
        Add("WARE_EISEN", "Eisen", "Iron");
        Add("WARE_SALZ", "Salz", "Salt");
        Add("WARE_WEIN", "Wein", "Wine");
        Add("WARE_WOLLE", "Wolle", "Wool");
        Add("WARE_FELLE", "Felle", "Furs");
        Add("WARE_HONIG", "Honig", "Honey");

        // Jahreszeiten
        Add("SEASON_SPRING", "Frühling", "Spring");
        Add("SEASON_SUMMER", "Sommer", "Summer");
        Add("SEASON_AUTUMN", "Herbst", "Autumn");
        Add("SEASON_WINTER", "Winter", "Winter");

        // --- MONATE (Neu) ---
        Add("MONTH_1", "Januar", "January");
        Add("MONTH_2", "Februar", "February");
        Add("MONTH_3", "März", "March");
        Add("MONTH_4", "April", "April");
        Add("MONTH_5", "Mai", "May");
        Add("MONTH_6", "Juni", "June");
        Add("MONTH_7", "Juli", "July");
        Add("MONTH_8", "August", "August");
        Add("MONTH_9", "September", "September");
        Add("MONTH_10", "Oktober", "October");
        Add("MONTH_11", "November", "November");
        Add("MONTH_12", "Dezember", "December");
    }

    // Hilfsfunktion für Monate
    public string GetMonthName(int monthIndex)
    {
        return Get("MONTH_" + monthIndex);
    }

    // Hilfsfunktion zum Füllen
    void Add(string key, string de, string en)
    {
        var entry = new Dictionary<GameLanguage, string>();
        entry.Add(GameLanguage.German, de);
        entry.Add(GameLanguage.English, en);
        dictionary.Add(key, entry);
    }

    // --- TEXT ABFRAGEN ---
    public string Get(string key)
    {
        if (!dictionary.ContainsKey(key)) return "MISSING:" + key;
        if (!dictionary[key].ContainsKey(currentLanguage)) return dictionary[key][GameLanguage.German]; // Fallback
        return dictionary[key][currentLanguage];
    }

    // Spezielle Helfer für Enums (Waren/Jahreszeiten)
    public string GetWareName(string wareEnumName)
    {
        return Get("WARE_" + wareEnumName.ToUpper());
    }

    public string GetSeasonName(Season season)
    {
        switch (season)
        {
            case Season.Frühling: return Get("SEASON_SPRING");
            case Season.Sommer: return Get("SEASON_SUMMER");
            case Season.Herbst: return Get("SEASON_AUTUMN");
            case Season.Winter: return Get("SEASON_WINTER");
            default: return season.ToString();
        }
    }

    // --- SPRACHE WECHSELN ---
    public void SetLanguage(GameLanguage lang)
    {
        currentLanguage = lang;
        Debug.Log("Sprache gewechselt zu: " + lang);
        OnLanguageChanged?.Invoke(); // Alle UI Elemente updaten!
    }
    public void SetGerman()
    {
        SetLanguage(GameLanguage.German);
    }

    public void SetEnglish()
    {
        SetLanguage(GameLanguage.English);
    }
}