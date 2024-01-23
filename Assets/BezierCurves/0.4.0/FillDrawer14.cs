using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FillDrawer14 : MonoBehaviour
{
    public Path14 pathInitial = new Path14();
    
    public Path14 pathEnd = new Path14();
    
    
    public Material FillMaterial;
    public Material StrokeMaterial;
    
    private ComputeBuffer curveDataBufferSource;
    private ComputeBuffer curveDataBufferTarget;
    private ComputeBuffer verticesBufferTarget;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    public bool showDebug=false;
    
    struct CurveDataWrapper
    {
        public Vector3 start;
        public Vector3 control1;
        public Vector3 control2;
        public Vector3 end;
        public int basePointIndex;
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
        insertExtraPoints();
        findtheBestOder();
        
        //Debug.Log(pathInitial.subPaths.Count);
        //Debug.Log(pathEnd.subPaths.Count);
        
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        List<Vector3> vertices= new List<Vector3>();
        List<int> indices= new List<int>();
        
        Console.WriteLine($"Value of d attribute: ");
        if (!FillMaterial)
        {
            Debug.LogError("Please Assign a material on the inspector");
            return;
        }
        //Prepare data for the initial path 
        List<CurveDataWrapper> ShaderinputDataWrappersSource = new List<CurveDataWrapper>();
        int basePointIndexCounter = 0;
        for (int i = 0; i < pathInitial.subPaths.Count; i++)
        {
            SubPath14 controlPointsSubPath=pathInitial.subPaths[i];
            
            List<Vector3> boundingBoxVerticesInTriangleStrip = ComputeBoundingBox(pathInitial.subPaths[i]);
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
                
                ShaderinputDataWrapper.basePointIndex = basePointIndexCounter;
                
                ShaderinputDataWrappersSource.Add(ShaderinputDataWrapper);
            }

            basePointIndexCounter += controlPointsSubPath.segments.Count;
        }
        
        //Prepare data for the final path 
        List<Vector3> verticesTarget= new List<Vector3>();
        List<CurveDataWrapper> ShaderinputDataWrappersTarget = new List<CurveDataWrapper>();
        basePointIndexCounter = 0;
        for (int i = 0; i < pathEnd.subPaths.Count; i++)
        {
            SubPath14 controlPointsSubPath=pathEnd.subPaths[i];
            
            List<Vector3> boundingBoxVerticesInTriangleStrip = ComputeBoundingBox(pathEnd.subPaths[i]);
            foreach (Vector3 point in boundingBoxVerticesInTriangleStrip)
            {
                verticesTarget.Add(point);
            }
            for (int j = 0; j < controlPointsSubPath.segments.Count; j++)
            {
                CurveDataWrapper ShaderinputDataWrapper = new CurveDataWrapper();

                ShaderinputDataWrapper.start = controlPointsSubPath.segments[j].p0;
                ShaderinputDataWrapper.control1 = controlPointsSubPath.segments[j].p1;
                ShaderinputDataWrapper.control2 = controlPointsSubPath.segments[j].p2;
                ShaderinputDataWrapper.end = controlPointsSubPath.segments[j].p3;
                
                ShaderinputDataWrapper.basePointIndex = basePointIndexCounter;
                
                ShaderinputDataWrappersTarget.Add(ShaderinputDataWrapper);
            }
            basePointIndexCounter += controlPointsSubPath.segments.Count;
        }
        //create mesh object
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        for (int i = 0; i < vertices.Count; i++)
        {
            indices.Add(i);
        }
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        //transfer data to buffer
        int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CurveDataWrapper));
        
        curveDataBufferSource = new ComputeBuffer(ShaderinputDataWrappersSource.Count, stride);
        curveDataBufferSource.SetData(ShaderinputDataWrappersSource.ToArray());
        FillMaterial.SetBuffer("curvess_buffer", curveDataBufferSource);
        
        curveDataBufferTarget = new ComputeBuffer(ShaderinputDataWrappersTarget.Count, stride);
        curveDataBufferTarget.SetData(ShaderinputDataWrappersTarget.ToArray());
        FillMaterial.SetBuffer("curvest_buffer", curveDataBufferTarget);
        
        stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector3));
        verticesBufferTarget = new ComputeBuffer(verticesTarget.Count, stride);
        verticesBufferTarget.SetData(verticesTarget.ToArray());
        FillMaterial.SetBuffer("verticestarget", verticesBufferTarget);
        
        meshFilter.mesh = mesh;
        meshRenderer.material = FillMaterial;
        
        //Perpare for Stroke
        GameObject gameObject = new GameObject("stroke");
        gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>();
        gameObject.AddComponent<StrokeDrawer14>();
        gameObject.transform.parent = transform;
        
        var strokeObject =gameObject.GetComponent<StrokeDrawer14>();
        strokeObject.material = StrokeMaterial;
        strokeObject.pathInitial=pathInitial;
        strokeObject.pathEnd=pathEnd;
        strokeObject.curveDataBufferSource = curveDataBufferSource;
        strokeObject.curveDataBufferTarget = curveDataBufferTarget;
        
    }

    public void findtheBestOder()
    {
        for (int contourIndex = 0; contourIndex < pathInitial.subPaths.Count; contourIndex++)
        {
            SubPath14 contourInitial = pathInitial.subPaths[contourIndex];
            SubPath14 contourFinal = pathEnd.subPaths[contourIndex];
            int bestStartID = 0;
            float minDistance = int.MaxValue;
            // minDistance = 0;
            for (int i = 0; i < contourInitial.segments.Count(); i++)
            {
                float currentTotalDistance = calculateTotalDistance(i,contourInitial,contourFinal);
                
                //Debug.Log(currentTotalDistance+" "+i);
                if (currentTotalDistance< minDistance)
                {
                    minDistance = currentTotalDistance;
                    bestStartID = i;
                }
            }
            //Debug.Log(bestStartID);
            //bestStartID = 6;
            int count = contourInitial.segments.Count();
            //Debug.Log(contourInitial);
            
            contourInitial.segments = contourInitial.segments.GetRange(bestStartID, count - bestStartID).Concat(
                contourInitial.segments.GetRange(0, bestStartID)).ToList<Segment14>();
        }
    }

    public float calculateTotalDistance(int startID,SubPath14 contourInitial,SubPath14 contourEnd)
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
            
            SubPath14 contourInitial = pathInitial.subPaths[contourIndex];
            
            SubPath14 contourFinal = pathEnd.subPaths[contourIndex];

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
                
                Segment14 firstCurve = new Segment14();
                firstCurve.p0 = splittedCurve.Item1;
                firstCurve.p1 = splittedCurve.Item2;
                firstCurve.p2 = splittedCurve.Item3;
                firstCurve.p3 = splittedCurve.Item4;
                Segment14 secondCurve = new Segment14();
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

    public static List<Vector3> ComputeBoundingBox(SubPath14 subPath)
    {
        List<Segment14> points = subPath.segments;
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
    

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnDestroy()
    {
        curveDataBufferSource.Release();
        curveDataBufferTarget.Release();
        verticesBufferTarget.Release();
    }
}

