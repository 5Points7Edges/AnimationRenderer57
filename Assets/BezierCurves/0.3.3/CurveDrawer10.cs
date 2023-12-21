using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CurveDrawer10 : MonoBehaviour
{
    public List<SubPath10> allPointsInitial = new List<SubPath10>();
    public List<SubPath10> allPointsAnimationInitialized = new List<SubPath10>();
    
    public List<SubPath10> allPointsEnd = new List<SubPath10>();
    
    public List<SubPath10> allPoints = new List<SubPath10>();
    
    public List<float> lengthOfCurves = new List<float>();
    public Material material;
    public Material visualM;
    
    private ComputeBuffer curveDataBuffer;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    public bool showDebug=true;
    
    private GameObject shape;
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
        //Debug.Log(allPointsInitial[0].controlPoints.Count());
        
        animationInitialization();
        
        //Debug.Log(allPointsInitial[0].controlPoints.Count());
        //Debug.Log(allPointsEnd[0].controlPoints.Count());
        
        for (int i = 0; i < allPointsInitial.Count(); i++)
        {
            allPoints.Add(new SubPath10(allPointsInitial[i].orientation,allPointsInitial[i].controlPoints));
        }
        
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
                vertices.Add(point);
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
                ShaderinputDataWrapper.orientationMainTri = GetDirection(allPoints[i].controlPoints[0],ShaderinputDataWrapper.start,ShaderinputDataWrapper.end);
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
        
        //Debug
        mesh.SetIndices(indices, MeshTopology.LineStrip, 0);
        shape = new GameObject("shapeDebug");
        shape.AddComponent<MeshFilter>();
        shape.AddComponent<MeshRenderer>();
        shape.GetComponent<MeshRenderer>().material = visualM;
        shape.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.red);
        shape.transform.parent = transform;
        shape.transform.position+=new Vector3(0, 0, (float)-0.05);
        //Debug End
        
        //Debug.Log(allPointsEnd[0].controlPoints.Count);
        
    }

    struct segment
    {
        private Vector3 start;
        private Vector3 control1;
        private Vector3 control2;
        private Vector3 end;
        private float length;
    }
    class DescendingComparer<T> : IComparer<T> where T : IComparable<T> {
        public int Compare(T x, T y) {
            return y.CompareTo(x);
        }
    }
    public void animationInitialization()
    {

    
        for (int contourIndex = 0; contourIndex < allPointsInitial.Count; contourIndex++)
        {
            SortedDictionary<float,int> lengthList = new SortedDictionary<float,int>(new DescendingComparer<float>());
            
            SubPath10 contourInitial = allPointsInitial[contourIndex];
            
            SubPath10 contourFinal = allPointsEnd[contourIndex];
            
            
            //calculate length of each segment in the initial shape
            for (int j = 0; j < contourInitial.controlPoints.Count/ 3; j++)
            {
                Vector3 start = contourInitial.controlPoints[j * 3];
                Vector3 control1 = contourInitial.controlPoints[j * 3+1];
                Vector3 control2 = contourInitial.controlPoints[j * 3+2];
                Vector3 end = contourInitial.controlPoints[j * 3+3];

                float length = curveLengthCalculation(start,control1,control2,end);
                lengthList.Add(length,j);
            }

            float contourLengthInitial = 0;
            foreach (KeyValuePair<float, int> length in lengthList)
            {
                contourLengthInitial+=length.Key;
                Debug.Log(length.Key+" "+length.Value);
            }
            
            int initalCurveCount=contourInitial.controlPoints.Count/3;
            int endCurveCount=contourFinal.controlPoints.Count/3;
        
            int PointsNeedToAdd = endCurveCount - initalCurveCount;
            Debug.Log(PointsNeedToAdd);
            
            float targetLengthOfeachSegment=contourLengthInitial / endCurveCount;

            while (PointsNeedToAdd > 0)
            {
                Debug.Log("-------------------------------------------"+PointsNeedToAdd);
                for (int i = 0; i < contourInitial.controlPoints.Count; i++)
                {
                    Debug.Log(contourInitial.controlPoints[i]+""+i);
                }
                
                int id=lengthList.Values.First();
                Vector3 start = contourInitial.controlPoints[id * 3];
                Vector3 control1 = contourInitial.controlPoints[id * 3+1];
                Vector3 control2 = contourInitial.controlPoints[id * 3+2];
                Vector3 end = contourInitial.controlPoints[id * 3+3];
                
                contourInitial.controlPoints.RemoveRange(id*3,3);
                lengthList.Remove(lengthList.Keys.First());
                
                Debug.Log("-------------------------------------------"+PointsNeedToAdd);
                for (int i = 0; i < contourInitial.controlPoints.Count; i++)
                {
                    Debug.Log(contourInitial.controlPoints[i]+""+i);
                }
                var splittedCurve = splitCurve(start, control1, control2, end,0.5f);
                contourInitial.controlPoints.Insert(id*3,splittedCurve.Item8);
                contourInitial.controlPoints.Insert(id*3,splittedCurve.Item7);
                contourInitial.controlPoints.Insert(id*3,splittedCurve.Item6);
                //contourInitial.controlPoints.Insert(id,splittedCurve.Item5);
                
                contourInitial.controlPoints.Insert(id*3,splittedCurve.Item4);
                contourInitial.controlPoints.Insert(id*3,splittedCurve.Item3);
                contourInitial.controlPoints.Insert(id*3,splittedCurve.Item2);
                //contourInitial.controlPoints.Insert(id,splittedCurve.Item1);
                
                Debug.Log("-------------------------------------------"+PointsNeedToAdd);
                for (int i = 0; i < contourInitial.controlPoints.Count; i++)
                {
                    Debug.Log(contourInitial.controlPoints[i]+""+i);
                }
                
                var tmp= lengthList.ToDictionary(d => d.Key , d=> d.Value> id ? d.Value : d.Value +1);
                lengthList = new SortedDictionary<float, int>(tmp,new DescendingComparer<float>());
                
                lengthList.Add(curveLengthCalculation(splittedCurve.Item1,splittedCurve.Item2,splittedCurve.Item3,splittedCurve.Item4),id);
                lengthList.Add(curveLengthCalculation(splittedCurve.Item5,splittedCurve.Item6,splittedCurve.Item7,splittedCurve.Item8),id+1);
                
                PointsNeedToAdd--;
            }
            foreach (KeyValuePair<float, int> length in lengthList)
            {
                Debug.Log(length.Key+" "+length.Value);
            }
        }
        //Debug.Log(splitCurve(new Vector3(0,0,0),new Vector3(0,1,0),new Vector3(1,1,0),new Vector3(1,0,0),(float)0.5));
    }

    public (Vector3,Vector3,Vector3,Vector3,Vector3,Vector3,Vector3,Vector3) splitCurve(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,float t)
    {
        Vector3 p01 = (p1 - p0) * t + p0;
        Vector3 p12 = (p2 - p1) * t + p1;
        Vector3 p23 = (p3 - p2) * t + p2;
        Vector3 p012 = (p12 - p01) * t + p01;
        Vector3 p123 = (p23 - p12) * t + p12;
        Vector3 p0123 = (p123 - p012) * t + p012;

        return (p0, p01, p012, p0123, p0123, p123,p23, p3);
    }
    public float curveLengthCalculation(Vector3 start,Vector3 c1,Vector3 c2,Vector3 end)
    {
        float totalLength = 0;
        int approximation = 2;
        float unit = (float)1 / approximation;
        for (int i = 0; i < approximation; i++)
        {
            Vector3 point0=BezierCubic(start, c1, c2, end, unit * i);
            Vector3 point1=BezierCubic(start, c1, c2, end, unit * (i+1));
            totalLength+=Vector3.Distance(point0,point1);
        }
        return totalLength;
    }
    //(1-t)^3*p0 + 3t(1-t)^2*p1 + 3t^2*(1-t)*p2 + t^3*p3
    public Vector3 BezierCubic(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        Vector3 result = (float)Math.Pow((1 - t),3)*p0 + (float)Math.Pow((1 - t),2)*t*3*p1 + 3*t*t*(1-t)*p2+(float)Math.Pow(t,3)*p3;
        return result;
    }
    public List<Vector3> computeBoundingBox(SubPath10 subpath)
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
            
            boundingBoxVertices.Add(subpath.controlPoints[0]);
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
        
        List<CurveDataWrapper> shaderinputDataWrappers = new List<CurveDataWrapper>();
        
        for (int i = 0; i < allPoints.Count; i++)
        {
            List<Vector3> controlPointsSubPath=allPoints[i].controlPoints;
            
            List<Vector3> boundingBoxVerticesInTriangleStrip = computeBoundingBox(allPoints[i]);
            foreach (Vector3 point in boundingBoxVerticesInTriangleStrip)
            {
                vertices.Add(point);
            }
            for (int j = 0; j < controlPointsSubPath.Count / 3; j++)
            {
                CurveDataWrapper shaderinputDataWrapper = new CurveDataWrapper();

                shaderinputDataWrapper.start = controlPointsSubPath[j * 3];
                shaderinputDataWrapper.control1 = controlPointsSubPath[j * 3+1];
                shaderinputDataWrapper.control2 = controlPointsSubPath[j * 3+2];
                shaderinputDataWrapper.end = controlPointsSubPath[j * 3+3];
                
                shaderinputDataWrapper.orientation1 = GetDirection(shaderinputDataWrapper.start, shaderinputDataWrapper.control1, shaderinputDataWrapper.control2);
                shaderinputDataWrapper.orientation2 = GetDirection(shaderinputDataWrapper.start, shaderinputDataWrapper.control2, shaderinputDataWrapper.end);
                shaderinputDataWrapper.orientationMainTri = GetDirection(allPoints[i].controlPoints[0],shaderinputDataWrapper.start,shaderinputDataWrapper.end);
                shaderinputDataWrappers.Add(shaderinputDataWrapper);
            }
        }
        
        for (int i = 0; i < vertices.Count; i++)
        {
            indices.Add(i);
        }
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        curveDataBuffer.SetData(shaderinputDataWrappers.ToArray());
        meshFilter.mesh = mesh;

        if (showDebug)
        {
            Mesh mesh1 = new Mesh();
            mesh1.vertices = vertices.ToArray();
            mesh1.SetIndices(indices, MeshTopology.LineStrip, 0);
            shape.GetComponent<MeshFilter>().mesh = mesh1;
            shape.transform.position=transform.position+new Vector3(0, 0, (float)-0.05);
        }
        else
        {
            Mesh mesh1 = new Mesh();
            shape.GetComponent<MeshFilter>().mesh = mesh1;
        }
    }
    private void OnDestroy()
    {
        curveDataBuffer.Release();
    }
}