using GenericShape;
using Habrador_Computational_Geometry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
//using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.UI.GridLayoutGroup;
using Color = UnityEngine.Color;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CurveDrawer7 : MonoBehaviour
{
    
    public List<SubPath> allPoints = new List<SubPath>();
    public List<Vector3> vertices= new List<Vector3>();
    public Material material;

    
    private ComputeBuffer curveDataBuffer;
    struct CurvesControlPointsPack
    {
        public Vector4 start;
        public Vector4 control1;
        public Vector4 control2;
        public Vector4 end;
        public int fillAll;
        public int orientation;
    }

    // Start is called before the first frame update
    void Start()
    {
        Console.WriteLine($"Value of d attribute: ");
        if (!material)
        {
            Debug.LogError("Please Assign a material on the inspector");
            return;
        }

        //Debug.Log(getOrientation(holePoints[0]));
        for (int i = 0; i < allPoints.Count; i++)
        {
            List<float3> boundingBoxVerticesOfHull = computeBoundingBox(allPoints[i].controlPoints);
            foreach (float3 point in boundingBoxVerticesOfHull)
            {
                vertices.Add(new Vector3(point.x,point.y,point.z));
            }
        }
        
        
        
        //GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material = material;

    }
    protected List<float3> computeBoundingBox(List<float3> Points)
    {
        List<float3> boundingBoxVertices = new List<float3>();

        for(int i = 0; i < Points.Count/3; i++)
        {
            boundingBoxVertices.Add(Points[i * 3]);
            boundingBoxVertices.Add(Points[i * 3 + 1]);
            boundingBoxVertices.Add(Points[i * 3 + 2]);
            boundingBoxVertices.Add(Points[i * 3]);
            boundingBoxVertices.Add(Points[i * 3 + 2]);
            boundingBoxVertices.Add(Points[i * 3 + 3]);
        }

        return boundingBoxVertices;
    }

    
    protected int getOrientation(List<float3> polygon)
    {
        double d = 0;
        for (int i = 0; i < polygon.Count - 1; i++)
            d += -0.5 * (polygon[i + 1].y + polygon[i].y) * (polygon[i + 1].x - polygon[i].x);
        if (d > 0)
        {
            return 1; //counterclockwise
        }
        return 0;       //clockwise
    }
    protected int GetDirection(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Vector3 edge1 = p2 - p1;
        Vector3 edge2 = p3 - p2;

        Vector3 crossProduct = Vector3.Cross(edge1, edge2);

        // decide which side the triangle points at
        if (crossProduct.z > 0)
        {
            return 0;
        }
        else if (crossProduct.z <= 0)
        {
            return 1;
        }
        return 1;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnDestroy()
    {
        //curveDataBuffer.Release();
    }
}
