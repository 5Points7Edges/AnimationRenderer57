using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class math14
{
    public static Vector3 BezierCubic(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        Vector3 result = (float)Math.Pow((1 - t),3)*p0 + (float)Math.Pow((1 - t),2)*t*3*p1 + 3*t*t*(1-t)*p2+(float)Math.Pow(t,3)*p3;
        return result;
    }
    public static Vector3 RotateRadians(Vector3 v, float radians)
    {
        var ca = (float)Math.Cos(radians);
        var sa = (float)Math.Sin(radians);
        return new Vector3(ca*v.x - sa*v.y, sa*v.x + ca*v.y,0);
    }
}

