using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Segment15
{
    public Vector3 p0;
    public Vector3 p1;
    public Vector3 p2;
    public Vector3 p3;
    public float length;
    
    public Segment15()
    {
    }

    public Segment15(Vector3 initVec)
    {
        p0 = new Vector3(initVec.x,initVec.y,initVec.z);
        p1 = new Vector3(initVec.x,initVec.y,initVec.z);
        p2 = new Vector3(initVec.x,initVec.y,initVec.z);
        p3 = new Vector3(initVec.x,initVec.y,initVec.z);
    }
    public Segment15(Segment15 newSegment15)
    {
        p0 = newSegment15.p0;
        p1 = newSegment15.p1;
        p2 = newSegment15.p2;
        p3 = newSegment15.p3;
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
            Vector3 point0=math15.BezierCubic(p0, p1, p2, p3, unit * i);
            Vector3 point1=math15.BezierCubic(p0, p1, p2, p3, unit * (i+1));
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
        p0 = math15.RotateRadians(p0, radian);
        p1 = math15.RotateRadians(p1, radian);
        p2 = math15.RotateRadians(p2, radian);
        p3 = math15.RotateRadians(p3, radian);
    }

    public override string ToString()
    {
        return p0 + " " + p1 + " " + p2 + " " + p3;
    }
}

public class SubPath15
{
    
    public List<Segment15> segments;
    public int orientation;
    public SubPath15(int orientation,List<Segment15> controlPoints)
    {
        this.segments = new List<Segment15>(controlPoints);
        this.orientation = orientation;
    }
    public SubPath15()
    {
        segments = new List<Segment15>();
    }

    public SubPath15(Vector3 initialVec)
    {
        segments = new List<Segment15>();
        segments.Add(new Segment15(initialVec));
    }
    public Vector3 GetBasePoint()
    {
        return segments[0].p0;
    }
    public void Add(Segment15 newSegment)
    {
        segments.Add(newSegment);
    }

    public Vector3 getCentralPoint()
    {
        Vector3 total = new Vector3(0,0,0);
        foreach(var segment in segments)
        {
            total += segment.p0;
        }

        return total / segments.Count;
    }
    public SubPath15 transform(float x, float y)
    {
        SubPath15 tmp = new SubPath15();
        foreach (var segment in segments)
        {
            Segment15 ttmp = new Segment15();
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

public class Path15
{
    
    public List<SubPath15> subPaths;
    public Path15()
    {
        subPaths = new List<SubPath15>();
    }
    public Path15(List<SubPath15> subPaths)
    {
        this.subPaths = new List<SubPath15>(subPaths);
    }
    public void Add(SubPath15 newSubPath)
    {
        subPaths.Add(newSubPath);
    }

    public Vector3 getCentralPoint()
    {
        Vector3 centralPoint = new Vector3(0, 0, 0);
        foreach (SubPath15 sp in subPaths)
        {
            centralPoint += sp.getCentralPoint();
        }
        return centralPoint / subPaths.Count;
    }
    public Path15 transform(float x, float y)
    {
        List<SubPath15> tmp = new List<SubPath15>();
        foreach (var subpath in subPaths)
        {
            tmp.Add(subpath.transform(x,y));
        }
        return new Path15(tmp);
    }
}