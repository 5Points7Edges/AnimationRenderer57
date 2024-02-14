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
 * v0.4.1
 * stroke implementation
 */

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AnimationTest15 : MonoBehaviour
{

    public Material Fillmaterial;
    public Material Strokematerial;
    public Material visualM;
    private List<GameObject> allShapeGameObjects = new List<GameObject>();

    private List<Path15> sourceData = new List<Path15>();
    private List<Path15> targetData = new List<Path15>();

    public bool enableStroke = false;
    
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

        // int maxIndex = Math.Max(coordinatesSource.Count, coordinatesTarget.Count);
        // int minIndex= Math.Min(coordinatesSource.Count, coordinatesTarget.Count);
        
        if (coordinatesSource.Count < coordinatesTarget.Count)
        {
            int j = 0;
            for (int i = 0; i < coordinatesSource.Count; i++)
            {
                while (j <= (i+1) *(float)coordinatesTarget.Count/coordinatesSource.Count-1)
                {
                    createObject(ref i, ref j, ref idSource, ref idTarget,ref coordinatesSource,ref coordinatesTarget);
                    j++;
                }
            }
        }
        else
        {
            int i = 0;
            for (int j = 0; j < coordinatesTarget.Count; j++)
            {
                while (i <= (j+1) *(float)coordinatesSource.Count/coordinatesTarget.Count-1)
                {
                    createObject(ref i, ref j, ref idSource, ref idTarget,ref coordinatesSource,ref coordinatesTarget);
                    i++;
                }
            }
        }
        
    }

    public void createObject(ref int i, ref int j,ref List<string> idSource,ref List<string> idTarget,ref List<Coordinate> coordinatesSource,ref List<Coordinate> coordinatesTarget )
    {
        
        //Debug.Log(idSource[distinctID]);
        GameObject gameObject = new GameObject("shape["+i+"]");
        gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>();
        gameObject.AddComponent<FillDrawer15>();
        gameObject.GetComponent<FillDrawer15>().FillMaterial = new Material(Fillmaterial);
        gameObject.GetComponent<FillDrawer15>().StrokeMaterial = new Material(Strokematerial);
        gameObject.transform.parent = transform;
            
        int pathIndexSource = idSource.IndexOf(coordinatesSource[i].pathID);    //the index of the ID in IDList is the index of the path
        int pathIndexTarget = idTarget.IndexOf(coordinatesTarget[j].pathID);
        gameObject.GetComponent<FillDrawer15>().pathInitial=sourceData[pathIndexSource].transform(coordinatesSource[i].x,coordinatesSource[i].y);
        gameObject.GetComponent<FillDrawer15>().pathEnd=targetData[pathIndexTarget].transform(coordinatesTarget[j].x,coordinatesTarget[j].y);
        gameObject.GetComponent<FillDrawer15>().strokeEnable = enableStroke;
            
        allShapeGameObjects.Add(gameObject);
        // if (i == 4)
        // {
        //     Debug.Log(gameObject.GetComponent<FillDrawer15>().pathInitial.subPaths.Count);
        //     Debug.Log(gameObject.GetComponent<FillDrawer15>().pathInitial.subPaths[0]);
        // }
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
        try
        {
            cmd.Start();
        }
        catch (Exception e)
        {
            Debug.Log("xelatex compile error");
            Debug.Log(e);
            return;
        }

        cmd.WaitForExit();
        
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
        try
        {
            cmd.Start();
        }
        catch (Exception e)
        {
            Debug.Log("dvisvgm compile error");
            Debug.Log(e);
            return;
        }
        cmd.WaitForExit();
        
        Debug.Log("dvisvgm compiled");
    }
    public struct Coordinate
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
    public void parseSourceSVGFile(string data,List<Path15> structuredData)
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
                
                Path15 readData = new Path15();
               foreach (var contour in shape.Contours) //each closed M Or a SubPath
               {
                   SubPath15 controlPointsOfaM = new SubPath15();
                   for (int segmentIndex=0;segmentIndex< contour.Segments.Length-1;segmentIndex++)
                   {
                       var segment = contour.Segments[segmentIndex];
                       
                       Segment15 mysegment = new Segment15();
                       float xScale = 1f, yScale = -1f;
                       float xDelta = 0, yDelta = 0; 
                       mysegment.p0 = new Vector3(segment.P0.x*xScale+xDelta, segment.P0.y*yScale+yDelta, -9);
                       mysegment.p1 = new Vector3(segment.P1.x*xScale+xDelta, segment.P1.y*yScale+yDelta, -9);
                       mysegment.p2 = new Vector3(segment.P2.x*xScale+xDelta, segment.P2.y*yScale+yDelta, -9);
                       mysegment.p3 = new Vector3(contour.Segments[segmentIndex + 1].P0.x*xScale+xDelta, contour.Segments[segmentIndex + 1].P0.y*yScale+yDelta, -9);
                       //mysegment.rotate(3.1415f);
                       // mysegment.reverse();
                       controlPointsOfaM.Add(mysegment);
                   }
                   readData.Add(controlPointsOfaM);
               } 
               structuredData.Add(readData);
            }
        }
    }

    public void parseTargetSVGFile(string data,List<Path15> structuredData)
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
                
                Path15 readData = new Path15();
                foreach (var contour in shape.Contours) //each closed M Or a SubPath
                {
                    SubPath15 controlPointsOfaM = new SubPath15();
                    for (int segmentIndex=0;segmentIndex< contour.Segments.Length-1;segmentIndex++)
                    {
                        var segment = contour.Segments[segmentIndex];
                       
                        Segment15 mysegment = new Segment15();
                        float xScale = 1f, yScale = -1f;
                        float xDelta = 0, yDelta = 0; 
                        mysegment.p0 = new Vector3(segment.P0.x*xScale+xDelta, segment.P0.y*yScale+yDelta, -9);
                        mysegment.p1 = new Vector3(segment.P1.x*xScale+xDelta, segment.P1.y*yScale+yDelta, -9);
                        mysegment.p2 = new Vector3(segment.P2.x*xScale+xDelta, segment.P2.y*yScale+yDelta, -9);
                        mysegment.p3 = new Vector3(contour.Segments[segmentIndex + 1].P0.x*xScale+xDelta, contour.Segments[segmentIndex + 1].P0.y*yScale+yDelta, -9);
                        //mysegment.rotate(3.1516f);
                        // mysegment.reverse();
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
            gameObject.GetComponent<CurveDrawer15>().material.SetInt("_enable", animationEnable?1:0);
        }
        */
    }
}
