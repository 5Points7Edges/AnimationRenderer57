using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
//using System.Numerics;
using UnityEngine;
using Unity.VectorGraphics;
using Unity.VisualScripting;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

/*
 * Author:Thomas Lu
 * v0.3.6
 */

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AnimationTest13 : MonoBehaviour
{

    public Material material;
    public Material visualM;
    private List<GameObject> allShapeGameObjects = new List<GameObject>();

    private List<Path13> sourceData = new List<Path13>();
    private List<Path13> targetData = new List<Path13>();
    
    
    public float speed = 1;
    private float val=0;
    
    private float timer = 0;
    public bool animationEnable=false;
    
    public String sourceFileName;
    public String targetFileName;
    private string workFolder = @"E:\SVGCache\";
    
    // Start is called before the first frame update
    void Start()
    {
        
        if (sourceFileName == "") sourceFileName = "CSB";
        if (targetFileName == "") targetFileName = "CSB";
        if (!material)
        {
            Debug.LogError("Please Assign a material on the inspector");
            return;
        }
        if (!File.Exists(workFolder + sourceFileName + ".tex"))
        {
            Debug.LogError("source Tex File do not exist");
            return;
        }
        if (!File.Exists(workFolder + targetFileName + ".tex"))
        {
            Debug.LogError("target Tex File do not exist");
            return;
        }
        if (!File.Exists(workFolder + sourceFileName + ".svg"))
        {
            compile(sourceFileName);
        }
        if (!File.Exists(workFolder + targetFileName + ".svg"))
        {
            compile(targetFileName);
        }

        //string sourceSVGString = sourceSVGFile.text;
        string sourceSVGString=File.ReadAllText(workFolder+sourceFileName+".svg");
        
        //string targetSVGString = targetSVGFile.text;
        string targetSVGString = File.ReadAllText(workFolder+targetFileName+".svg");
        
        parseSVGFile(sourceSVGString,sourceData);
        parseTargetSVGFile(targetSVGString,targetData);
        
        
        //Debug.Log(allShapeGameObjects.Count);
        //parseTargetSVGFile(targetSVGFile.text);
        
        List<string> idSource=IDParsing(sourceSVGString);
        List<Coordinate> coordinatesSource=CoordinatesParsing(sourceSVGString);
        
        List<string> idTarget=IDParsing(targetSVGString);
        List<Coordinate> coordinatesTarget=CoordinatesParsing(targetSVGString);
        
        for (int i = 0; i < Math.Min(coordinatesSource.Count,coordinatesTarget.Count); i++)
        {
            int distinctIDS = idSource.IndexOf(coordinatesSource[i].pathID);
            int distinctIDT = idTarget.IndexOf(coordinatesTarget[i].pathID);
            //Debug.Log(idSource[distinctID]);
            GameObject gameObject = new GameObject("shape["+i+"]");
            gameObject.AddComponent<MeshFilter>();
            gameObject.AddComponent<MeshRenderer>();
            gameObject.AddComponent<CurveDrawer13>();
            gameObject.GetComponent<CurveDrawer13>().material = new Material(material);
            gameObject.GetComponent<CurveDrawer13>().visualM = visualM;
            gameObject.transform.parent = transform;
            gameObject.GetComponent<CurveDrawer13>().pathInitial=sourceData[distinctIDS].transform(coordinatesSource[i].x,coordinatesSource[i].y);
            gameObject.GetComponent<CurveDrawer13>().pathEnd=targetData[distinctIDT].transform(coordinatesTarget[i].x,coordinatesTarget[i].y);
            allShapeGameObjects.Add(gameObject);
        }
        
        
    }

    public void compile(string filename)
    {
        Process cmd = new Process();
        cmd.StartInfo.FileName = @"xelatex";
        cmd.StartInfo.WorkingDirectory = workFolder; 
        cmd.StartInfo.CreateNoWindow = true;
        cmd.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        string param1 = "-no-pdf", param2 = filename+".tex";
        cmd.StartInfo.Arguments = param1+" "+param2;
        cmd.Start();
        while (!cmd.HasExited)
        {
            
        }
        Debug.Log("xelatex compiled");
        cmd = new Process();
        cmd.StartInfo.FileName = @"dvisvgm";
        cmd.StartInfo.UseShellExecute = false;
        cmd.StartInfo.RedirectStandardInput = true;
        cmd.StartInfo.RedirectStandardOutput = true;
        cmd.StartInfo.WorkingDirectory = workFolder;
        cmd.StartInfo.CreateNoWindow = true;
        cmd.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        string param3 = "-n -v 0";
        string param4 = filename+".xdv";
        cmd.StartInfo.Arguments = param3+" "+param4;
        cmd.Start();
        while (!cmd.HasExited)
        {
            
        }
        Debug.Log("dvisvgm compiled");
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
            coord.y = -float.Parse(match.Groups[2].Value);
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
    public void parseSVGFile(string data,List<Path13> structuredData)
    {
        StringReader textReader = new StringReader(data);
        var sceneInfo = SVGParser.ImportSVG(textReader);
        //Debug.Log(sceneInfo.NodeIDs.Count);

        
        
        foreach (var path in sceneInfo.NodeIDs) //each Path
        {
            //Debug.Log("Haha");
            if (path.Value.Shapes == null) continue;
            foreach (var shape in path.Value.Shapes)
            {
                
                Path13 readData = new Path13();
               foreach (var contour in shape.Contours) //each closed M Or a SubPath
               {
                   SubPath13 controlPointsOfaM = new SubPath13();
                   for (int segmentIndex=0;segmentIndex< contour.Segments.Length-1;segmentIndex++)
                   {
                       var segment = contour.Segments[segmentIndex];
                       
                       Segment13 mysegment = new Segment13();
                       float xScale = 1f, yScale = -1f;
                       float xDelta = 0, yDelta = 0; 
                       mysegment.p0 = new Vector3(segment.P0.x*xScale+xDelta, segment.P0.y*yScale+yDelta, -9);
                       mysegment.p1 = new Vector3(segment.P1.x*xScale+xDelta, segment.P1.y*yScale+yDelta, -9);
                       mysegment.p2 = new Vector3(segment.P2.x*xScale+xDelta, segment.P2.y*yScale+yDelta, -9);
                       mysegment.p3 = new Vector3(contour.Segments[segmentIndex + 1].P0.x*xScale+xDelta, contour.Segments[segmentIndex + 1].P0.y*yScale+yDelta, -9);
                       //mysegment.rotate(3.1416f);
                       //mysegment.reverse();
                       controlPointsOfaM.Add(mysegment);
                   }
                   readData.Add(controlPointsOfaM);
               } 
               structuredData.Add(readData);
            }
        }
    }

    public void parseTargetSVGFile(string data,List<Path13> structuredData)
    {
        StringReader textReader = new StringReader(data);
        var sceneInfo = SVGParser.ImportSVG(textReader);
        //Debug.Log(sceneInfo.NodeIDs.Count);

        
        
        foreach (var path in sceneInfo.NodeIDs) //each Path
        {
            //Debug.Log("Haha");
            if (path.Value.Shapes == null) continue;
            foreach (var shape in path.Value.Shapes)
            {
                
                Path13 readData = new Path13();
                foreach (var contour in shape.Contours) //each closed M Or a SubPath
                {
                    SubPath13 controlPointsOfaM = new SubPath13();
                    for (int segmentIndex=0;segmentIndex< contour.Segments.Length-1;segmentIndex++)
                    {
                        var segment = contour.Segments[segmentIndex];
                       
                        Segment13 mysegment = new Segment13();
                        float xScale = 1f, yScale = -1f;
                        float xDelta = 10, yDelta = 0; 
                        mysegment.p0 = new Vector3(segment.P0.x*xScale+xDelta, segment.P0.y*yScale+yDelta, -9);
                        mysegment.p1 = new Vector3(segment.P1.x*xScale+xDelta, segment.P1.y*yScale+yDelta, -9);
                        mysegment.p2 = new Vector3(segment.P2.x*xScale+xDelta, segment.P2.y*yScale+yDelta, -9);
                        mysegment.p3 = new Vector3(contour.Segments[segmentIndex + 1].P0.x*xScale+xDelta, contour.Segments[segmentIndex + 1].P0.y*yScale+yDelta, -9);
                        //mysegment.rotate(3.1416f);
                        //mysegment.reverse();
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
            
            for (int i=0;i < shape?.GetComponent<CurveDrawer13>().pathCurrent.subPaths.Count;i++)
            {
                SubPath13 subPath = shape?.GetComponent<CurveDrawer13>().pathCurrent.subPaths[i];
                SubPath13 subPathInitial = shape?.GetComponent<CurveDrawer13>().pathInitial.subPaths[i];
                SubPath13 subPathEnd = shape?.GetComponent<CurveDrawer13>().pathEnd.subPaths[i];
                
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
