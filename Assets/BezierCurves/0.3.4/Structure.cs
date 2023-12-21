using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Segment11
{
    public Vector3 p0;
    public Vector3 p1;
    public Vector3 p2;
    public Vector3 p3;
    public float length;
    
    public Segment11()
    {
    }
    public Segment11(Segment11 newSegment11)
    {
        p0 = newSegment11.p0;
        p1 = newSegment11.p1;
        p2 = newSegment11.p2;
        p3 = newSegment11.p3;
    }
    public (Vector3,Vector3,Vector3,Vector3,Vector3,Vector3,Vector3,Vector3) SplitCurve(float t)
    {
        Vector3 p01 = (p1 - p0) * t + p0;
        Vector3 p12 = (p2 - p1) * t + p1;
        Vector3 p23 = (p3 - p2) * t + p2;
        Vector3 p012 = (p12 - p01) * t + p01;
        Vector3 p123 = (p23 - p12) * t + p12;
        Vector3 p0123 = (p123 - p012) * t + p012;

        return (p0, p01, p012, p0123, p0123, p123,p23, p3);
    }
    public float GetLength()
    {
        float totalLength = 0;
        int approximation = 2;
        float unit = (float)1 / approximation;
        for (int i = 0; i < approximation; i++)
        {
            Vector3 point0=math.BezierCubic(p0, p1, p2, p3, unit * i);
            Vector3 point1=math.BezierCubic(p0, p1, p2, p3, unit * (i+1));
            totalLength+=Vector3.Distance(point0,point1);
        }

        length = totalLength;
        return totalLength;
    }

    public override string ToString()
    {
        return p0 + " " + p1 + " " + p2 + " " + p3;
    }
}

public class SubPath11
{
    
    public List<Segment11> segments;
    public int orientation;
    public SubPath11(int orientation,List<Segment11> controlPoints)
    {
        this.segments = new List<Segment11>(controlPoints);
        this.orientation = orientation;
    }
    public SubPath11()
    {
        segments = new List<Segment11>();
    }

    public Vector3 GetBasePoint()
    {
        return segments[0].p0;
    }
    public void Add(Segment11 newSegment)
    {
        segments.Add(newSegment);
    }

    public override string ToString()
    {
        Debug.Log("----------------------------");
        foreach (var t in segments)
        {
            Debug.Log(t);
        };
        return "----------------------------";
    }
}

public class Path11
{
    
    public List<SubPath11> subPaths;
    public Path11()
    {
        subPaths = new List<SubPath11>();
    }
    public Path11(List<SubPath11> subPaths)
    {
        this.subPaths = new List<SubPath11>(subPaths);
    }
    public void Add(SubPath11 newSubPath)
    {
        subPaths.Add(newSubPath);
    }
}