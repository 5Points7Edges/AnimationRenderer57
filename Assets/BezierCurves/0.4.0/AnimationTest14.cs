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
 * v0.4.0
 * stroke implementation
 */

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AnimationTest14 : MonoBehaviour
{

    public Material Fillmaterial;
    public Material Strokematerial;
    public Material visualM;
    private List<GameObject> allShapeGameObjects = new List<GameObject>();

    private List<Path14> sourceData = new List<Path14>();
    private List<Path14> targetData = new List<Path14>();
    
    
    
    public bool animationEnable=false;
    
    public String sourceFileName;
    public String targetFileName;
    private string workFolder = @"E:\SVGCache\";
    
    // Start is called before the first frame update
    void Start()
    {
        
        if (sourceFileName == "") sourceFileName = "CSB";
        if (targetFileName == "") targetFileName = "CSB";
        if (!Fillmaterial)
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
        string sourceSVGPath=File.ReadAllText(workFolder+sourceFileName+".svg");
        
        //string targetSVGString = targetSVGFile.text;
        string targetSVGPath = File.ReadAllText(workFolder+targetFileName+".svg");
        
        parseSourceSVGFile(sourceSVGPath,sourceData);
        parseTargetSVGFile(targetSVGPath,targetData);
        
        
        
        List<string> idSource=IDParsing(sourceSVGPath); //A list of IDs of paths. The order of the path should not be changed
        List<Coordinate> coordinatesSource=CoordinatesParsing(sourceSVGPath);//a pair of <use ...> xlink coordinates.
        
        List<string> idTarget=IDParsing(targetSVGPath);
        List<Coordinate> coordinatesTarget=CoordinatesParsing(targetSVGPath);
        
        for (int i = 0; i < Math.Min(coordinatesSource.Count,coordinatesTarget.Count); i++)
        {
            int pathIndexSource = idSource.IndexOf(coordinatesSource[i].pathID);    //the index of the ID in IDList is the index of the path
            int pathIndexTarget = idTarget.IndexOf(coordinatesTarget[i].pathID);
            //Debug.Log(idSource[distinctID]);
            GameObject gameObject = new GameObject("shape["+i+"]");
            gameObject.AddComponent<MeshFilter>();
            gameObject.AddComponent<MeshRenderer>();
            gameObject.AddComponent<FillDrawer14>();
            gameObject.GetComponent<FillDrawer14>().FillMaterial = new Material(Fillmaterial);
            gameObject.GetComponent<FillDrawer14>().StrokeMaterial = new Material(Strokematerial);
            gameObject.transform.parent = transform;
            
            gameObject.GetComponent<FillDrawer14>().pathInitial=sourceData[pathIndexSource].transform(coordinatesSource[i].x,coordinatesSource[i].y);
            gameObject.GetComponent<FillDrawer14>().pathEnd=targetData[pathIndexTarget].transform(coordinatesTarget[i].x,coordinatesTarget[i].y);
            
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
    public void parseSourceSVGFile(string data,List<Path14> structuredData)
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
                
                Path14 readData = new Path14();
               foreach (var contour in shape.Contours) //each closed M Or a SubPath
               {
                   SubPath14 controlPointsOfaM = new SubPath14();
                   for (int segmentIndex=0;segmentIndex< contour.Segments.Length-1;segmentIndex++)
                   {
                       var segment = contour.Segments[segmentIndex];
                       
                       Segment14 mysegment = new Segment14();
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

    public void parseTargetSVGFile(string data,List<Path14> structuredData)
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
                
                Path14 readData = new Path14();
                foreach (var contour in shape.Contours) //each closed M Or a SubPath
                {
                    SubPath14 controlPointsOfaM = new SubPath14();
                    for (int segmentIndex=0;segmentIndex< contour.Segments.Length-1;segmentIndex++)
                    {
                        var segment = contour.Segments[segmentIndex];
                       
                        Segment14 mysegment = new Segment14();
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
    
    // Update is called once per frame
    private void Update()
    {
        /*foreach (var gameObject in allShapeGameObjects)
        {
            gameObject.GetComponent<CurveDrawer14>().material.SetInt("_enable", animationEnable?1:0);
        }
        */
    }
}
