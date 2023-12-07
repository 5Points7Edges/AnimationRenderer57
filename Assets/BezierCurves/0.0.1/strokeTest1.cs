/*
 * Author:Thomas Lu
 * v0.0.1
 * A single pixel curve defined by two handle and two anchor
 * The color is customizable
 */


using UnityEngine;
[RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
public class lineTest : MonoBehaviour
{
    public Vector3 startPoint = new Vector3(0, -5, 0);
    public Vector3 controlPoint1 = new Vector3(0, 0, 3);
    public Vector3 controlPoint2 = new Vector3(0, 0, -3);
    public Vector3 endPoint = new Vector3(0, 5, 0);
    // Start is called before the first frame update
    public static Vector3 GetBezierPoint(float t, Vector3 start, Vector3 center1, Vector3 center2, Vector3 end)
    {
        return (1 - t) * (1 - t) * (1 - t) * start + 3 * t * (1 - t) * (1 - t) * center1 + 3 * t * t * (1 - t) * center2 + t * t * t * end;
    }
    void Start()
    {
        int segements = 100;
        Vector3[] vertices= new Vector3[105];
        vertices[0] = startPoint;
        vertices[1] = controlPoint1;
        vertices[2] = controlPoint2;
        vertices[3] = endPoint;
        for (int i = 0;i <= segements; i++)
        {
            vertices[i]=GetBezierPoint((float)i / (float)segements, startPoint, controlPoint1, controlPoint2, endPoint);
        }
        int[] indices = new int[segements+1] ;
        for(int i = 0; i <= segements; i++)
        {
            indices[i] = i;
        }
        Mesh mesh= new Mesh();
        mesh.vertices = vertices;
        mesh.SetIndices(indices, MeshTopology.LineStrip, 0);
        MeshFilter meshFilter= GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;
    }

    
}
