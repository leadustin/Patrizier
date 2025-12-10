using UnityEngine;
using System.Collections.Generic;

public class SeaLane : MonoBehaviour
{
    [Header("Verbindung")]
    public SeaNode startNode;
    public SeaNode endNode;

    [Header("Wegpunkte (Deine 'Pins')")]
    public List<Transform> shapePoints = new List<Transform>();

    [Header("Qualität")]
    [Range(5, 50)] public int resolution = 20; // Wie viele Teilstücke pro Kurvenabschnitt? (Höher = runder)

    // Button im Inspector-Kontextmenü (Rechtsklick auf Titel -> Form-Punkte suchen)
    [ContextMenu("Form-Punkte suchen")]
    public void FindShapePoints()
    {
        shapePoints.Clear();
        foreach (Transform child in transform) shapePoints.Add(child);
    }

    // --- SPLINE BERECHNUNG (Catmull-Rom) ---
    // Diese Funktion macht aus eckigen Punkten eine runde Kurve
    public List<Vector3> GetSmoothPathPoints(bool reverse)
    {
        // 1. Liste aller Kontrollpunkte erstellen (Start -> P1 -> P2... -> Ende)
        List<Vector3> controlPoints = new List<Vector3>();

        if (!reverse)
        {
            if (startNode) controlPoints.Add(startNode.transform.position);
            foreach (Transform t in shapePoints) if (t) controlPoints.Add(t.position);
            if (endNode) controlPoints.Add(endNode.transform.position);
        }
        else
        {
            if (endNode) controlPoints.Add(endNode.transform.position);
            for (int i = shapePoints.Count - 1; i >= 0; i--)
            {
                if (shapePoints[i]) controlPoints.Add(shapePoints[i].position);
            }
            if (startNode) controlPoints.Add(startNode.transform.position);
        }

        // Sicherheitscheck: Wenn wir nur Start und Ende haben, ist es eine gerade Linie
        if (controlPoints.Count < 2) return controlPoints;

        // 2. Die Kurve berechnen
        List<Vector3> smoothPoints = new List<Vector3>();

        // Um von P[i] nach P[i+1] zu rechnen, braucht Catmull-Rom 4 Punkte: i-1, i, i+1, i+2
        for (int i = 0; i < controlPoints.Count - 1; i++)
        {
            Vector3 p0 = (i == 0) ? controlPoints[0] : controlPoints[i - 1];
            Vector3 p1 = controlPoints[i];
            Vector3 p2 = controlPoints[i + 1];
            Vector3 p3 = (i + 2 < controlPoints.Count) ? controlPoints[i + 2] : p2;

            // Wir unterteilen die Strecke in kleine Schritte
            for (int t = 0; t < resolution; t++)
            {
                // Den allerletzten Punkt nicht doppelt hinzufügen, außer im allerletzten Durchgang
                if (i > 0 && t == 0) continue;

                float progress = (float)t / resolution;
                smoothPoints.Add(CalculateCatmullRom(progress, p0, p1, p2, p3));
            }
        }

        // Den allerletzten Punkt noch exakt hinzufügen
        smoothPoints.Add(controlPoints[controlPoints.Count - 1]);

        return smoothPoints;
    }

    // Die Formel für den Spline
    Vector3 CalculateCatmullRom(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Vector3 a = 2f * p1;
        Vector3 b = p2 - p0;
        Vector3 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
        Vector3 d = -p0 + 3f * p1 - 3f * p2 + p3;

        return 0.5f * (a + (b * t) + (c * t * t) + (d * t * t * t));
    }

    // Zeichnet die Kurve im Editor GRÜN
    void OnDrawGizmos()
    {
        if (startNode == null || endNode == null) return;

        // Zeichne die "echten" Punkte als gelbe Würfel (damit du sie greifen kannst)
        Gizmos.color = Color.yellow;
        foreach (Transform t in shapePoints)
        {
            if (t != null) Gizmos.DrawCube(t.position, Vector3.one * 0.2f);
        }

        // Zeichne die berechnete Kurve (Vorschau)
        List<Vector3> curve = GetSmoothPathPoints(false);
        Gizmos.color = Color.green;

        for (int i = 0; i < curve.Count - 1; i++)
        {
            Gizmos.DrawLine(curve[i], curve[i + 1]);
        }
    }
}