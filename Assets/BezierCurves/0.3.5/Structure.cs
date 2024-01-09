using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Segment12
{
    public Vector3 p0;
    public Vector3 p1;
    public Vector3 p2;
    public Vector3 p3;
    public float length;
    
    public Segment12()
    {
    }
    public Segment12(Segment12 newSegment12)
    {
        p0 = newSegment12.p0;
        p1 = newSegment12.p1;
        p2 = newSegment12.p2;
        p3 = newSegment12.p3;
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
            Vector3 point0=math12.BezierCubic(p0, p1, p2, p3, unit * i);
            Vector3 point1=math12.BezierCubic(p0, p1, p2, p3, unit * (i+1));
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
        p0 = math12.RotateRadians(p0, radian);
        p1 = math12.RotateRadians(p1, radian);
        p2 = math12.RotateRadians(p2, radian);
        p3 = math12.RotateRadians(p3, radian);
    }

    public override string ToString()
    {
        return p0 + " " + p1 + " " + p2 + " " + p3;
    }
}

public class SubPath12
{
    
    public List<Segment12> segments;
    public int orientation;
    public SubPath12(int orientation,List<Segment12> controlPoints)
    {
        this.segments = new List<Segment12>(controlPoints);
        this.orientation = orientation;
    }
    public SubPath12()
    {
        segments = new List<Segment12>();
    }

    public Vector3 GetBasePoint()
    {
        return segments[0].p0;
    }
    public void Add(Segment12 newSegment)
    {
        segments.Add(newSegment);
    }

    public SubPath12 transform(float x, float y)
    {
        SubPath12 tmp = new SubPath12();
        foreach (var segment in segments)
        {
            Segment12 ttmp = new Segment12();
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

public class Path12
{
    
    public List<SubPath12> subPaths;
    public Path12()
    {
        subPaths = new List<SubPath12>();
    }
    public Path12(List<SubPath12> subPaths)
    {
        this.subPaths = new List<SubPath12>(subPaths);
    }
    public void Add(SubPath12 newSubPath)
    {
        subPaths.Add(newSubPath);
    }

    public Path12 transform(float x, float y)
    {
        List<SubPath12> tmp = new List<SubPath12>();
        foreach (var subpath in subPaths)
        {
            tmp.Add(subpath.transform(x,y));
        }
        return new Path12(tmp);
    }
}