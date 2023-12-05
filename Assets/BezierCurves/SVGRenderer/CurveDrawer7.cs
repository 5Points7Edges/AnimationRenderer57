using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CurveDrawer7 : MonoBehaviour
{
    
    public List<SubPath> allPoints = new List<SubPath>();
    public List<Vector3> vertices= new List<Vector3>();
    public List<int> indices= new List<int>();
    public Material material;

    
    private ComputeBuffer curveDataBuffer;
    struct CurveDataWrapper
    {
        public Vector4 start;
        public Vector4 control1;
        public Vector4 control2;
        public Vector4 end;
        //public int fillAll;
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
        List<CurveDataWrapper> ShaderinputDataWrappers = new List<CurveDataWrapper>();
        
        for (int i = 0; i < allPoints.Count; i++)
        {
            List<float3> controlPointsSubPath=allPoints[i].controlPoints;
            
            List<float3> boundingBoxVerticesInTriangleStrip = computeBoundingBox(allPoints[i]);
            foreach (float3 point in boundingBoxVerticesInTriangleStrip)
            {
                vertices.Add(new Vector3(point.x,point.y,point.z));
            }
            for (int j = 0; j < controlPointsSubPath.Count / 3; j++)
            {
                CurveDataWrapper ShaderinputDataWrapper = new CurveDataWrapper();

                ShaderinputDataWrapper.start = new Vector4(controlPointsSubPath[j * 3].x, controlPointsSubPath[j * 3].y,
                    controlPointsSubPath[j * 3].z, 0);
                ShaderinputDataWrapper.control1 = new Vector4(controlPointsSubPath[j * 3+1].x, controlPointsSubPath[j * 3+1].y,
                    controlPointsSubPath[j * 3+1].z, 0);
                ShaderinputDataWrapper.control2 = new Vector4(controlPointsSubPath[j * 3+2].x, controlPointsSubPath[j * 3+2].y,
                    controlPointsSubPath[j * 3+2].z, 0);
                ShaderinputDataWrapper.end = new Vector4(controlPointsSubPath[j * 3+3].x, controlPointsSubPath[j * 3+3].y,
                    controlPointsSubPath[j * 3+3].z, 0);
                Vector3 a = new Vector3(ShaderinputDataWrapper.start.x,ShaderinputDataWrapper.start.y,ShaderinputDataWrapper.start.z);
                Vector3 b = new Vector3(ShaderinputDataWrapper.control1.x,ShaderinputDataWrapper.control1.y,ShaderinputDataWrapper.control1.z);
                Vector3 c = new Vector3(ShaderinputDataWrapper.control2.x,ShaderinputDataWrapper.control2.y,ShaderinputDataWrapper.control2.z);
                ShaderinputDataWrapper.orientation = GetDirection(a,b,c);
                Debug.Log(ShaderinputDataWrapper.orientation);
                ShaderinputDataWrappers.Add(ShaderinputDataWrapper);
            }
        }

        
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        for (int i = 0; i < vertices.Count; i++)
        {
            indices.Add(i);
        }
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);

        int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CurveDataWrapper));
        curveDataBuffer = new ComputeBuffer(ShaderinputDataWrappers.Count, stride);
        curveDataBuffer.SetData(ShaderinputDataWrappers.ToArray());
        material.SetBuffer("curves", curveDataBuffer);
        
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material = material;

    }
    protected List<float3> computeBoundingBox(SubPath subpath)
    {
        List<float3> Points = subpath.controlPoints;
        List<float3> boundingBoxVertices = new List<float3>();

        for(int i = 0; i < Points.Count/3; i++)
        {
            
            boundingBoxVertices.Add(Points[i * 3]);
            boundingBoxVertices.Add(Points[i * 3 + 1]);
            boundingBoxVertices.Add(Points[i * 3 + 2]);
            
            boundingBoxVertices.Add(Points[i * 3]);
            boundingBoxVertices.Add(Points[i * 3 + 2]);
            boundingBoxVertices.Add(Points[i * 3 + 3]);
            
            boundingBoxVertices.Add(subpath.basePoint);
            boundingBoxVertices.Add(Points[i * 3]);
            boundingBoxVertices.Add(Points[i * 3 + 3]);
            
        }

        return boundingBoxVertices;
    }
    
    protected int GetDirection(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Vector3 edge1 = p2 - p1;
        Vector3 edge2 = p3 - p2;
        Vector3 crossProduct = Vector3.Cross(edge1, edge2);

        // decide which side the triangle points at
        if (crossProduct.z > 0)
        {
            return 1;
        }
        else if (crossProduct.z <= 0)
        {
            return -1;
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
