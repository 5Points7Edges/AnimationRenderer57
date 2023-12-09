using GenericShape;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
//using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;
/*
 * Author:Thomas Lu
 * v0.3.2
 * Animation Renderer
 * code cleaning
 * manipulate each point of the shapes seperately
 * FPS (ChuShiBiao.svg): ca. 12 fps
 */
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AnimationTest9 : MonoBehaviour
{

    public Material material;

    private List<GameObject> allShapeGameObjects = new List<GameObject>();
    Dictionary<string, int> a;

    Dictionary<string, List<List<Vector3>>> UnpackedSourceSVG =new Dictionary<string, List<List<Vector3>>>();
    Dictionary<string, List<List<Vector3>>> UnpackedTargetSVG =new Dictionary<string, List<List<Vector3>>>();

    public TextAsset sourceSVGFile;
    public TextAsset targetSVGFile;
    
    public float speed = 3;
    private float val=0;
    
    public float timer = 0;
    
    public bool animationEnable=false;
    struct Coordinate
    {
        public float x, y;
        public string pathID;
    }

    // Start is called before the first frame update
    void Start()
    {
        Console.WriteLine($"Value of d attribute: ");
        if (!material)
        {
            Debug.LogError("Please Assign a material on the inspector");
            return;
        }


        UnpackedSourceSVG = PathParsing(sourceSVGFile.text);
        List<Coordinate> coordinatesSource = CoordinatesParsing(sourceSVGFile.text);
        
        UnpackedTargetSVG = PathParsing(targetSVGFile.text);
        List<Coordinate> coordinatesTarget = CoordinatesParsing(targetSVGFile.text);

        
        //Debug.Log("still Alive"+Coordinates.Count);
        
        //Pepare for source SVG
        for(int CoordinateIndex = 0; CoordinateIndex < coordinatesSource.Count; CoordinateIndex++)
        {
            GameObject shape = new GameObject("shape[" + CoordinateIndex + "]");
            shape.AddComponent<MeshFilter>();
            shape.AddComponent<MeshRenderer>();
            shape.AddComponent<CurveDrawer9>();
            shape.GetComponent<CurveDrawer9>().material = new Material(material);
            shape.transform.parent = transform;
            
            for (int MIndex = 0; MIndex < UnpackedSourceSVG[coordinatesSource[CoordinateIndex].pathID].Count; MIndex++)
            {
                
                List<Vector3> controlPointsOfaM = new List<Vector3>(UnpackedSourceSVG[coordinatesSource[CoordinateIndex].pathID][MIndex]);
                
                for (int k = 0; k < controlPointsOfaM.Count; k++)
                {
                    controlPointsOfaM[k] = new Vector3(controlPointsOfaM[k].x + coordinatesSource[CoordinateIndex].x,
                      -(controlPointsOfaM[k].y + coordinatesSource[CoordinateIndex].y), controlPointsOfaM[k].z);
                    //controlPointsOfaM[k] = new Vector3(controlPointsOfaM[k].x, -(controlPointsOfaM[k].y), 0);
                    controlPointsOfaM[k] = transform.TransformPoint(controlPointsOfaM[k]);
                }

                int orientation = getDirection(controlPointsOfaM);
                
                
                shape.GetComponent<CurveDrawer9>().allPointsInitial.Add(new SubPath9(orientation,controlPointsOfaM));
                shape.GetComponent<CurveDrawer9>().allPoints.Add(new SubPath9(orientation,controlPointsOfaM));
            }
            allShapeGameObjects.Add(shape);

        }

        //Pepare for target SVG
        for(int CoordinateIndex = 0; CoordinateIndex < coordinatesTarget.Count; CoordinateIndex++)
        {
            GameObject shape = allShapeGameObjects[CoordinateIndex];
            
            for (int MIndex = 0; MIndex < UnpackedTargetSVG[coordinatesTarget[CoordinateIndex].pathID].Count; MIndex++)
            {
                
                List<Vector3> controlPointsOfaM = new List<Vector3>(UnpackedTargetSVG[coordinatesTarget[CoordinateIndex].pathID][MIndex]);
                
                for (int k = 0; k < controlPointsOfaM.Count; k++)
                {
                    controlPointsOfaM[k] = new Vector3(controlPointsOfaM[k].x + coordinatesTarget[CoordinateIndex].x-10,
                        -(controlPointsOfaM[k].y + coordinatesTarget[CoordinateIndex].y), controlPointsOfaM[k].z);
                    //controlPointsOfaM[k] = new Vector3(controlPointsOfaM[k].x, -(controlPointsOfaM[k].y), 0);
                    controlPointsOfaM[k] = transform.TransformPoint(controlPointsOfaM[k]);
                }

                controlPointsOfaM.Reverse();
                int orientation = getDirection(controlPointsOfaM);
                
                shape.GetComponent<CurveDrawer9>().allPointsEnd.Add(new SubPath9(orientation,controlPointsOfaM));
            }
        }
        
    }
    public void OnAnimationStartOrStop()
    {
        animationEnable = !animationEnable;
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
        return result;
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
    public Dictionary<string, List<List<Vector3>>> PathParsing(string input)
    {

        Dictionary<string, List<List<Vector3>>> result = new Dictionary<string, List<List<Vector3>>>();
        

        string pattern = @"<path\s*id\s*=\s*[""']([^""']*)[""']\s*[^>]*d\s*=\s*[""']([^""']*)[""'][^>]*>";

        Regex regex = new Regex(pattern);

        Match match = regex.Match(input);

        while (match.Success)
        {
            List<List<Vector3>> Shapes = new List<List<Vector3>>();

            string id = match.Groups[1].Value;
            //Debug.Log($"Value of ID attribute: {id}");

            string path = match.Groups[2].Value;
            //Debug.Log($"Value of d attribute: {path}");
            match = match.NextMatch();

            string separators = @"(?=[A-Za-z])";
            var tokens = Regex.Split(path, separators).Where(t => !string.IsNullOrEmpty(t));

            float lastX=0,lastY=0, lastlastX = 0, lastlastY = 0, x,y;

            List<Vector3> controlPoints = new List<Vector3>();

            
            // our "interpreter". Runs the list of commands and does something for each of them.
            foreach (string token in tokens)
            {
                SVGCommand c = SVGCommand.Parse(token);

                switch (c.command)
                {
                    case 'M':
                        
                        controlPoints.Add(new Vector3(c.arguments[0], c.arguments[1],0));

                        lastX = c.arguments[0];
                        lastY = c.arguments[1];

                        break;
                    case 'C':
                        controlPoints.Add(new Vector3(c.arguments[0], c.arguments[1], 0));
                        controlPoints.Add(new Vector3(c.arguments[2], c.arguments[3], 0));
                        controlPoints.Add(new Vector3(c.arguments[4], c.arguments[5], 0));

                        lastX = c.arguments[4];
                        lastY = c.arguments[5];
                        lastlastX = c.arguments[2];
                        lastlastY = c.arguments[3];


                        break;
                    case 'S':
                        controlPoints.Add(new Vector3(lastX - lastlastX + lastX,lastY-lastlastY+lastY, 0));
                        controlPoints.Add(new Vector3(c.arguments[0], c.arguments[1], 0));
                        controlPoints.Add(new Vector3(c.arguments[2], c.arguments[3], 0));

                        lastX = c.arguments[2];
                        lastY = c.arguments[3];
                        lastlastX = c.arguments[0];
                        lastlastY = c.arguments[1];
                        break;

                    case 'L':
                        x = c.arguments[0];
                        y = c.arguments[1];
                        controlPoints.Add(new Vector3((x - lastX) /3 + lastX, (y - lastY) / 3 + lastY, 0));
                        controlPoints.Add(new Vector3((x - lastX) * 2 / 3 + lastX, (y - lastY) * 2 / 3 + lastY, 0));
                        controlPoints.Add(new Vector3(x, y, 0));

                        lastX = x;
                        lastY = y;
                        break;
                    case 'H':
                        x = c.arguments[0];
                        y = lastY;
                        controlPoints.Add(new Vector3((x - lastX) / 3 + lastX, (y - lastY) / 3 + lastY, 0));
                        controlPoints.Add(new Vector3((x - lastX) * 2 / 3 + lastX, (y - lastY) * 2 / 3 + lastY, 0));
                        controlPoints.Add(new Vector3(x, y, 0));

                        lastX = x;
                        lastY = y;
                        break;
                    case 'V':
                        x = lastX;
                        y = c.arguments[0];
                        controlPoints.Add(new Vector3((x - lastX) / 3 + lastX, (y - lastY) / 3 + lastY, 0));
                        controlPoints.Add(new Vector3((x - lastX) * 2 / 3 + lastX, (y - lastY) * 2 / 3 + lastY, 0));
                        controlPoints.Add(new Vector3(x, y, 0));

                        lastX = x;
                        lastY = y;
                        break;
                    case 'Z':
                        Shapes.Add(controlPoints);
                        controlPoints = new List<Vector3>();
                        break;
                    default:
                        break;
                    
                    
                }
            }

            result.Add(id, Shapes);
        }

        

        return result;
    }


    // Update is called once per frame
    private void Update()
    {
        if (!animationEnable) return;
        timer += Time.deltaTime;
        val = (float)(Math.Sin(timer*speed)+1)/2;
        
        foreach(GameObject shape in allShapeGameObjects)
        {
            for (int i=0;i < shape?.GetComponent<CurveDrawer9>().allPoints.Count;i++)
            {
                SubPath9 subPath = shape?.GetComponent<CurveDrawer9>().allPoints[i];
                SubPath9 subPathInitial = shape?.GetComponent<CurveDrawer9>().allPointsInitial[i];
                SubPath9 subPathEnd = shape?.GetComponent<CurveDrawer9>().allPointsEnd[i];
                for (int j = 0; j < subPath.controlPoints.Count; j++)
                {
                    float startx = subPathInitial.controlPoints[j].x;
                    float starty = subPathInitial.controlPoints[j].y;

                    float endx = subPathEnd.controlPoints[j].x;
                    float endy = subPathEnd.controlPoints[j].y;

                    float currentx = (endx - startx) * val + startx;
                    float currenty = (endy - starty) * val + starty;
                    subPath.controlPoints[j]= new Vector3(currentx,currenty, subPathInitial.controlPoints[j].z);
                }
                subPath.calculateBasePoint();
            }
        }
        
    }
}
