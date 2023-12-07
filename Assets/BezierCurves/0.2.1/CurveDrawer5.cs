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
public class CurveDrawer5 : MonoBehaviour
{

    public List<float3> hullPoints = new List<float3>();
    public List<List<float3>> holePoints = new List<List<float3>>();
    public List<List<MyVector2>> holePoints_My = new List<List<MyVector2>>();
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

        Debug.Log(getOrientation(holePoints[0]));
        List<float3> boundingBoxVerticesOfHull = computeBoundingBox(hullPoints);
        foreach (float3 point in boundingBoxVerticesOfHull)
        {
            vertices.Add(new Vector3(point.x,point.y,point.z));
        }
        
        CurvesControlPointsPack[] ShaderinputData = new CurvesControlPointsPack[hullPoints.Count / 3];

        List<MyVector2> internalAreaCorners_My = new List<MyVector2>();
        List<List<MyVector2>> internalAreaHolesCorners = new List<List<MyVector2>>();

        List<CurvesControlPointsPack> curveDataList=new List<CurvesControlPointsPack>();

        Debug.Log(hullPoints.Count);

        //iterate through all points on the hull
        for (int i = 0; i < hullPoints.Count/3; i++)
        {
            CurvesControlPointsPack curveData = new CurvesControlPointsPack();
            curveData.start=new Vector4(hullPoints[i*3].x,
                    hullPoints[i * 3].y,
                    hullPoints[i * 3].z,0);
            curveData.control1 = new Vector4(hullPoints[i * 3 + 1].x,
                    hullPoints[i * 3 + 1].y,
                    hullPoints[i * 3 + 1].z, 0);
            curveData.control2 = new Vector4(hullPoints[i * 3 + 2].x,
                    hullPoints[i * 3 + 2].y,
                    hullPoints[i * 3 + 2].z, 0);
            curveData.end = new Vector4(hullPoints[i * 3 + 3].x,
                    hullPoints[i * 3 + 3].y,
                    hullPoints[i * 3 + 3].z, 0);
            Vector3 a = new Vector3(hullPoints[i * 3].x, hullPoints[i * 3].y, hullPoints[i * 3].z);
            Vector3 b = new Vector3(hullPoints[i * 3 + 1].x, hullPoints[i * 3 + 1].y, hullPoints[i * 3 + 1].z);
            Vector3 c = new Vector3(hullPoints[i * 3 + 2].x, hullPoints[i * 3 + 2].y, hullPoints[i * 3 + 2].z);
            int orientation = GetDirection(a,b,c);
            if (orientation == 0)
            {
                internalAreaCorners_My.Add(new MyVector2(hullPoints[i * 3].x, hullPoints[i * 3].y));
            }
            else if (orientation == 1)
            {
                internalAreaCorners_My.Add(new MyVector2(hullPoints[i * 3].x, hullPoints[i * 3].y));
                internalAreaCorners_My.Add(new MyVector2(hullPoints[i * 3 + 1].x, hullPoints[i * 3 + 1].y));
                internalAreaCorners_My.Add(new MyVector2(hullPoints[i * 3 + 2].x, hullPoints[i * 3 + 2].y));
            }
            curveData.orientation = orientation;
            curveData.fillAll = 0;
            curveDataList.Add(curveData);

        }
        //iterate through all holes
        for(int holeIndex=0; holeIndex < holePoints.Count; holeIndex++)
        {
            holePoints_My.Add(new List<MyVector2>());

            List<float3> boundingBoxVerticesOfAHole = computeBoundingBox(holePoints[holeIndex]);
            foreach (float3 point in boundingBoxVerticesOfAHole)
            {
                vertices.Add(new Vector3(point.x, point.y, point.z));
            }
            Debug.Log(boundingBoxVerticesOfAHole.Count);
            for (int j = 0; j < holePoints[holeIndex].Count-1; j++)
            {
                holePoints_My[holeIndex].Add(new MyVector2(holePoints[holeIndex][j].x, holePoints[holeIndex][j].y));
            }

            //iterate through all points of a hole
            for (int j = 0; j < holePoints[holeIndex].Count/3; j++)
            {

                CurvesControlPointsPack curveData = new CurvesControlPointsPack();
                curveData.start = new Vector4(holePoints[holeIndex][j * 3].x,
                        holePoints[holeIndex][j * 3].y,
                        holePoints[holeIndex][j * 3].z, 0);
                curveData.control1 = new Vector4(holePoints[holeIndex][j * 3 + 1].x,
                        holePoints[holeIndex][j * 3 + 1].y,
                        holePoints[holeIndex][j * 3 + 1].z, 0);
                curveData.control2 = new Vector4(holePoints[holeIndex][j * 3 + 2].x,
                        holePoints[holeIndex][j * 3 + 2].y,
                        holePoints[holeIndex][j * 3 + 2].z, 0);
                curveData.end = new Vector4(holePoints[holeIndex][j * 3 + 3].x,
                        holePoints[holeIndex][j * 3 + 3].y,
                        holePoints[holeIndex][j * 3 + 3].z, 0);
                Vector3 a = new Vector3(holePoints[holeIndex][j * 3].x, holePoints[holeIndex][j * 3].y, holePoints[holeIndex][j * 3].z);
                Vector3 b = new Vector3(holePoints[holeIndex][j * 3 + 1].x, holePoints[holeIndex][j * 3 + 1].y, holePoints[holeIndex][j * 3 + 1].z);
                Vector3 c = new Vector3(holePoints[holeIndex][j * 3 + 2].x, holePoints[holeIndex][j * 3 + 2].y, holePoints[holeIndex][j * 3 + 2].z);
                int orientation = GetDirection(a, b, c);
                curveData.orientation = orientation;
                curveData.fillAll = 0;
                curveDataList.Add(curveData);
            }
        }
        
