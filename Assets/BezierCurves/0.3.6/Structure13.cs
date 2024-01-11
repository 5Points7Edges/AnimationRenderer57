using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Segment13
{
    public Vector3 p0;
    public Vector3 p1;
    public Vector3 p2;
    public Vector3 p3;
    public float length;
    
    public Segment13()
    {
    }
    public Segment13(Segment13 newSegment13)
    {
        p0 = newSegment13.p0;
        p1 = newSegment13.p1;
        p2 = newSegment13.p2;
        p3 = newSegment13.p3;
    }
    public (Vector3,Vector3,Vector3,Vector3,Vector3,Vector3,Vector3,Vector3) SplitCurve(float t)
    {
        Vector3 p01 = (p1 - p0) * t + p0;
        Vector3 p13 = (p2 - p1) * t + p1;
        Vector3 p23 = (p3 - p2) * t + p2;
        Vector3 p013 = (p13 - p01) * t + p01;
        Vector3 p133 = (p23 - p13) * t + p13;
        Vector3 p0133 = (p133 - p013) * t + p013;

        return (p0, p01, p013, p0133, p0133, p133,p23, p3);
    }
    public float GetLength()
    {
        float totalLength = 0;
        int approximation = 2;
        float unit = (float)1 / approximation;
        for (int i = 0; i < approximation; i++)
        {
            Vector3 point0=math13.BezierCubic(p0, p1, p2, p3, unit * i);
            Vector3 point1=math13.BezierCubic(p0, p1, p2, p3, unit * (i+1));
            totalLength+=Vector3.Distance(point0,point1);
        }

        length = totalLength;
        return totalLength;
    }

    public void reverse()
    {
        (p0,p3)=(p3,p0);
        (p1,p2)=(p2,p1);
    }
    public void rotate(float radian)
    {
        p0 = math13.RotateRadians(p0, radian);
        p1 = math13.RotateRadians(p1, radian);
        p2 = math13.RotateRadians(p2, radian);
        p3 = math13.RotateRadians(p3, radian);
    }

    public override string ToString()
    {
        return p0 + " " + p1 + " " + p2 + " " + p3;
    }
}

public class SubPath13
{
    
    public List<Segment13> segments;
    public int orientation;
    public SubPath13(int orientation,List<Segment13> controlPoints)
    {
        this.segments = new List<Segment13>(controlPoints);
        this.orientation = orientation;
    }
    public SubPath13()
    {
        segments = new List<Segment13>();
    }

    public Vector3 GetBasePoint()
    {
        return segments[0].p0;
    }
    public void Add(Segment13 newSegment)
    {
        segments.Add(newSegment);
    }

    public SubPath13 transform(float x, float y)
    {
        SubPath13 tmp = new SubPath13();
        foreach (var segment in segments)
        {
            Segment13 ttmp = new Segment13();
            ttmp.p0 = new Vector3(segment.p0.x + x, segment.p0.y + y, 0);
            ttmp.p1 = new Vector3(segment.p1.x + x, segment.p1.y + y, 0);
            ttmp.p2 = new Vector3(segment.p2.x + x, segment.p2.y + y, 0);
            ttmp.p3 = new Vector3(segment.p3.x + x, segment.p3.y + y, 0);
            ttmp.length = segment.length;
            tmp.segments.Add(ttmp);
        }

        tmp.orientation = this.orientation;
        return tmp;
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

public class Path13
{
    
    public List<SubPath13> subPaths;
    public Path13()
    {
        subPaths = new List<SubPath13>();
    }
    public Path13(List<SubPath13> subPaths)
    {
        this.subPaths = new List<SubPath13>(subPaths);
    }
    public void Add(SubPath13 newSubPath)
    {
        subPaths.Add(newSubPath);
    }

    public Path13 transform(float x, float y)
    {
        List<SubPath13> tmp = new List<SubPath13>();
        foreach (var subpath in subPaths)
        {
            tmp.Add(subpath.transform(x,y));
        }
        return new Path13(tmp);
    }
}