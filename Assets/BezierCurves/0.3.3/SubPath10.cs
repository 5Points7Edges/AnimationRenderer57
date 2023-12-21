using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


public class SubPath10
{
    
    public List<Vector3> controlPoints;
    public int orientation;
    public SubPath10(int orientation,List<Vector3> controlPoints)
    {
        this.controlPoints = new List<Vector3>(controlPoints);
        this.orientation = orientation;
    }

    
}
