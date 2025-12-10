using UnityEngine;
using UnityEditor;

// Dieses Attribut verbindet das Skript mit deiner SeaLane
[CustomEditor(typeof(SeaLane))]
public class SeaLaneEditor : Editor
{
    // Diese Funktion malt in das Scene-Fenster
    protected virtual void OnSceneGUI()
    {
        // Wir holen uns die Lane, die du gerade angeklickt hast
        SeaLane lane = (SeaLane)target;

        if (lane.shapePoints == null) return;

        // Wir gehen alle Punkte durch (P1, P2, P3...)
        for (int i = 0; i < lane.shapePoints.Count; i++)
        {
            Transform point = lane.shapePoints[i];

            if (point != null)
            {
                // Prüfen, ob wir etwas geändert haben
                EditorGUI.BeginChangeCheck();

                // Zeichnet den Move-Pfeil (Handle) direkt an den Punkt
                // Du kannst ihn jetzt greifen, ohne das Objekt in der Hierarchy suchen zu müssen!
                Vector3 newPosition = Handles.PositionHandle(point.position, Quaternion.identity);

                // Wenn du den Pfeil bewegt hast...
                if (EditorGUI.EndChangeCheck())
                {
                    // ...speichern wir das für "Strg+Z" (Undo)
                    Undo.RecordObject(point, "Move Waypoint");

                    // ...und setzen die neue Position
                    point.position = newPosition;

                    // (Optional) Sag der Lane, dass sie sich neu zeichnen soll, falls nötig
                    EditorUtility.SetDirty(lane);
                }

                // Zeigt den Namen (P1, P2) über dem Punkt an, damit du weißt, wer wer ist
                Handles.Label(point.position + Vector3.up * 0.5f, point.name);
            }
        }
    }
}