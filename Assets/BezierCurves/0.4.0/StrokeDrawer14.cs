using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrokeDrawer14 : MonoBehaviour
{
    // Start is called before the first frame update
    public Material material;
    
    public Path14 pathInitial = new Path14();
    public Path14 pathEnd = new Path14();
    
    public ComputeBuffer curveDataBufferSource;
    public ComputeBuffer curveDataBufferTarget;
    private ComputeBuffer verticesBufferTarget;
    
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    
    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        
        material.SetBuffer("curvess_buffer", curveDataBufferSource);
        material.SetBuffer("curvest_buffer", curveDataBufferTarget);
        
        
        List<int> indices= new List<int>();

        List<Vector3> verticesSource= new List<Vector3>();
        List<Vector3> verticesTarget= new List<Vector3>();
        
        // store bounding box into verticesSource
        for (int i = 0; i < pathInitial.subPaths.Count; i++)
        {
            List<Vector3> boundingBoxVerticesInTriangleStrip = ComputeBoundingBox(pathInitial.subPaths[i]);
            foreach (Vector3 point in boundingBoxVerticesInTriangleStrip)
            {
                verticesSource.Add(point);
            }
            
        }
        // store bounding box into verticesTarget
        for (int i = 0; i < pathEnd.subPaths.Count; i++)
        {
            List<Vector3> boundingBoxVerticesInTriangleStrip = ComputeBoundingBox(pathEnd.subPaths[i]);
            foreach (Vector3 point in boundingBoxVerticesInTriangleStrip)
            {
                verticesTarget.Add(point);
            }
        }
        // create mesh, transfer verticesSource into shader
        Mesh mesh = new Mesh();
        mesh.vertices = verticesSource.ToArray();
        for (int i = 0; i < verticesSource.Count; i++)
        {
            indices.Add(i);
        }
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        
        // transfer verticesTarget into shader

        int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector3));
        verticesBufferTarget = new ComputeBuffer(verticesTarget.Count, stride);
        verticesBufferTarget.SetData(verticesTarget.ToArray());
        material.SetBuffer("StrokeVerticesTarget", verticesBufferTarget);
        
        //Finalization: set mesh and material
        meshFilter.mesh = mesh;
        meshRenderer.material = material;
    }
    public static List<Vector3> ComputeBoundingBox(SubPath14 subPath)
    {
        float width = 0.5f;
        List<Segment14> points = subPath.segments;
        List<Vector3> boundingBoxVertices = new List<Vector3>();

        for(int i = 0; i < points.Count; i++)
        {
            Vector3 abVec = points[i].p1 - points[i].p0;
            Vector3 cdVec = points[i].p3 - points[i].p2;
            Vector3 c0Norm = new Vector3(abVec.y, -abVec.x, abVec.z).normalized*width;
            Vector3 c3Norm = new Vector3(cdVec.y, -cdVec.x, cdVec.z).normalized*width;
            
            boundingBoxVertices.Add(points[i].p0+c0Norm);
            boundingBoxVertices.Add(points[i].p0-c0Norm);
            boundingBoxVertices.Add(points[i].p1);
            
            boundingBoxVertices.Add(points[i].p0-c0Norm);
            boundingBoxVertices.Add(points[i].p1);
            boundingBoxVertices.Add(points[i].p2);
            
            boundingBoxVertices.Add(points[i].p0-c0Norm);
            boundingBoxVertices.Add(points[i].p2);
            boundingBoxVertices.Add(points[i].p3-c3Norm);
            
            boundingBoxVertices.Add(points[i].p3-c3Norm);
            boundingBoxVertices.Add(points[i].p2);
            boundingBoxVertices.Add(points[i].p3+c3Norm);
        }
        return boundingBoxVertices;
    }
    private void OnDestroy()
    {
        curveDataBufferSource.Release();
        curveDataBufferTarget.Release();
        verticesBufferTarget.Release();
    }
}
