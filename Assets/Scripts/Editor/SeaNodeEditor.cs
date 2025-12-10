using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SeaNode))]
[CanEditMultipleObjects] // Erlaubt dir, mehrere Nodes gleichzeitig zu verschieben!
public class SeaNodeEditor : Editor
{
    protected virtual void OnSceneGUI()
    {
        SeaNode node = (SeaNode)target;

        EditorGUI.BeginChangeCheck();

        // 1. Der Move-Pfeil (Handle)
        // Er erscheint direkt am Node.
        Vector3 newPos = Handles.PositionHandle(node.transform.position, Quaternion.identity);

        // 2. Das Namensschild (Label)
        // Wir machen es Cyan und fett, damit es gut lesbar ist
        GUIStyle labelStyle = new GUIStyle();
        labelStyle.normal.textColor = Color.cyan;
        labelStyle.fontSize = 12;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.alignment = TextAnchor.MiddleCenter;

        // Text etwas über dem Punkt anzeigen
        Handles.Label(node.transform.position + Vector3.up * 1.5f, node.name, labelStyle);

        // Wenn bewegt wurde -> Speichern & Undo ermöglichen
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(node.transform, "Move Sea Node");
            node.transform.position = newPos;
        }
    }
}