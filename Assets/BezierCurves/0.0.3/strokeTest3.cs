using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
/*
 * Author:Thomas Lu
 * v0.0.3
 * cubic bezier curve stroke
 * This renderer can draw a series of bezier curve segments.
 * input data structure is just a series of points (Number of points=3*N+1 N=number of cubic bezier curves)
 * A simple bounding box is computed
 * The color and width are customizable
 */
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class strokeTest3 : MonoBehaviour
{

    public List<float3> controlPoints = new List<float3>();

    public Material material;
    public ComputeShader BoundingBoxComputeShader;
    public List<GameObject> gameObjects = new List<GameObject>();

    //public const float C_circle = (float)0.55228;
    public const float C_circle = (float)0.8;


    void generateCircle(float radius)
    {
        controlPoints.Add(new float3(radius,0, 0));

        controlPoints.Add(new float3(radius, radius*C_circle, 0));
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

    }

    // Start is called before the first frame update
    void Start()
    {
        if (!material)
        {
            Debug.LogError("Please Assign a material on the inspector");
            return;
        }

        generateCircle(radius:10);

        int numberOfCurves = controlPoints.Count/3; //两个curve共用一个端点

        // 创建ComputeBuffer
        ComputeBuffer BoundingBoxComputeBuffer_IN = new ComputeBuffer(controlPoints.Count+1, sizeof(float) * 3); // float3的大小为3个float
        ComputeBuffer BoundingBoxComputeBuffer_OUT = new ComputeBuffer(numberOfCurves*12, sizeof(float) * 3);
        // 将数据传入ComputeBuffer
        controlPoints.Add(controlPoints[0]);
        BoundingBoxComputeBuffer_IN.SetData(controlPoints.ToArray());
        
        int kernelIndex = BoundingBoxComputeShader.FindKernel("CSMain");
        // 将ComputeBuffer设置到shader中
        BoundingBoxComputeShader.SetBuffer(kernelIndex, "Input", BoundingBoxComputeBuffer_IN);
        BoundingBoxComputeShader.SetBuffer(kernelIndex, "Result", BoundingBoxComputeBuffer_OUT);

        //TODO:numberOfCurves 应当有最大限制

        // 执行Compute Shader
        BoundingBoxComputeShader.Dispatch(kernelIndex, numberOfCurves, 1, 1);

        float3[] boundingBoxVertices = new float3[numberOfCurves * 12];
        //读取结果
        BoundingBoxComputeBuffer_OUT.GetData(boundingBoxVertices);

        
        // 在使用完ComputeBuffer后，记得释放资源
        BoundingBoxComputeBuffer_IN.Release();
        BoundingBoxComputeBuffer_OUT.Release();

        List<int> indices = new List<int>() {0,1,2,3,4,5,6,7,8,9,10,11}; //indices 永远是从零开始的数列，就放在循环外面了
        /*
        for (int i = 0; i < 12; i++)
        {
            indices.Add(i);
        }
        */
        for (int curveIndex = 0; curveIndex < numberOfCurves; curveIndex++)
        {
            GameObject curve = new GameObject("curve[" + curveIndex + "]");
            
            curve.AddComponent<MeshFilter>();
            curve.AddComponent<MeshRenderer>();

            Vector3[] corners = new Vector3[12]; //boundingBoxVertices转成Vector类型
            for (int j = 0; j < 12; j++)
            {
                corners[j] = transform.TransformPoint(new Vector3(boundingBoxVertices[curveIndex * 12 + j].x, 
                    boundingBoxVertices[curveIndex * 12 + j].y, 
                    boundingBoxVertices[curveIndex * 12 + j].z));
            }

            Mesh mesh = new Mesh();
            mesh.vertices = corners;
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            
            curve.GetComponent<MeshFilter>().mesh = mesh;
            curve.GetComponent<MeshRenderer>().material = material;

            curve.transform.parent = transform;
            gameObjects.Add(curve);
        }

        /*
        Debug.Log(gameObjects[0].GetComponent<MeshFilter>().mesh.vertices[0]);
        Debug.Log(gameObjects[0].GetComponent<MeshFilter>().mesh.vertices[1]);
        Debug.Log(gameObjects[0].GetComponent<MeshFilter>().mesh.vertices[2]);

        Debug.Log(gameObjects[0].GetComponent<MeshFilter>().mesh.vertices[3]);
        Debug.Log(gameObjects[0].GetComponent<MeshFilter>().mesh.vertices[4]);
        Debug.Log(gameObjects[0].GetComponent<MeshFilter>().mesh.vertices[5]);

        Debug.Log(gameObjects[0].GetComponent<MeshFilter>().mesh.vertices[6]);
        Debug.Log(gameObjects[0].GetComponent<MeshFilter>().mesh.vertices[7]);
        Debug.Log(gameObjects[0].GetComponent<MeshFilter>().mesh.vertices[8]);
        */
    }
   
    // Update is called once per frame
    void Update()
    {
        Vector2[] vec = new Vector2[4];
        
        for (int curveIndex=0;curveIndex < gameObjects.Count;curveIndex++)
        {
            for (int j = 0; j < 4; j++)
            {
                
                vec[j] = Camera.main.WorldToScreenPoint(transform.TransformPoint(controlPoints[curveIndex*3+j]));

            }
            gameObjects[curveIndex].GetComponent<MeshRenderer>().material.SetVector("_start", new Vector2(vec[0].x, vec[0].y));
            gameObjects[curveIndex].GetComponent<MeshRenderer>().material.SetVector("_control1", new Vector2(vec[1].x, vec[1].y));
            gameObjects[curveIndex].GetComponent<MeshRenderer>().material.SetVector("_control2", new Vector2(vec[2].x, vec[2].y));
            gameObjects[curveIndex].GetComponent<MeshRenderer>().material.SetVector("_end", new Vector2(vec[3].x, vec[3].y));
        }
    }
}