        Debug.Log(internalAreaCorners_My.Count);
        /*
        for(int i=0; i<internalAreaCorners_My.Count; i++)
        {
            Debug.Log(internalAreaCorners_My[i].x.ToString() + internalAreaCorners_My[i].y.ToString());
        }
        */
        //Debug.Log(holePoints_My[0].Count);
        
        /*
        for(int i=0;i< holePoints_My[0].Count; i++)
        {
            Debug.Log(holePoints_My[0][i].x.ToString()+ holePoints_My[0][i].y.ToString());
        }
        */
        HashSet<Triangle2> triangles=_EarClipping.Triangulate(internalAreaCorners_My, holePoints_My);

        List<int> indices = new List<int>();

        int triangleIndex = 0;
        CurvesControlPointsPack[] ShaderinputData2 = new CurvesControlPointsPack[triangles.Count];
        foreach (var triangle in triangles)
        {
            vertices.Add(new Vector3(triangle.p1.x, triangle.p1.y, 0));
            vertices.Add(new Vector3(triangle.p2.x, triangle.p2.y, 0));
            vertices.Add(new Vector3(triangle.p3.x, triangle.p3.y, 0));
            ShaderinputData2[triangleIndex].fillAll = 1;
            triangleIndex++;
        }
        /*
        for(int i = 0; i < tmp.Length; i++)
        {
            Debug.Log(tmp[i].ToString());
        }
        */
        CurvesControlPointsPack[] ShaderinputData1 = curveDataList.ToArray();
        CurvesControlPointsPack[] ShaderinputData3 = new CurvesControlPointsPack[ShaderinputData1.Length + ShaderinputData2.Length];
        Array.Copy(ShaderinputData1, ShaderinputData3, ShaderinputData1.Length);
        Array.Copy(ShaderinputData2, 0, ShaderinputData3, ShaderinputData1.Length, ShaderinputData2.Length);

        Debug.Log(triangles.Count());
        Debug.Log(ShaderinputData1.Length);
        Debug.Log(ShaderinputData2.Length);
        Debug.Log(vertices.Count);

        
        for(int i=0; i< vertices.Count; i++)
        {
            Debug.Log(vertices[i]);
        }

        for (int i = 0; i < ShaderinputData3.Length; i++)
        {
            Debug.Log(ShaderinputData3[i].start.ToString() + "  " + ShaderinputData3[i].control1.ToString() + "  " + ShaderinputData3[i].control2.ToString() + "  " + ShaderinputData3[i].end.ToString() + "  " + ShaderinputData3[i].orientation.ToString());
        }
        
        for (int i = 0;i < vertices.Count; i++)
        {
            indices.Add(i);
        }
        
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);

        int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CurvesControlPointsPack));
        
        curveDataBuffer = new ComputeBuffer(ShaderinputData3.Length, stride);
        curveDataBuffer.SetData(ShaderinputData3);
        material.SetBuffer("curves", curveDataBuffer);

        //Debug.Log(ShaderinputData3.Length.ToString() + " " + tmp.Length.ToString() + " " + indices.Count.ToString());
        
        GetComponent<MeshFilter>().mesh = mesh;
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
