using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class SubPath8
{
    public List<float3> controlPoints;
    public int orientation;
    public float3 basePoint;
    public SubPath8(int orientation,List<float3> controlPoints)
    {
        this.controlPoints = controlPoints;
        this.orientation = orientation;
        basePoint = this.controlPoints[0];
    }
}
