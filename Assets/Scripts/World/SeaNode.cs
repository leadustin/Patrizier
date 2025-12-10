using UnityEngine;

public class SeaNode : MonoBehaviour
{
    // Farbe für den Editor
    public Color gizmoColor = Color.cyan;
    public float gizmoSize = 0.5f;

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, gizmoSize);
    }

    // Wenn du das Objekt auswählst, wird es gelb
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, gizmoSize * 1.2f);
    }
}