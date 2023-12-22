using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CurveDrawer12 : MonoBehaviour
{
    public Path12 pathInitial = new Path12();
    
    public Path12 pathEnd = new Path12();
    
    public Path12 pathCurrent = new Path12();
    
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
    public struct lengthPair
    {
        public float length;
        public int index;
    }
    public sealed class lengthPairComparer : IComparer<lengthPair> 
    {
        public int Compare(lengthPair x, lengthPair y)
        {
            if (x.length < y.length) return 1;
            if (x.length > y.length) return -1;
            return 0;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        //Debug.Log(allPointsInitial[0].controlPoints.Count());
        
        insertExtraPoints();
        findBestReihenfolge();
        
        //Debug.Log(allPointsInitial[0].controlPoints.Count());
        //Debug.Log(allPointsEnd[0].controlPoints.Count());
        
        for (int i = 0; i < pathInitial.subPaths.Count(); i++)
        {
            SubPath12 tmp = new SubPath12();
            for (int j = 0; j < pathInitial.subPaths[i].segments.Count();j++)
            {
                tmp.Add(new Segment12(pathInitial.subPaths[i].segments[j]));    
            }
            pathCurrent.Add(tmp);
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
        
        for (int i = 0; i < pathCurrent.subPaths.Count; i++)
        {
            SubPath12 controlPointsSubPath=pathCurrent.subPaths[i];
            
            List<Vector3> boundingBoxVerticesInTriangleStrip = ComputeBoundingBox(pathCurrent.subPaths[i]);
            foreach (Vector3 point in boundingBoxVerticesInTriangleStrip)
            {
                vertices.Add(point);
            }
            for (int j = 0; j < controlPointsSubPath.segments.Count; j++)
            {
                CurveDataWrapper ShaderinputDataWrapper = new CurveDataWrapper();

                ShaderinputDataWrapper.start = controlPointsSubPath.segments[j].p0;
                ShaderinputDataWrapper.control1 = controlPointsSubPath.segments[j].p1;
                ShaderinputDataWrapper.control2 = controlPointsSubPath.segments[j].p2;
                ShaderinputDataWrapper.end = controlPointsSubPath.segments[j].p3;
                
                ShaderinputDataWrapper.orientation1 = GetDirection(ShaderinputDataWrapper.start, ShaderinputDataWrapper.control1, ShaderinputDataWrapper.control2);
                ShaderinputDataWrapper.orientation2 = GetDirection(ShaderinputDataWrapper.start, ShaderinputDataWrapper.control2, ShaderinputDataWrapper.end);
                ShaderinputDataWrapper.orientationMainTri = GetDirection(pathCurrent.subPaths[i].GetBasePoint(),ShaderinputDataWrapper.start,ShaderinputDataWrapper.end);
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
        
    }

    public void findBestReihenfolge()
    {
        for (int contourIndex = 0; contourIndex < pathInitial.subPaths.Count; contourIndex++)
        {
            SubPath12 contourInitial = pathInitial.subPaths[contourIndex];
            SubPath12 contourFinal = pathEnd.subPaths[contourIndex];
            int bestStartID = 0;
            float minDistance = int.MaxValue;
            minDistance = 0;
            for (int i = 0; i < contourInitial.segments.Count(); i++)
            {
                float currentTotalDistance = calculateTotalDistance(i,contourInitial,contourFinal);
                
                Debug.Log(currentTotalDistance+" "+i);
                if (currentTotalDistance> minDistance)
                {
                    minDistance = currentTotalDistance;
                    bestStartID = i;
                }
            }
            Debug.Log(bestStartID);
            //bestStartID = 6;
            int count = contourInitial.segments.Count();
            //Debug.Log(contourInitial);
            
            contourInitial.segments = contourInitial.segments.GetRange(bestStartID, count - bestStartID).Concat(
                contourInitial.segments.GetRange(0, bestStartID)).ToList<Segment12>();
        }
    }

    public float calculateTotalDistance(int startID,SubPath12 contourInitial,SubPath12 contourEnd)
    {
        List<float> distanceList = new List<float>();
        float sum = 0;
        for (int i = 0; i < contourInitial.segments.Count(); i++)
        {
            float distance= Vector3.Distance(contourInitial.segments[i].p0, contourEnd.segments[(i+startID)%contourInitial.segments.Count()].p0);
            sum += distance;
            distanceList.Add(distance);
            distance= Vector3.Distance(contourInitial.segments[i].p1, contourEnd.segments[(i+startID)%contourInitial.segments.Count()].p1);
            sum += distance;
            distanceList.Add(distance);
            distance= Vector3.Distance(contourInitial.segments[i].p2, contourEnd.segments[(i+startID)%contourInitial.segments.Count()].p2);
            sum += distance;
            distanceList.Add(distance);
        }

        float average = sum / distanceList.Count/3;

        float deviation = 0;
        for (int i = 0; i < distanceList.Count; i++)
        {
            deviation += distanceList[i] * distanceList[i];
        }
        
        return deviation;
    }
    public void insertExtraPoints()
    {
        
        for (int contourIndex = 0; contourIndex < pathInitial.subPaths.Count; contourIndex++)
        {
            List<lengthPair> lengthList = new List<lengthPair>();
            
            SubPath12 contourInitial = pathInitial.subPaths[contourIndex];
            
            SubPath12 contourFinal = pathEnd.subPaths[contourIndex];

            if (contourInitial.segments.Count() > contourFinal.segments.Count())
            {
                (contourInitial, contourFinal) = (contourFinal, contourInitial);
            }
            //calculate length of each segment in the initial shape
            float contourLengthInitial = 0;
            for (int j = 0; j < contourInitial.segments.Count(); j++)
            {
                float length = contourInitial.segments[j].GetLength();
                contourLengthInitial += length;
                lengthList.Add(new lengthPair{length = length,index = j});
            }
            
            int initalCurveCount=contourInitial.segments.Count;
            int endCurveCount=contourFinal.segments.Count;
        
            int PointsNeedToAdd = endCurveCount - initalCurveCount;
           
            while (PointsNeedToAdd > 0)
            {
                //Debug.Log("--------------------------"+PointsNeedToAdd);
                //for(int  i=0;i<contourInitial.segments.Count();i++)Debug.Log(contourInitial.segments[i]);
                lengthList.Sort(new lengthPairComparer());
                int id=lengthList[0].index;
                
                var splittedCurve = contourInitial.segments[id].SplitCurve(0.46f);
                //Debug.Log(splittedCurve);
                
                contourInitial.segments.RemoveAt(id);
                lengthList.Remove(lengthList[0]);
                
                //Debug.Log("--------------------------"+PointsNeedToAdd);
                //for(int  i=0;i<contourInitial.segments.Count();i++)Debug.Log(contourInitial.segments[i]);
                
                Segment12 firstCurve = new Segment12();
                firstCurve.p0 = splittedCurve.Item1;
                firstCurve.p1 = splittedCurve.Item2;
                firstCurve.p2 = splittedCurve.Item3;
                firstCurve.p3 = splittedCurve.Item4;
                Segment12 secondCurve = new Segment12();
                secondCurve.p0 = splittedCurve.Item5;
                secondCurve.p1 = splittedCurve.Item6;
                secondCurve.p2 = splittedCurve.Item7;
                secondCurve.p3 = splittedCurve.Item8;
                contourInitial.segments.Insert(id,secondCurve);
                contourInitial.segments.Insert(id,firstCurve);
                
                
                for(int k=0;k<lengthList.Count;k++)
                {
                    lengthPair tmp = lengthList[k];
                    tmp.index = tmp.index > id ? id + 1 : id;
                    lengthList[k] = tmp;
                }
                
                lengthList.Add(new lengthPair{length = firstCurve.GetLength(),index = id});
                lengthList.Add(new lengthPair{length = secondCurve.GetLength(),index = id+1});
                
                //Debug.Log("--------------------------"+PointsNeedToAdd);
                //for(int  i=0;i<contourInitial.segments.Count();i++)Debug.Log(contourInitial.segments[i]);
                
                PointsNeedToAdd--;
            }
            
        }
    }

    public static List<Vector3> ComputeBoundingBox(SubPath12 subPath)
    {
        List<Segment12> points = subPath.segments;
        List<Vector3> boundingBoxVertices = new List<Vector3>();

        for(int i = 0; i < points.Count; i++)
        {
            
            boundingBoxVertices.Add(points[i].p0);
            boundingBoxVertices.Add(points[i].p1);
            boundingBoxVertices.Add(points[i].p2);
            
            boundingBoxVertices.Add(points[i].p0);
            boundingBoxVertices.Add(points[i].p2);
            boundingBoxVertices.Add(points[i].p3);
            
            boundingBoxVertices.Add(subPath.GetBasePoint());
            boundingBoxVertices.Add(points[i].p0);
            boundingBoxVertices.Add(points[i].p3);
        }
        return boundingBoxVertices;
    }

    public static bool isOnBothSide(Segment12 segment)
    {
        if (GetDirection(segment.p0, segment.p3, segment.p1) !=
            GetDirection(segment.p0, segment.p3, segment.p2)) return true;
        return false;
    }
    //n = u(x1, y1, z1) x v(x2, y2, z2) = (y1z2 - y2z1, x2z1-z2x1, x1y2 -x2y1)
    public static int GetDirection(Vector3 p1, Vector3 p2, Vector3 p3)
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
        
        List<CurveDataWrapper> shaderInputDataWrappers = new List<CurveDataWrapper>();
        
        for (int i = 0; i < pathCurrent.subPaths.Count(); i++)
        {
            SubPath12 controlPointsSubPath=pathCurrent.subPaths[i];
            
            List<Vector3> boundingBoxVerticesInTriangleStrip = ComputeBoundingBox(pathCurrent.subPaths[i]);
            foreach (Vector3 point in boundingBoxVerticesInTriangleStrip)
            {
                vertices.Add(point);
            }
            
            for (int j = 0; j < controlPointsSubPath.segments.Count; j++)
            {
                CurveDataWrapper shaderInputDataWrapper = new CurveDataWrapper();

                shaderInputDataWrapper.start = controlPointsSubPath.segments[j].p0;
                shaderInputDataWrapper.control1 = controlPointsSubPath.segments[j].p1;
                shaderInputDataWrapper.control2 = controlPointsSubPath.segments[j].p2;
                shaderInputDataWrapper.end = controlPointsSubPath.segments[j].p3;
                
                shaderInputDataWrapper.orientation1 = GetDirection(shaderInputDataWrapper.start, shaderInputDataWrapper.control1, shaderInputDataWrapper.control2);
                shaderInputDataWrapper.orientation2 = GetDirection(shaderInputDataWrapper.start, shaderInputDataWrapper.control2, shaderInputDataWrapper.end);
                shaderInputDataWrapper.orientationMainTri = GetDirection(pathCurrent.subPaths[i].GetBasePoint(),shaderInputDataWrapper.start,shaderInputDataWrapper.end);
                //Debug.Log(ShaderInputDataWrapper.orientation1);
                shaderInputDataWrappers.Add(shaderInputDataWrapper);
            }
        }
        
        for (int i = 0; i < vertices.Count; i++)
        {
            indices.Add(i);
        }
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        curveDataBuffer.SetData(shaderInputDataWrappers.ToArray());
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

