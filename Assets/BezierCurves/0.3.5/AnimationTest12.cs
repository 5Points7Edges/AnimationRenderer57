using System;
using System.Collections.Generic;
using System.IO;
//using System.Numerics;
using UnityEngine;
using Unity.VectorGraphics;
using Unity.VisualScripting;
using System.Text.RegularExpressions;
using System.Linq;
/*
 * Author:Thomas Lu
 * v0.3.5
 */

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AnimationTest12 : MonoBehaviour
{

    public Material material;
    public Material visualM;
    private List<GameObject> allShapeGameObjects = new List<GameObject>();

    private List<Path12> sourceData = new List<Path12>();
    private List<Path12> targetData = new List<Path12>();
    
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
        
        parseSVGFile(sourceSVGFile.text,sourceData);
        parseTargetSVGFile(targetSVGFile.text,targetData);
        
        
        //Debug.Log(allShapeGameObjects.Count);
        //parseTargetSVGFile(targetSVGFile.text);

        List<string> idList=IDParsing(sourceSVGFile.text);
        List<Coordinate> co=CoordinatesParsing(sourceSVGFile.text);
        

        for (int i = 0; i < co.Count; i++)
        {
            int distinctID = idList.IndexOf(co[i].pathID);
            Debug.Log(idList[distinctID]);
            GameObject gameObject = new GameObject("shape["+i+"]");
            gameObject.AddComponent<MeshFilter>();
            gameObject.AddComponent<MeshRenderer>();
            gameObject.AddComponent<CurveDrawer12>();
            gameObject.GetComponent<CurveDrawer12>().material = new Material(material);
            gameObject.GetComponent<CurveDrawer12>().visualM = visualM;
            gameObject.transform.parent = transform;
            gameObject.GetComponent<CurveDrawer12>().pathInitial=sourceData[distinctID].transform(co[i].x,co[i].y);
            gameObject.GetComponent<CurveDrawer12>().pathEnd=targetData[distinctID].transform(co[i].x,co[i].y);
            allShapeGameObjects.Add(gameObject);
        }
        
        for (int i = 0; i < idList.Count; i++)
        {
            //Debug.Log(idList[i]);
        }
        for (int i = 0; i < co.Count; i++)
        {
            //Debug.Log(co[i].pathID+" "+i);
        }
    }
    struct Coordinate
    {
        public float x, y;
        public string pathID;
    }
    class SVGCommand
    {
        public char command {get; private set;}
        public float[] arguments {get; private set;}

        public SVGCommand(char command, params float[] arguments)
        {
            this.command=command;
            this.arguments=arguments;
        }

        public static SVGCommand Parse(string SVGpathstring)
        {
            var cmd = SVGpathstring.Take(1).Single();
            string remainingargs = SVGpathstring.Substring(1);

            string argSeparators = @"[\s,]|(?=-)";
            var splitArgs = Regex
                .Split(remainingargs, argSeparators)
                .Where(t => !string.IsNullOrEmpty(t));

            float[] floatArgs = splitArgs.Select(arg => float.Parse(arg)).ToArray();
            return new SVGCommand(cmd,floatArgs);
        }
    }
    List<Coordinate> CoordinatesParsing(string input)
    {
        List < Coordinate > result = new List < Coordinate >();
        string pattern = @"<use\s*x\s*=\s*[""']([^""']*)[""']\s*[^>]*y\s*=\s*[""']([^""']*)[""'][^>]*xlink:href\s*=\s*[""']#([^""']*)[""'][^>]*>";

        Regex regex = new Regex(pattern);

        Match match = regex.Match(input);


        while (match.Success)
        {
            Coordinate coord;
            coord.x = float.Parse(match.Groups[1].Value);
            coord.y = float.Parse(match.Groups[2].Value);
            coord.pathID= match.Groups[3].Value;
            result.Add(coord);
            match = match.NextMatch();
        }

        if (result.Count == 0)
        {
            Debug.Log("No PathCoordinate Found");
        }
        return result;
    }
    List<string> IDParsing(string input)
    {
        List < string > result = new List < string >();
        string pattern = @"<path\s*id\s*=\s*[""']([^""']*)[""']\s*[^>]*>";

        Regex regex = new Regex(pattern);

        Match match = regex.Match(input);
        
        while (match.Success)
        {
            string id = match.Groups[1].Value;
            result.Add(id);
            
            match = match.NextMatch();
        }

        if (result.Count == 0)
        {
            Debug.Log("No PathID Found");
        }
        return result;
    }
    public void parseSVGFile(string data,List<Path12> structuredData)
    {
        StringReader textReader = new StringReader(data);
        var sceneInfo = SVGParser.ImportSVG(textReader);
        Debug.Log(sceneInfo.NodeIDs.Count);

        
        
        foreach (var path in sceneInfo.NodeIDs) //each Path
        {
            Debug.Log("Haha");
            if (path.Value.Shapes == null) continue;
            foreach (var shape in path.Value.Shapes)
            {
                
                Path12 readData = new Path12();
               foreach (var contour in shape.Contours) //each closed M Or a SubPath
               {
                   SubPath12 controlPointsOfaM = new SubPath12();
                   for (int segmentIndex=0;segmentIndex< contour.Segments.Length-1;segmentIndex++)
                   {
                       var segment = contour.Segments[segmentIndex];
                       
                       Segment12 mysegment = new Segment12();
                       float xScale = 1f, yScale = -1f;
                       float xDelta = 0, yDelta = 0; 
                       mysegment.p0 = new Vector3(segment.P0.x*xScale+xDelta, segment.P0.y*yScale+yDelta, -9);
                       mysegment.p1 = new Vector3(segment.P1.x*xScale+xDelta, segment.P1.y*yScale+yDelta, -9);
                       mysegment.p2 = new Vector3(segment.P2.x*xScale+xDelta, segment.P2.y*yScale+yDelta, -9);
                       mysegment.p3 = new Vector3(contour.Segments[segmentIndex + 1].P0.x*xScale+xDelta, contour.Segments[segmentIndex + 1].P0.y*yScale+yDelta, -9);
                       //mysegment.rotate(3.1416f);
                       mysegment.reverse();
                       controlPointsOfaM.Add(mysegment);
                   }
                   readData.Add(controlPointsOfaM);
               } 
               structuredData.Add(readData);
            }
        }
    }

    public void parseTargetSVGFile(string data,List<Path12> structuredData)
    {
        StringReader textReader = new StringReader(data);
        var sceneInfo = SVGParser.ImportSVG(textReader);
        Debug.Log(sceneInfo.NodeIDs.Count);

        
        
        foreach (var path in sceneInfo.NodeIDs) //each Path
        {
            Debug.Log("Haha");
            if (path.Value.Shapes == null) continue;
            foreach (var shape in path.Value.Shapes)
            {
                
                Path12 readData = new Path12();
                foreach (var contour in shape.Contours) //each closed M Or a SubPath
                {
                    SubPath12 controlPointsOfaM = new SubPath12();
                    for (int segmentIndex=0;segmentIndex< contour.Segments.Length-1;segmentIndex++)
                    {
                        var segment = contour.Segments[segmentIndex];
                       
                        Segment12 mysegment = new Segment12();
                        float xScale = 1f, yScale = -1f;
                        float xDelta = 10, yDelta = 0; 
                        mysegment.p0 = new Vector3(segment.P0.x*xScale+xDelta, segment.P0.y*yScale+yDelta, -9);
                        mysegment.p1 = new Vector3(segment.P1.x*xScale+xDelta, segment.P1.y*yScale+yDelta, -9);
                        mysegment.p2 = new Vector3(segment.P2.x*xScale+xDelta, segment.P2.y*yScale+yDelta, -9);
                        mysegment.p3 = new Vector3(contour.Segments[segmentIndex + 1].P0.x*xScale+xDelta, contour.Segments[segmentIndex + 1].P0.y*yScale+yDelta, -9);
                        //mysegment.rotate(3.1416f);
                        mysegment.reverse();
                        controlPointsOfaM.Add(mysegment);
                    }
                    readData.Add(controlPointsOfaM);
                } 
                structuredData.Add(readData);
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
            
            for (int i=0;i < shape?.GetComponent<CurveDrawer12>().pathCurrent.subPaths.Count;i++)
            {
                SubPath12 subPath = shape?.GetComponent<CurveDrawer12>().pathCurrent.subPaths[i];
                SubPath12 subPathInitial = shape?.GetComponent<CurveDrawer12>().pathInitial.subPaths[i];
                SubPath12 subPathEnd = shape?.GetComponent<CurveDrawer12>().pathEnd.subPaths[i];
                
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
