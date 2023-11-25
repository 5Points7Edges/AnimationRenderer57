using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class BezierStroke : MonoBehaviour
{
    public Vector3 startPoint = new Vector3(0, -5, 0);
    public Vector3 controlPoint1 = new Vector3(0, 0, 3);
    public Vector3 controlPoint2 = new Vector3(0, 0, -3);
    public Vector3 endPoint = new Vector3(0, 5, 0);

    Vector3[] vertices = new Vector3[4];

    Vector2[] vec = new Vector2[4];

    public Material material;


    // Start is called before the first frame update
    void Start()
    {
        //material = Resources.Load("Test2_Mat", typeof(Material)) as Material;

        vertices[0] = startPoint;
        vertices[1] = controlPoint1;
        vertices[2] = controlPoint2;
        vertices[3] = endPoint;

        int[] indices = new int[24] { 0, 1, 2, 2, 1, 0, 1, 2, 3, 3, 2, 1, 2, 3, 0, 0, 3, 2, 3, 0, 1, 1, 0, 3 };

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;
    }

    // Update is called once per frame
    void Update()
    {
        
        for(int i = 0; i < 4; i++)
        {
            vec[i] = Camera.main.WorldToScreenPoint(transform.TransformPoint(vertices[i]));
            
        }
        
        material.SetVector("_start", new Vector2(vec[0].x, vec[0].y));
        material.SetVector("_control1", new Vector2(vec[1].x, vec[1].y));
        material.SetVector("_control2", new Vector2(vec[2].x, vec[2].y));
        material.SetVector("_end", new Vector2(vec[3].x, vec[3].y));

        //Debug.Log(dist);
        //Debug.Log(vec[0]);
        //Debug.Log(vec[1]);
        //Debug.Log(vec[2]);
        //Debug.Log(vec[3]);
    }
}
