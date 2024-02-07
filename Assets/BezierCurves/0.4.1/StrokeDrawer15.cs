using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class StrokeDrawer15 : MonoBehaviour
{
    // Start is called before the first frame update
    public Material material;
    
    public Path15 pathInitial = new Path15();
    public Path15 pathEnd = new Path15();
    
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
    public static List<Vector3> ComputeBoundingBox(SubPath15 subPath)
    {
        float width = 0.1f;
        List<Segment15> points = subPath.segments;
        List<Vector3> boundingBoxVertices = new List<Vector3>();

        for(int i = 0; i < points.Count; i++)
        {
            Vector3 abVec = (points[i].p1 - points[i].p0).normalized*width;
            Vector3 bcVec = (points[i].p2 - points[i].p1).normalized*width;
            Vector3 cdVec = (points[i].p3 - points[i].p2).normalized*width;
            Vector3 c0Norm = new Vector3(abVec.y, -abVec.x, abVec.z).normalized*width;
            Vector3 c3Norm = new Vector3(cdVec.y, -cdVec.x, cdVec.z).normalized*width;

            Vector3 pp1 = points[i].p0 + c0Norm - abVec;
            Vector3 pp2 = points[i].p0 - c0Norm - abVec;
            Vector3 pp3 = points[i].p1 + c0Norm ;
            Vector3 pp4 = points[i].p1 - c0Norm ;
            Vector3 pp5 = points[i].p2 + c3Norm;
            Vector3 pp6 = points[i].p2 - c3Norm;
            Vector3 pp7 = points[i].p3 + c3Norm;
            Vector3 pp8 = points[i].p3 - c3Norm;
            
            boundingBoxVertices.AddRange(new List<Vector3>(){pp1,pp2,pp3});
            boundingBoxVertices.AddRange(new List<Vector3>(){pp1,pp2,pp4});
            
            boundingBoxVertices.AddRange(new List<Vector3>(){pp3,pp2,pp5});
            boundingBoxVertices.AddRange(new List<Vector3>(){pp5,pp2,pp8});
            
            boundingBoxVertices.AddRange(new List<Vector3>(){pp3,pp2,pp7});
            boundingBoxVertices.AddRange(new List<Vector3>(){pp7,pp2,pp6});
            
            boundingBoxVertices.AddRange(new List<Vector3>(){pp1,pp4,pp7});
            boundingBoxVertices.AddRange(new List<Vector3>(){pp7,pp4,pp6});
            
            boundingBoxVertices.AddRange(new List<Vector3>(){pp1,pp4,pp5});
            boundingBoxVertices.AddRange(new List<Vector3>(){pp5,pp4,pp8});
            
            boundingBoxVertices.AddRange(new List<Vector3>(){pp5,pp8,pp7});
            boundingBoxVertices.AddRange(new List<Vector3>(){pp7,pp6,pp8});
            
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
