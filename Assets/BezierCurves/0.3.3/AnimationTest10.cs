using System;
using System.Collections.Generic;
using System.IO;
//using System.Numerics;
using UnityEngine;
using Unity.VectorGraphics;
using Unity.VisualScripting;

/*
 * Author:Thomas Lu
 * v0.3.3
 * Animation Renderer
 * redesigned the SVGParser, using SVGParser from VectorGraphics
 * can morph a shape to another shape with more amount of curves
 */
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AnimationTest10 : MonoBehaviour
{

    public Material material;
    public Material visualM;
    private List<GameObject> allShapeGameObjects = new List<GameObject>();
    Dictionary<string, int> a;

    Dictionary<string, List<List<Vector3>>> UnpackedSourceSVG =new Dictionary<string, List<List<Vector3>>>();
    Dictionary<string, List<List<Vector3>>> UnpackedTargetSVG =new Dictionary<string, List<List<Vector3>>>();

    public TextAsset sourceSVGFile;
    public TextAsset targetSVGFile;

    public float speed = 1;
    private float val=0;

    public float timer = 0;
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
               gameObject.AddComponent<CurveDrawer10>();
               gameObject.GetComponent<CurveDrawer10>().material = new Material(material);
               gameObject.GetComponent<CurveDrawer10>().visualM = visualM;
               gameObject.transform.parent = transform;
               foreach (var contour in shape.Contours) //each closed M Or a SubPath
               {
                   List<Vector3> controlPointsOfaM = new List<Vector3>();
                   for (int segmentIndex=0;segmentIndex< contour.Segments.Length-1;segmentIndex++)
                   {
                       var segment = contour.Segments[segmentIndex];
                       controlPointsOfaM.Add(new Vector3(segment.P0.x,-segment.P0.y,0));
                       controlPointsOfaM.Add(new Vector3(segment.P1.x,-segment.P1.y,0));
                       controlPointsOfaM.Add(new Vector3(segment.P2.x,-segment.P2.y,0));
                   }

                   controlPointsOfaM.Add(new Vector3(controlPointsOfaM[0].x,controlPointsOfaM[0].y,0));

                   //controlPointsOfaM.Reverse();
                   int orientation = getDirection(controlPointsOfaM);
                   gameObject.GetComponent<CurveDrawer10>().allPointsInitial.Add(new SubPath10(orientation,controlPointsOfaM));
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
                    List<Vector3> controlPointsOfaM = new List<Vector3>();
                    for (int segmentIndex=0;segmentIndex< contour.Segments.Length-1;segmentIndex++)
                    {
                        var segment = contour.Segments[segmentIndex];
                        controlPointsOfaM.Add(new Vector3(segment.P0.x-10,-segment.P0.y,0));
                        controlPointsOfaM.Add(new Vector3(segment.P1.x-10,-segment.P1.y,0));
                        controlPointsOfaM.Add(new Vector3(segment.P2.x-10,-segment.P2.y,0));
                    }
                    controlPointsOfaM.Add(new Vector3(controlPointsOfaM[0].x,controlPointsOfaM[0].y,0));

                    //controlPointsOfaM.Reverse();
                    int orientation = getDirection(controlPointsOfaM);
                    gameObject.GetComponent<CurveDrawer10>().allPointsEnd.Add(new SubPath10(orientation,controlPointsOfaM));
                }
                i++;
            }
        }
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

    protected int getDirection(List<Vector3> polygon)
    {
        double d = 0;
        for (int i = 0; i < polygon.Count - 1; i++)
            d += -0.5 * (polygon[i + 1].y + polygon[i].y) * (polygon[i + 1].x - polygon[i].x);
        if (d > 0)
        {
            return 1; //counterclockwise
        }
        return -1;       //clockwise
    }

    public Vector3 rateFunction_Linear(Vector3 start, Vector3 end, float val)
    {
        return (end - start) * val + start;
    }

    // Update is called once per frame
    private void Update()
    {
        if (!animationEnable) return;

        timer += Time.deltaTime;
        val = (float)(-Math.Cos(timer*speed)+1)/2;

        foreach(GameObject shape in allShapeGameObjects)
        {

            for (int i=0;i < shape?.GetComponent<CurveDrawer10>().allPoints.Count;i++)
            {
                SubPath10 subPath = shape?.GetComponent<CurveDrawer10>().allPoints[i];
                SubPath10 subPathInitial = shape?.GetComponent<CurveDrawer10>().allPointsInitial[i];
                SubPath10 subPathEnd = shape?.GetComponent<CurveDrawer10>().allPointsEnd[i];

                for (int j = 0; j < subPathInitial.controlPoints.Count; j++)
                {
                    subPath.controlPoints[j]= rateFunction_Linear(subPathInitial.controlPoints[j],subPathEnd.controlPoints[j],val);
                }

            }
        }

    }
}
