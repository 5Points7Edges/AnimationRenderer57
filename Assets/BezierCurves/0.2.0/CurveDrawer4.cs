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
using static UnityEngine.UI.GridLayoutGroup;
using Color = UnityEngine.Color;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CurveDrawer4 : MonoBehaviour
{

    public List<float3> controlPoints = new List<float3>();

    public Material material;
    public ComputeShader BoundingBoxComputeShader;


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
        /*
        for (int i = 0; i < hullPoints.Count; i++)
        {
            Debug.Log(hullPoints[i]);
        }
        */

        int numberOfCurves = controlPoints.Count / 3; //every two curves have one common point
        const int numberOfOutputPoints = 6;

        // create ComputeBuffer
        ComputeBuffer BoundingBoxComputeBuffer_IN = new ComputeBuffer(controlPoints.Count + 1, sizeof(float) * 3); // float3的大小为3个float
        ComputeBuffer BoundingBoxComputeBuffer_OUT = new ComputeBuffer(numberOfCurves * numberOfOutputPoints, sizeof(float) * 3);
        // transport data to ComputeBuffer
        BoundingBoxComputeBuffer_IN.SetData(controlPoints.ToArray());

        int kernelIndex = BoundingBoxComputeShader.FindKernel("CSMain");
        // set ComputeBuffer into the shader
        BoundingBoxComputeShader.SetBuffer(kernelIndex, "Input", BoundingBoxComputeBuffer_IN);
        BoundingBoxComputeShader.SetBuffer(kernelIndex, "Result", BoundingBoxComputeBuffer_OUT);

        //TODO:numberOfCurves should have a maximum limit

        // start the Compute Shader
        BoundingBoxComputeShader.Dispatch(kernelIndex, numberOfCurves, 1, 1);

        float3[] boundingBoxVertices = new float3[numberOfCurves * numberOfOutputPoints];
        //read result
        BoundingBoxComputeBuffer_OUT.GetData(boundingBoxVertices);


        // release buffer when compute shader has done its job
        BoundingBoxComputeBuffer_IN.Release();
        BoundingBoxComputeBuffer_OUT.Release();

        //Debug.Log("still Alive");
        List<MyVector2> internalAreaCorners = new List<MyVector2>();
        List<List<MyVector2>> internalAreaHolesCorners = new List<List<MyVector2>>();

        CurvesControlPointsPack[] ShaderinputData1=new CurvesControlPointsPack[controlPoints.Count / 3];

        Debug.Log(ShaderinputData1.Length);
        
        for (int j = 0; j < controlPoints.Count/3; j++)
        {
            ShaderinputData1[j].start=new Vector4(controlPoints[j*3].x,
                    controlPoints[j * 3].y,
                    controlPoints[j * 3].z,0);
            ShaderinputData1[j].control1 = new Vector4(controlPoints[j * 3 + 1].x,
                    controlPoints[j * 3 + 1].y,
                    controlPoints[j * 3 + 1].z, 0);
            ShaderinputData1[j].control2 = new Vector4(controlPoints[j * 3 + 2].x,
                    controlPoints[j * 3 + 2].y,
                    controlPoints[j * 3 + 2].z, 0);
            ShaderinputData1[j].end = new Vector4(controlPoints[j * 3 + 3].x,
                    controlPoints[j * 3 + 3].y,
                    controlPoints[j * 3 + 3].z, 0);
            Vector3 a = new Vector3(controlPoints[j * 3].x, controlPoints[j * 3].y, controlPoints[j * 3].z);
            Vector3 b = new Vector3(controlPoints[j * 3 + 1].x, controlPoints[j * 3 + 1].y, controlPoints[j * 3 + 1].z);
            Vector3 c = new Vector3(controlPoints[j * 3 + 2].x, controlPoints[j * 3 + 2].y, controlPoints[j * 3 + 2].z);
            int orientation = GetOrientation(a,b,c);
            if (orientation == 0)
            {
                internalAreaCorners.Add(new MyVector2(controlPoints[j * 3].x, controlPoints[j * 3].y));
            }
            else if (orientation == 1)
            {
                internalAreaCorners.Add(new MyVector2(controlPoints[j * 3].x, controlPoints[j * 3].y));
                internalAreaCorners.Add(new MyVector2(controlPoints[j * 3 + 1].x, controlPoints[j * 3 + 1].y));
                internalAreaCorners.Add(new MyVector2(controlPoints[j * 3 + 2].x, controlPoints[j * 3 + 2].y));
            }
            ShaderinputData1[j].orientation = orientation;
            ShaderinputData1[j].fillAll = 0;
        }
        /*
        for (int i = 0; i < ShaderinputData1.Length; i++)
        {
            Debug.Log(ShaderinputData1[i].start.ToString() + "  " + ShaderinputData1[i].control1.ToString()+ "  " + ShaderinputData1[i].control2.ToString() + "  " + ShaderinputData1[i].end.ToString());
        }
        */
        
        HashSet<Triangle2> triangles=_EarClipping.Triangulate(internalAreaCorners);


        List<int> indices = new List<int>();
        
        Vector3[] tmp = new Vector3[boundingBoxVertices.Length+ triangles.Count * 3];
        for (int i = 0; i < boundingBoxVertices.Length; i++)
        {
            tmp[i] = new Vector3(boundingBoxVertices[i].x, boundingBoxVertices[i].y, boundingBoxVertices[i].z);
        }
        int triangleIndex = 0;
        CurvesControlPointsPack[] ShaderinputData2 = new CurvesControlPointsPack[triangles.Count];
        foreach (var triangle in triangles)
        {
            tmp[boundingBoxVertices.Length + triangleIndex * 3] = new Vector3(triangle.p1.x, triangle.p1.y, 0);
            tmp[boundingBoxVertices.Length + triangleIndex * 3 + 1] = new Vector3(triangle.p2.x, triangle.p2.y, 0);
            tmp[boundingBoxVertices.Length + triangleIndex * 3 + 2] = new Vector3(triangle.p3.x, triangle.p3.y, 0);
            ShaderinputData2[triangleIndex].fillAll = 1;

            triangleIndex++;
        }
        /*
        for(int i = 0; i < tmp.Length; i++)
        {
            Debug.Log(tmp[i].ToString());
        }
        */
        CurvesControlPointsPack[] ShaderinputData3 = new CurvesControlPointsPack[ShaderinputData1.Length + ShaderinputData2.Length];
        Array.Copy(ShaderinputData1, ShaderinputData3, ShaderinputData1.Length);
        Array.Copy(ShaderinputData2, 0, ShaderinputData3, ShaderinputData1.Length, ShaderinputData2.Length);
        /*
        for (int i = 0; i < ShaderinputData3.Length; i++)
        {
            Debug.Log(ShaderinputData3[i].start.ToString() + "  " + ShaderinputData3[i].control1.ToString() + "  " + ShaderinputData3[i].control2.ToString() + "  " + ShaderinputData3[i].end.ToString() + "  " + ShaderinputData3[i].fillAll.ToString());
        }
        */
        for (int i = 0;i < tmp.Length; i++)
        {
            indices.Add(i);
        }
        Mesh mesh = new Mesh();
        mesh.vertices = tmp;
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);

        int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CurvesControlPointsPack));

        curveDataBuffer = new ComputeBuffer(ShaderinputData3.Length, stride);
        curveDataBuffer.SetData(ShaderinputData3);
        material.SetBuffer("curves", curveDataBuffer);

        //Debug.Log(ShaderinputData3.Length.ToString() + " " + tmp.Length.ToString() + " " + indices.Count.ToString());

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material = material;

    }

    protected int GetOrientation(Vector3 p1, Vector3 p2, Vector3 p3)
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
        curveDataBuffer.Release();
    }
}
