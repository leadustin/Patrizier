using UnityEngine;

public class SeaLane : MonoBehaviour
{
    public SeaNode startNode;
    public SeaNode endNode;

    [Header("Kurven-Griffe")]
    public Transform controlPointA; // Griff nahe Start
    public Transform controlPointB; // Griff nahe Ende

    // Berechnet die Position auf der Kurve (t = 0 bis 1)
    public Vector3 GetPointAt(float t)
    {
        if (startNode == null || endNode == null || controlPointA == null || controlPointB == null)
            return Vector3.zero;

        Vector3 p0 = startNode.transform.position;
        Vector3 p1 = controlPointA.position;
        Vector3 p2 = controlPointB.position;
        Vector3 p3 = endNode.transform.position;

        // Kubische Bezier-Formel
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 p = uuu * p0;
        p += 3 * uu * t * p1;
        p += 3 * u * tt * p2;
        p += ttt * p3;

        return p;
    }

    // Zeichnet die Linie im Editor
    void OnDrawGizmos()
    {
        if (startNode == null || endNode == null || controlPointA == null || controlPointB == null) return;

        Gizmos.color = Color.yellow;
        Vector3 prev = startNode.transform.position;

        for (int i = 1; i <= 20; i++)
        {
            float t = i / 20f;
            Vector3 current = GetPointAt(t);
            Gizmos.DrawLine(prev, current);
            prev = current;
        }

        // Griffe anzeigen
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(startNode.transform.position, controlPointA.position);
        Gizmos.DrawLine(endNode.transform.position, controlPointB.position);
        Gizmos.DrawSphere(controlPointA.position, 0.1f);
        Gizmos.DrawSphere(controlPointB.position, 0.1f);
    }
}