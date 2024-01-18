using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Segment14
{
    public Vector3 p0;
    public Vector3 p1;
    public Vector3 p2;
    public Vector3 p3;
    public float length;
    
    public Segment14()
    {
    }
    public Segment14(Segment14 newSegment14)
    {
        p0 = newSegment14.p0;
        p1 = newSegment14.p1;
        p2 = newSegment14.p2;
        p3 = newSegment14.p3;
    }
    public (Vector3,Vector3,Vector3,Vector3,Vector3,Vector3,Vector3,Vector3) SplitCurve(float t)
    {
        Vector3 p01 = (p1 - p0) * t + p0;
        Vector3 p14 = (p2 - p1) * t + p1;
        Vector3 p23 = (p3 - p2) * t + p2;
        Vector3 p014 = (p14 - p01) * t + p01;
        Vector3 p143 = (p23 - p14) * t + p14;
        Vector3 p0143 = (p143 - p014) * t + p014;

        return (p0, p01, p014, p0143, p0143, p143,p23, p3);
    }
    public float GetLength()
    {
        float totalLength = 0;
        int approximation = 2;
        float unit = (float)1 / approximation;
        for (int i = 0; i < approximation; i++)
        {
            Vector3 point0=math14.BezierCubic(p0, p1, p2, p3, unit * i);
            Vector3 point1=math14.BezierCubic(p0, p1, p2, p3, unit * (i+1));
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
        p0 = math14.RotateRadians(p0, radian);
        p1 = math14.RotateRadians(p1, radian);
        p2 = math14.RotateRadians(p2, radian);
        p3 = math14.RotateRadians(p3, radian);
    }

    public override string ToString()
    {
        return p0 + " " + p1 + " " + p2 + " " + p3;
    }
}

public class SubPath14
{
    
    public List<Segment14> segments;
    public int orientation;
    public SubPath14(int orientation,List<Segment14> controlPoints)
    {
        this.segments = new List<Segment14>(controlPoints);
        this.orientation = orientation;
    }
    public SubPath14()
    {
        segments = new List<Segment14>();
    }

    public Vector3 GetBasePoint()
    {
        return segments[0].p0;
    }
    public void Add(Segment14 newSegment)
    {
        segments.Add(newSegment);
    }

    public SubPath14 transform(float x, float y)
    {
        SubPath14 tmp = new SubPath14();
        foreach (var segment in segments)
        {
            Segment14 ttmp = new Segment14();
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

public class Path14
{
    
    public List<SubPath14> subPaths;
    public Path14()
    {
        subPaths = new List<SubPath14>();
    }
    public Path14(List<SubPath14> subPaths)
    {
        this.subPaths = new List<SubPath14>(subPaths);
    }
    public void Add(SubPath14 newSubPath)
    {
        subPaths.Add(newSubPath);
    }

    public Path14 transform(float x, float y)
    {
        List<SubPath14> tmp = new List<SubPath14>();
        foreach (var subpath in subPaths)
        {
            tmp.Add(subpath.transform(x,y));
        }
        return new Path14(tmp);
    }
}