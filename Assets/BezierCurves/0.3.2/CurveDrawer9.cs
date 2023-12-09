using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CurveDrawer9 : MonoBehaviour
{
    public List<SubPath9> allPointsInitial = new List<SubPath9>();
    public List<SubPath9> allPointsEnd = new List<SubPath9>();
    public List<SubPath9> allPoints = new List<SubPath9>();
    
    public Material material;

    
    private ComputeBuffer curveDataBuffer;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    struct CurveDataWrapper
    {
        public Vector3 start;
        public Vector3 control1;
        public Vector3 control2;
        public Vector3 end;
        public int orientation1;
        public int orientation2;
        public int orientationMainTri;
    }

    // Start is called before the first frame update
    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        List<Vector3> vertices= new List<Vector3>();
        List<int> indices= new List<int>();
        
        Console.WriteLine($"Value of d attribute: ");
        if (!material)
        {
            Debug.LogError("Please Assign a material on the inspector");
            return;
        }

        List<CurveDataWrapper> ShaderinputDataWrappers = new List<CurveDataWrapper>();
        
        for (int i = 0; i < allPoints.Count; i++)
        {
            List<Vector3> controlPointsSubPath=allPoints[i].controlPoints;
            
            List<Vector3> boundingBoxVerticesInTriangleStrip = computeBoundingBox(allPoints[i]);
            foreach (Vector3 point in boundingBoxVerticesInTriangleStrip)
            {
                vertices.Add(new Vector3(point.x,point.y,point.z));
            }
            for (int j = 0; j < controlPointsSubPath.Count / 3; j++)
            {
                CurveDataWrapper ShaderinputDataWrapper = new CurveDataWrapper();

                ShaderinputDataWrapper.start = controlPointsSubPath[j * 3];
                ShaderinputDataWrapper.control1 = controlPointsSubPath[j * 3+1];
                ShaderinputDataWrapper.control2 = controlPointsSubPath[j * 3+2];
                ShaderinputDataWrapper.end = controlPointsSubPath[j * 3+3];
                
                ShaderinputDataWrapper.orientation1 = GetDirection(ShaderinputDataWrapper.start, ShaderinputDataWrapper.control1, ShaderinputDataWrapper.control2);
                ShaderinputDataWrapper.orientation2 = GetDirection(ShaderinputDataWrapper.start, ShaderinputDataWrapper.control2, ShaderinputDataWrapper.end);
                ShaderinputDataWrapper.orientationMainTri = GetDirection(allPoints[i].basePoint,ShaderinputDataWrapper.start,ShaderinputDataWrapper.end);
                //Debug.Log(ShaderinputDataWrapper.orientation1);
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
        
        meshFilter.mesh = mesh;
        meshRenderer.material = material;

    }
    public List<Vector3> computeBoundingBox(SubPath9 subpath)
    {
        List<Vector3> Points = subpath.controlPoints;
        List<Vector3> boundingBoxVertices = new List<Vector3>();

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
    
    //n = u(x1, y1, z1) x v(x2, y2, z2) = (y1z2 - y2z1, x2z1-z2x1, x1y2 -x2y1)
    public int GetDirection(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Vector3 edge1 = p2 - p1;
        Vector3 edge2 = p3 - p2;

        Vector3 crossProduct = Vector3.Cross(edge1, edge2);
        
        // decide which side the triangle points at
        return crossProduct.z > 0 ? 1 : -1;
    }

    // Update is called once per frame
    void Update()
    {
        List<Vector3> vertices= new List<Vector3>();
        List<int> indices= new List<int>();
        
        List<CurveDataWrapper> ShaderinputDataWrappers = new List<CurveDataWrapper>();
        
        for (int i = 0; i < allPoints.Count; i++)
        {
            List<Vector3> controlPointsSubPath=allPoints[i].controlPoints;
            
            List<Vector3> boundingBoxVerticesInTriangleStrip = computeBoundingBox(allPoints[i]);
            foreach (Vector3 point in boundingBoxVerticesInTriangleStrip)
            {
                vertices.Add(new Vector3(point.x,point.y,point.z));
            }
            for (int j = 0; j < controlPointsSubPath.Count / 3; j++)
            {
                CurveDataWrapper ShaderinputDataWrapper = new CurveDataWrapper();

                ShaderinputDataWrapper.start = controlPointsSubPath[j * 3];
                ShaderinputDataWrapper.control1 = controlPointsSubPath[j * 3+1];
                ShaderinputDataWrapper.control2 = controlPointsSubPath[j * 3+2];
                ShaderinputDataWrapper.end = controlPointsSubPath[j * 3+3];
                
                ShaderinputDataWrapper.orientation1 = GetDirection(ShaderinputDataWrapper.start, ShaderinputDataWrapper.control1, ShaderinputDataWrapper.control2);
                ShaderinputDataWrapper.orientation2 = GetDirection(ShaderinputDataWrapper.start, ShaderinputDataWrapper.control2, ShaderinputDataWrapper.end);
                ShaderinputDataWrapper.orientationMainTri = GetDirection(allPoints[i].basePoint,ShaderinputDataWrapper.start,ShaderinputDataWrapper.end);
                ShaderinputDataWrappers.Add(ShaderinputDataWrapper);
            }
        }
        
        for (int i = 0; i < vertices.Count; i++)
        {
            indices.Add(i);
        }
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        curveDataBuffer.SetData(ShaderinputDataWrappers.ToArray());
        meshFilter.mesh = mesh;
        
    }
    private void OnDestroy()
    {
        curveDataBuffer.Release();
    }
}
