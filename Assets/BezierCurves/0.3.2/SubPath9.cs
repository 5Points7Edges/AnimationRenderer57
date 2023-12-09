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
        this.controlPoints = new List<Vector3>(controlPoints);
        this.orientation = orientation;
        basePoint = this.controlPoints[0];
    }

    public void calculateBasePoint()
    {
        basePoint = this.controlPoints[0];
    }
    public SubPath9(SubPath9 subPath)
    {
        this.controlPoints = subPath.controlPoints;
        this.orientation = subPath.orientation;
        this.basePoint = subPath.basePoint;
    }
}
