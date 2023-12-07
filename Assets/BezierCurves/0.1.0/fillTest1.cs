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
 * v0.1.0
 * cubic bezier curve fill
 * This renderer can fill a single convex cubic curve defined polygon.
 * The triangulation of internal area of a shape is given by hand.
 * input data structure is just a series of points (Number of points=3*N+1 N=number of cubic bezier curves)
 * The color is customizable
 */
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class fillTest1 : MonoBehaviour
{

    public List<float3> controlPoints = new List<float3>();

    public Material material;
    public ComputeShader BoundingBoxComputeShader;
    public List<GameObject> gameObjects = new List<GameObject>();

    //public const float C_circle = (float)0.55228;
    public float C_circle = (float)0.8;


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

    }

    // Start is called before the first frame update
    void Start()
    {
        if (!material)
        {
            Debug.LogError("Please Assign a material on the inspector");
            return;
        }

        generateCircle(radius: 10);

        int numberOfCurves = controlPoints.Count / 3; //����curve����һ���˵�
        const int numberOfOutputPoints = 6;

        // ����ComputeBuffer
        ComputeBuffer BoundingBoxComputeBuffer_IN = new ComputeBuffer(controlPoints.Count + 1, sizeof(float) * 3); // float3�Ĵ�СΪ3��float
        ComputeBuffer BoundingBoxComputeBuffer_OUT = new ComputeBuffer(numberOfCurves * numberOfOutputPoints, sizeof(float) * 3);
        // �����ݴ���ComputeBuffer
        controlPoints.Add(controlPoints[0]);
        BoundingBoxComputeBuffer_IN.SetData(controlPoints.ToArray());

        int kernelIndex = BoundingBoxComputeShader.FindKernel("CSMain");
        // ��ComputeBuffer���õ�shader��
        BoundingBoxComputeShader.SetBuffer(kernelIndex, "Input", BoundingBoxComputeBuffer_IN);
        BoundingBoxComputeShader.SetBuffer(kernelIndex, "Result", BoundingBoxComputeBuffer_OUT);

        //TODO:numberOfCurves Ӧ�����������

        // ִ��Compute Shader
        BoundingBoxComputeShader.Dispatch(kernelIndex, numberOfCurves, 1, 1);

        float3[] boundingBoxVertices = new float3[numberOfCurves * numberOfOutputPoints];
        //��ȡ���
        BoundingBoxComputeBuffer_OUT.GetData(boundingBoxVertices);


        // ��ʹ����ComputeBuffer�󣬼ǵ��ͷ���Դ
        BoundingBoxComputeBuffer_IN.Release();
        BoundingBoxComputeBuffer_OUT.Release();

        List<int> indices = new List<int>() { 0, 1, 2, 3, 4, 5}; //indices ��Զ�Ǵ��㿪ʼ�����У��ͷ���ѭ��������

        //��������Object
        for (int curveIndex = 0; curveIndex < numberOfCurves; curveIndex++)
        {
            GameObject curve = new GameObject("curve[" + curveIndex + "]");

            curve.AddComponent<MeshFilter>();
            curve.AddComponent<MeshRenderer>();

            Vector3[] corners = new Vector3[numberOfOutputPoints]; //boundingBoxVerticesת��Vector����
            for (int j = 0; j < numberOfOutputPoints; j++)
            {
                corners[j] = transform.TransformPoint(new Vector3(boundingBoxVertices[curveIndex * numberOfOutputPoints + j].x,
                    boundingBoxVertices[curveIndex * numberOfOutputPoints + j].y,
                    boundingBoxVertices[curveIndex * numberOfOutputPoints + j].z));
            }

            Mesh mesh = new Mesh();
            mesh.vertices = corners;
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);

            curve.GetComponent<MeshFilter>().mesh = mesh;
            curve.GetComponent<MeshRenderer>().material = material;

            curve.transform.parent = transform;
            gameObjects.Add(curve);
        }
        //����һ���ڲ�����������ɵ�mesh
        GameObject internalArea = new GameObject("internalArea");

        internalArea.AddComponent<MeshFilter>();
        internalArea.AddComponent<MeshRenderer>();

        Vector3[] internalAreaCorners = new Vector3[controlPoints.Count]; 
        List<int> indicesOfInternalArea = new List<int>() {0,3,6,6,9,0};
        for (int j = 0; j < controlPoints.Count; j++)
        {
            internalAreaCorners[j] = transform.TransformPoint(new Vector3(controlPoints[j].x, controlPoints[j].y, controlPoints[j].z));
            
        }

        Mesh meshOfInternalArea = new Mesh();
        
        meshOfInternalArea.vertices = internalAreaCorners;
        meshOfInternalArea.SetIndices(indicesOfInternalArea, MeshTopology.Triangles, 0);

        internalArea.GetComponent<MeshFilter>().mesh = meshOfInternalArea;
        internalArea.GetComponent<MeshRenderer>().material = material;
        internalArea.GetComponent<MeshRenderer>().material.SetFloat("_fillAll", 1);
        internalArea.transform.parent = transform;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2[] vec = new Vector2[4];

        for (int curveIndex = 0; curveIndex < gameObjects.Count; curveIndex++)
        {
            for (int j = 0; j < 4; j++)
            {

                vec[j] = Camera.main.WorldToScreenPoint(transform.TransformPoint(controlPoints[curveIndex * 3 + j]));

            }
            gameObjects[curveIndex].GetComponent<MeshRenderer>().material.SetVector("_start", new Vector2(vec[0].x, vec[0].y));
            gameObjects[curveIndex].GetComponent<MeshRenderer>().material.SetVector("_control1", new Vector2(vec[1].x, vec[1].y));
            gameObjects[curveIndex].GetComponent<MeshRenderer>().material.SetVector("_control2", new Vector2(vec[2].x, vec[2].y));
            gameObjects[curveIndex].GetComponent<MeshRenderer>().material.SetVector("_end", new Vector2(vec[3].x, vec[3].y));
        }
    }
}
