using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class SubPath9
{
    public List<Vector3> controlPoints;
    public int orientation;
    public Vector3 basePoint;
    public SubPath9(int orientation,List<Vector3> controlPoints)
    {
        this.controlPoints = controlPoints;
        this.orientation = orientation;
        basePoint = this.controlPoints[0];
    }
}
