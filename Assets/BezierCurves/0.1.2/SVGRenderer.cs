/*
 * Author:Thomas Lu
 * v0.1.2
 * cubic bezier curve fill
 * This renderer can fill a single convex or concave cubic curve defined polygon.
 * The triangulation of internal area of a shape is given by hand.
 * input data structure is a svg file (It renders the first path it finds)
 * (Don't use multiple M, and numbers should already be parsed by " ")
 * The color is customizable
 */

using GenericShape;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SVGRendererTest : MonoBehaviour
{

    public List<float3> controlPoints = new List<float3>();

    public Material material;
    public ComputeShader BoundingBoxComputeShader;
    public List<GameObject> gameObjects = new List<GameObject>();

    //public const float C_circle = (float)0.55228;
    public float C_circle = (float)0.8;

    public TextAsset svgFile;

    void generateCircle(float radius)
    {
        controlPoints.Add(new float3(radius, 0, 0));

        controlPoints.Add(new float3(radius, radius * C_circle, 0));
        controlPoints.Add(new float3(radius * C_circle, radius, 0));

        controlPoints.Add(new float3(0, radius, 0));

        controlPoints.Add(new float3(-radius * C_circle, radius, 0));
        controlPoints.Add(new float3(-radius, radius * C_circle, 0));

        controlPoints.Add(new float3(-radius, 0, 0));

        controlPoints.Add(new float3(-radius, -radius * C_circle, 0));
        controlPoints.Add(new float3(-radius * C_circle, -radius, 0));

        controlPoints.Add(new float3(0, -radius, 0));

        controlPoints.Add(new float3(radius * C_circle, -radius, 0));
        controlPoints.Add(new float3(radius, -radius * C_circle, 0));
        controlPoints.Add(controlPoints[0]);
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

        Parsing(svgFile.text);
        controlPoints.Reverse();

        for (int i = 0; i < controlPoints.Count; i++)
        {
            controlPoints[i]= transform.TransformPoint(new Vector3(controlPoints[i].x, controlPoints[i].y, controlPoints[i].z));
        }
        
        /*
        for(int i=0; i<hullPoints.Count; i++)
        {
            Debug.Log(hullPoints[i]);
        }
        */

        //generateCircle(radius: 10);

        int numberOfCurves = controlPoints.Count / 3; //两个curve共用一个端点
        const int numberOfOutputPoints = 6;

        // 创建ComputeBuffer
        ComputeBuffer BoundingBoxComputeBuffer_IN = new ComputeBuffer(controlPoints.Count + 1, sizeof(float) * 3); // float3的大小为3个float
        ComputeBuffer BoundingBoxComputeBuffer_OUT = new ComputeBuffer(numberOfCurves * numberOfOutputPoints, sizeof(float) * 3);
        // 将数据传入ComputeBuffer
        BoundingBoxComputeBuffer_IN.SetData(controlPoints.ToArray());

        int kernelIndex = BoundingBoxComputeShader.FindKernel("CSMain");
        // 将ComputeBuffer设置到shader中
        BoundingBoxComputeShader.SetBuffer(kernelIndex, "Input", BoundingBoxComputeBuffer_IN);
        BoundingBoxComputeShader.SetBuffer(kernelIndex, "Result", BoundingBoxComputeBuffer_OUT);

        //TODO:numberOfCurves 应当有最大限制

        // 执行Compute Shader
        BoundingBoxComputeShader.Dispatch(kernelIndex, numberOfCurves, 1, 1);

        float3[] boundingBoxVertices = new float3[numberOfCurves * numberOfOutputPoints];
        //读取结果
        BoundingBoxComputeBuffer_OUT.GetData(boundingBoxVertices);


        // 在使用完ComputeBuffer后，记得释放资源
        BoundingBoxComputeBuffer_IN.Release();
        BoundingBoxComputeBuffer_OUT.Release();

        List<int> indices = new List<int>() { 0, 1, 2, 3, 4, 5 }; //indices 永远是这个数列，就放在循环外面了

        List<Vector2> internalAreaCorners = new List<Vector2>();

        //创建曲线Object
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
        //创建一个内部由三角形组成的mesh
        
        GameObject internalArea = new GameObject("internalArea");

        internalArea.AddComponent<MeshFilter>();
        internalArea.AddComponent<MeshRenderer>();

        
        List<int> indicesOfInternalArea = new List<int>();

        Polygon node = new Polygon(internalAreaCorners.ToArray());
        Triangle[] triangles;
        triangles = node.Triangulate();

        Vector3[] internalAreaTriangles_V = new Vector3[triangles.Length*3];

        for (int i = 0; i < triangles.Length; i++)
        {
            internalAreaTriangles_V[i * 3] = new Vector3(triangles[i].a.x, triangles[i].a.y, 0);
            internalAreaTriangles_V[i * 3+1] = new Vector3(triangles[i].b.x, triangles[i].b.y, 0);
            internalAreaTriangles_V[i * 3+2] = new Vector3(triangles[i].c.x, triangles[i].c.y, 0);
            //Debug.Log(triangles[i].a);
            //Debug.Log(triangles[i].b);
            //Debug.Log(triangles[i].c);
        }

        
        for (int i=0;i < internalAreaTriangles_V.Length; i++)
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

    protected void Parsing(string input)
    {
        string pattern = @"<path\s[^>]*d\s*=\s*[""']([^""']*)[""'][^>]*>";

        Regex regex = new Regex(pattern);

        Match match = regex.Match(input);

        Match firstMatch = match;

        while (match.Success)
        {
            string dAttributeValue = match.Groups[1].Value;
            Debug.Log($"Value of d attribute: {dAttributeValue}");
            match=match.NextMatch();
        }

        //can only render the first path
        string[] tokens = firstMatch.Groups[1].Value.Split(' ');
        
        for (int i = 0; i < tokens.Length; i++)
        {
            if (tokens[i] == "M")
            {
                // Convert six consecutive numbers to Vector2
                float x1 = float.Parse(tokens[i + 1]);
                float y1 = float.Parse(tokens[i + 2]);
                // Add to the list
                controlPoints.Add(new float3(x1 , y1 , 0));
                // Skip the processed numbers
                i += 2;
            }
            if (tokens[i] == "C" && i + 6 < tokens.Length)
            {
                // Convert six consecutive numbers to Vector2
                float x1 = float.Parse(tokens[i + 1]);
                float y1 = float.Parse(tokens[i + 2]);
                float x2 = float.Parse(tokens[i + 3]);
                float y2 = float.Parse(tokens[i + 4]);
                float x3 = float.Parse(tokens[i + 5]);
                float y3 = float.Parse(tokens[i + 6]);

                // Add to the list
                controlPoints.Add(new float3(x1 , y1, 0));
                controlPoints.Add(new float3(x2 , y2, 0));
                controlPoints.Add(new float3(x3 , y3, 0));

                // Skip the processed numbers
                i += 6;
            }
        }

        //hullPoints.Add(new float3(1,2,3));
    }
    protected int GetOrientation(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Vector3 edge1 = p2 - p1;
        Vector3 edge2 = p3 - p2;

        // 计算叉积
        Vector3 crossProduct = Vector3.Cross(edge1, edge2);

        // 判断三角形的朝向
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
            for (int j = 0; j < 4; j++)
            {

                vec[j] = Camera.main.WorldToScreenPoint(controlPoints[curveIndex * 3 + j]);

            }
            gameObjects[curveIndex].GetComponent<MeshRenderer>().material.SetVector("_start", new Vector2(vec[0].x, vec[0].y));
            gameObjects[curveIndex].GetComponent<MeshRenderer>().material.SetVector("_control1", new Vector2(vec[1].x, vec[1].y));
            gameObjects[curveIndex].GetComponent<MeshRenderer>().material.SetVector("_control2", new Vector2(vec[2].x, vec[2].y));
            gameObjects[curveIndex].GetComponent<MeshRenderer>().material.SetVector("_end", new Vector2(vec[3].x, vec[3].y));
        }
    }
}
