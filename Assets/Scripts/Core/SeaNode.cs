using UnityEngine;
using System.Collections.Generic;

public class SeaNode : MonoBehaviour
{
    // Jeder Knoten kennt seine "Ausgänge" (die Straßen, die von hier wegführen)
    public List<SeaLane> outgoingLanes = new List<SeaLane>();

    void OnDrawGizmos()
    {
        // Zeichnet den Knoten als blaue Kugel im Editor
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.3f);
    }
}