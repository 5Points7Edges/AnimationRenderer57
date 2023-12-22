using System;
using System.Collections.Generic;
using System.IO;
//using System.Numerics;
using UnityEngine;
using Unity.VectorGraphics;
using Unity.VisualScripting;

/*
 * Author:Thomas Lu
 * v0.3.4
 * Animation Renderer
 * code restructured (Path -> SubPath -> Segments -> Vector3)
 * finding the best sequence by morphing curves
 */

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AnimationTest11 : MonoBehaviour
{

    public Material material;
    public Material visualM;
    private List<GameObject> allShapeGameObjects = new List<GameObject>();
    
    public TextAsset sourceSVGFile;
    public TextAsset targetSVGFile;
    
    public float speed = 1;
    private float val=0;
    
    private float timer = 0;
    public bool animationEnable=false;


    // Start is called before the first frame update
    void Start()
    {
        Console.WriteLine($"Value of d attribute: ");
        if (!material)
        {
            Debug.LogError("Please Assign a material on the inspector");
            return;
        }

        
        parseSVGFile(sourceSVGFile.text);

        parseTargetSVGFile(targetSVGFile.text);

    }

    public void parseSVGFile(string data)
    {
        StringReader textReader = new StringReader(data);
        var sceneInfo = SVGParser.ImportSVG(textReader);
        Debug.Log(sceneInfo.NodeIDs.Count);
        
        foreach (var path in sceneInfo.NodeIDs) //each Path
        {
            if (path.Value.Shapes == null) continue;
            foreach (var shape in path.Value.Shapes)
            {
               GameObject gameObject = new GameObject("shape");
               gameObject.AddComponent<MeshFilter>();
               gameObject.AddComponent<MeshRenderer>();
               gameObject.AddComponent<CurveDrawer11>();
               gameObject.GetComponent<CurveDrawer11>().material = new Material(material);
               gameObject.GetComponent<CurveDrawer11>().visualM = visualM;
               gameObject.transform.parent = transform;
               foreach (var contour in shape.Contours) //each closed M Or a SubPath
               {
                   SubPath11 controlPointsOfaM = new SubPath11();
                   for (int segmentIndex=0;segmentIndex< contour.Segments.Length-1;segmentIndex++)
                   {
                       var segment = contour.Segments[segmentIndex];
                       
                       Segment11 mysegment = new Segment11();
                       mysegment.p0 = new Vector3(segment.P0.x, -segment.P0.y, 0);
                       mysegment.p1 = new Vector3(segment.P1.x, -segment.P1.y, 0);
                       mysegment.p2 = new Vector3(segment.P2.x, -segment.P2.y, 0);
                       mysegment.p3 = new Vector3(contour.Segments[segmentIndex + 1].P0.x, -contour.Segments[segmentIndex + 1].P0.y, 0);
                       
                       controlPointsOfaM.Add(mysegment);
                   }
                   
                   gameObject.GetComponent<CurveDrawer11>().pathInitial.Add(controlPointsOfaM);
               } 
               allShapeGameObjects.Add(gameObject);
            }
        }
    }

    public void parseTargetSVGFile(string data)
    {
        StringReader textReader = new StringReader(data);
        var sceneInfo = SVGParser.ImportSVG(textReader);
        Debug.Log(sceneInfo.NodeIDs.Count);
        int i = 0;
        
        foreach (var path in sceneInfo.NodeIDs) //each Path
        {
            if (path.Value.Shapes == null) continue;
            foreach (var shape in path.Value.Shapes)
            {
                
                GameObject gameObject = allShapeGameObjects[i];
                foreach (var contour in shape.Contours) //each closed M Or a SubPath
                {
                    SubPath11 controlPointsOfaM = new SubPath11();
                    for (int segmentIndex=0;segmentIndex< contour.Segments.Length-1;segmentIndex++)
                    {
                        var segment = contour.Segments[segmentIndex];
                       
                        Segment11 mysegment = new Segment11();

                        float xDelta = 2, yDelta = -10;
                        mysegment.p0= new Vector3(segment.P0.x+xDelta, -segment.P0.y+yDelta, 0);
                        mysegment.p1 = new Vector3(segment.P1.x+xDelta, -segment.P1.y+yDelta, 0);
                        mysegment.p2 = new Vector3(segment.P2.x+xDelta, -segment.P2.y+yDelta, 0);
                        mysegment.p3 = new Vector3(contour.Segments[segmentIndex + 1].P0.x+xDelta, -contour.Segments[segmentIndex + 1].P0.y+yDelta, 0);

                        //mysegment.p0 = RotateRadians(mysegment.p0,3.14f/6);
                        //mysegment.p1 = RotateRadians(mysegment.p1,3.14f/6);
                        //mysegment.p2 = RotateRadians(mysegment.p2,3.14f/6);
                        //mysegment.p3 = RotateRadians(mysegment.p3,3.14f/6);
                        controlPointsOfaM.Add(mysegment);
                    }
                    //controlPointsOfaM.segments.Reverse();
                   
                    gameObject.GetComponent<CurveDrawer11>().pathEnd.Add(controlPointsOfaM);
                }
                i++;
            }
        }
    }
    public Vector3 RotateRadians(Vector3 v, float radians)
    {
        var ca = (float)Math.Cos(radians);
        var sa = (float)Math.Sin(radians);
        return new Vector3(ca*v.x - sa*v.y, sa*v.x + ca*v.y,0);
    }
    public void OnAnimationStartOrStop()
    {
        animationEnable = !animationEnable;
        Debug.Log("stop Button Clicked!");
    }
    public void OnAnimationTest1()
    {
        //animationEnable = false;
        Debug.Log("Test Button Clicked!");
    }
    public void OnAnimationRestart()
    {
        this.Start();
        Debug.Log("Restart Button Clicked!");
    }
    
    public static Vector3 rateFunction_Linear(Vector3 start, Vector3 end, float val)
    {
        return (end - start) * val + start;
    }

    // Update is called once per frame
    private void Update()
    {
        if (!animationEnable) return;
        
        timer += Time.deltaTime;
        val = (float)(-Math.Cos(timer*speed)+1)/2;
        //val = timer - (int)timer;
        foreach(GameObject shape in allShapeGameObjects)
        {
            
            for (int i=0;i < shape?.GetComponent<CurveDrawer11>().pathCurrent.subPaths.Count;i++)
            {
                SubPath11 subPath = shape?.GetComponent<CurveDrawer11>().pathCurrent.subPaths[i];
                SubPath11 subPathInitial = shape?.GetComponent<CurveDrawer11>().pathInitial.subPaths[i];
                SubPath11 subPathEnd = shape?.GetComponent<CurveDrawer11>().pathEnd.subPaths[i];
                
                for (int j = 0; j < subPathInitial.segments.Count; j++)
                {
                    subPath.segments[j].p0= rateFunction_Linear(subPathInitial.segments[j].p0,subPathEnd.segments[j].p0,val);
                    subPath.segments[j].p1= rateFunction_Linear(subPathInitial.segments[j].p1,subPathEnd.segments[j].p1,val);
                    subPath.segments[j].p2= rateFunction_Linear(subPathInitial.segments[j].p2,subPathEnd.segments[j].p2,val);
                    subPath.segments[j].p3= rateFunction_Linear(subPathInitial.segments[j].p3,subPathEnd.segments[j].p3,val);
                }
                
            }
        }
        
    }
}
