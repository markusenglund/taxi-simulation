using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GraphSettings", menuName = "GraphSettings")]

public class GraphSettings : ScriptableObject
{
    public Vector3 waitingTimeGraphPos;
    public Vector3 supplyDemandGraphPos;
    public Vector3 driverProfitGraphPos;
    public Vector3 passengerSurplusGraphPos;
    public Vector3 passengerScatterPlotPos;
    public Vector3 resultsInfoPos;

}
