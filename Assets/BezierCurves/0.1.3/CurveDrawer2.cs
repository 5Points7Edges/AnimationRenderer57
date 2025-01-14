using GenericShape;
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
using Color = UnityEngine.Color;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CurveDrawer2 : MonoBehaviour
{

    public List<float3> controlPoints = new List<float3>();

    public Material material;
    public ComputeShader BoundingBoxComputeShader;
    public List<GameObject> gameObjects = new List<GameObject>();

    //public const float C_circle = (float)0.55228;
    public float C_circle = (float)0.8;



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

        for (int i = 0; i < controlPoints.Count; i++)
        {
            controlPoints[i] = new float3(transform.TransformPoint(new Vector3(controlPoints[i].x, controlPoints[i].y, controlPoints[i].z)));
        }

        /*
        for(int i=0; i<hullPoints.Count; i++)
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

        List<int> indices = new List<int>() { 0, 1, 2, 3, 4, 5 }; //indices 永远是这个数列，就放在循环外面了

        List<Vector2> internalAreaCorners = new List<Vector2>();

        //create every curve as a Gameobject
        for (int curveIndex = 0; curveIndex < numberOfCurves; curveIndex++)
        {
            GameObject curve = new GameObject("curve[" + curveIndex + "]");

            curve.AddComponent<MeshFilter>();
            curve.AddComponent<MeshRenderer>();

            Vector3[] corners = new Vector3[numberOfOutputPoints]; //boundingBoxVertices转成Vector类型
            for (int j = 0; j < numberOfOutputPoints; j++)
            {
                corners[j] = new Vector3(boundingBoxVertices[curveIndex * numberOfOutputPoints + j].x,
                    boundingBoxVertices[curveIndex * numberOfOutputPoints + j].y,
                    boundingBoxVertices[curveIndex * numberOfOutputPoints + j].z);

            }

            Mesh mesh = new Mesh();

            int orientation = GetOrientation(corners[0], corners[1], corners[2]);
            if (orientation == 0)
            {
                internalAreaCorners.Add(new Vector2(corners[0].x, corners[0].y));
            }
            else if (orientation == 1)
            {
                internalAreaCorners.Add(new Vector2(corners[0].x, corners[0].y));
                internalAreaCorners.Add(new Vector2(corners[1].x, corners[1].y));
                internalAreaCorners.Add(new Vector2(corners[2].x, corners[2].y));
            }

            mesh.vertices = corners;
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);

            curve.GetComponent<MeshFilter>().mesh = mesh;
            curve.GetComponent<MeshRenderer>().material = material;

            curve.GetComponent<MeshRenderer>().material.SetInt("_Orientation", orientation);

            //Debug.Log(GetDirection(corners[0], corners[1], corners[2]));

            curve.transform.parent = transform;
            gameObjects.Add(curve);
        }
        //create a mesh made of interial triangles

        GameObject internalArea = new GameObject("internalArea");

        internalArea.AddComponent<MeshFilter>();
        internalArea.AddComponent<MeshRenderer>();


        List<int> indicesOfInternalArea = new List<int>();

        Polygon node = new Polygon(internalAreaCorners.ToArray());
        Triangle[] triangles;
        triangles = node.Triangulate();

        Vector3[] internalAreaTriangles_V = new Vector3[triangles.Length * 3];

        for (int i = 0; i < triangles.Length; i++)
        {
            internalAreaTriangles_V[i * 3] = new Vector3(triangles[i].a.x, triangles[i].a.y, 0);
            internalAreaTriangles_V[i * 3 + 1] = new Vector3(triangles[i].b.x, triangles[i].b.y, 0);
            internalAreaTriangles_V[i * 3 + 2] = new Vector3(triangles[i].c.x, triangles[i].c.y, 0);
            //Debug.Log(triangles[i].a);
            //Debug.Log(triangles[i].b);
            //Debug.Log(triangles[i].c);
        }


        for (int i = 0; i < internalAreaTriangles_V.Length; i++)
        {
            indicesOfInternalArea.Add(i);
        }
        Mesh meshOfInternalArea = new Mesh();

        meshOfInternalArea.vertices = internalAreaTriangles_V;
        meshOfInternalArea.SetIndices(indicesOfInternalArea, MeshTopology.Triangles, 0);

        internalArea.GetComponent<MeshFilter>().mesh = meshOfInternalArea;
        internalArea.GetComponent<MeshRenderer>().material = material;
        internalArea.GetComponent<MeshRenderer>().material.SetFloat("_fillAll", 1);
        //internalArea.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.red);
        internalArea.transform.parent = transform;

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
        Vector2[] vec = new Vector2[4];

        for (int curveIndex = 0; curveIndex < gameObjects.Count; curveIndex++)
        {
            for (int i = 0; i < 4; i++)
            {

                vec[i] = Camera.main.WorldToScreenPoint(controlPoints[curveIndex * 3 + i]);

            }
            gameObjects[curveIndex].GetComponent<MeshRenderer>().material.SetVector("_start", new Vector2(vec[0].x, vec[0].y));
            gameObjects[curveIndex].GetComponent<MeshRenderer>().material.SetVector("_control1", new Vector2(vec[1].x, vec[1].y));
            gameObjects[curveIndex].GetComponent<MeshRenderer>().material.SetVector("_control2", new Vector2(vec[2].x, vec[2].y));
            gameObjects[curveIndex].GetComponent<MeshRenderer>().material.SetVector("_end", new Vector2(vec[3].x, vec[3].y));
        }
    }
}
