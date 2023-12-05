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
using Color = UnityEngine.Color;
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SVGRendererTest7 : MonoBehaviour
{

    public Material material;

    private List<GameObject> allShapeGameObjects = new List<GameObject>();
    Dictionary<string, int> a;

    Dictionary<string, List<List<float3>>> UnpackedSVG =new Dictionary<string, List<List<float3>>>();

    public TextAsset svgFile;

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


        Dictionary<string, List<List<float3>>> UnpackedPaths = PathParsing(svgFile.text);

        List<Coordinate> Coordinates = CoordinatesParsing(svgFile.text);
        //Debug.Log("still Alive"+Coordinates.Count);
        
        for(int CoordinateIndex = 0; CoordinateIndex < Coordinates.Count; CoordinateIndex++)
        {
            GameObject shape = new GameObject("shape[" + CoordinateIndex + "]");
            shape.AddComponent<MeshFilter>();
            shape.AddComponent<MeshRenderer>();
            shape.AddComponent<CurveDrawer7>();
            shape.GetComponent<CurveDrawer7>().material = new Material(material);
            shape.transform.parent = transform;
            
            for (int MIndex = 0; MIndex < UnpackedPaths[Coordinates[CoordinateIndex].pathID].Count; MIndex++)
            {
                
                List<float3> controlPointsOfaM =
                    new List<float3>(UnpackedPaths[Coordinates[CoordinateIndex].pathID][MIndex]);
                for (int k = 0; k < controlPointsOfaM.Count; k++)
                {
                    controlPointsOfaM[k] = new float3(controlPointsOfaM[k].x + Coordinates[CoordinateIndex].x,
                      -(controlPointsOfaM[k].y + Coordinates[CoordinateIndex].y), 0);
                    //controlPointsOfaM[k] = new float3(controlPointsOfaM[k].x, -(controlPointsOfaM[k].y), 0);
                    controlPointsOfaM[k] = new float3(transform.TransformPoint(new Vector3(controlPointsOfaM[k].x,
                        controlPointsOfaM[k].y, controlPointsOfaM[k].z)));
                }

                int orientation = getOrientation(controlPointsOfaM);
                SubPath subPath = new SubPath(orientation,controlPointsOfaM);
                shape.GetComponent<CurveDrawer7>().allPoints.Add(subPath);
            }
            allShapeGameObjects.Add(shape);

        }
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
    protected int getOrientation(List<float3> polygon)
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
    public Dictionary<string, List<List<float3>>> PathParsing(string input)
    {

        Dictionary<string, List<List<float3>>> result = new Dictionary<string, List<List<float3>>>();
        

        string pattern = @"<path\s*id\s*=\s*[""']([^""']*)[""']\s*[^>]*d\s*=\s*[""']([^""']*)[""'][^>]*>";

        Regex regex = new Regex(pattern);

        Match match = regex.Match(input);

        while (match.Success)
        {
            List<List<float3>> Shapes = new List<List<float3>>();

            string id = match.Groups[1].Value;
            //Debug.Log($"Value of ID attribute: {id}");

            string path = match.Groups[2].Value;
            //Debug.Log($"Value of d attribute: {path}");
            match = match.NextMatch();

            string separators = @"(?=[A-Za-z])";
            var tokens = Regex.Split(path, separators).Where(t => !string.IsNullOrEmpty(t));

            float lastX=0,lastY=0, lastlastX = 0, lastlastY = 0, x,y;

            List<float3> controlPoints = new List<float3>();

            
            // our "interpreter". Runs the list of commands and does something for each of them.
            foreach (string token in tokens)
            {
                SVGCommand c = SVGCommand.Parse(token);

                switch (c.command)
                {
                    case 'M':
                        
                        controlPoints.Add(new float3(c.arguments[0], c.arguments[1],0));

                        lastX = c.arguments[0];
                        lastY = c.arguments[1];

                        break;
                    case 'C':
                        controlPoints.Add(new float3(c.arguments[0], c.arguments[1], 0));
                        controlPoints.Add(new float3(c.arguments[2], c.arguments[3], 0));
                        controlPoints.Add(new float3(c.arguments[4], c.arguments[5], 0));

                        lastX = c.arguments[4];
                        lastY = c.arguments[5];
                        lastlastX = c.arguments[2];
                        lastlastY = c.arguments[3];


                        break;
                    case 'S':
                        controlPoints.Add(new float3(lastX - lastlastX + lastX,lastY-lastlastY+lastY, 0));
                        controlPoints.Add(new float3(c.arguments[0], c.arguments[1], 0));
                        controlPoints.Add(new float3(c.arguments[2], c.arguments[3], 0));

                        lastX = c.arguments[2];
                        lastY = c.arguments[3];
                        lastlastX = c.arguments[0];
                        lastlastY = c.arguments[1];
                        break;

                    case 'L':
                        x = c.arguments[0];
                        y = c.arguments[1];
                        controlPoints.Add(new float3((x - lastX) /3 + lastX, (y - lastY) / 3 + lastY, 0));
                        controlPoints.Add(new float3((x - lastX) * 2 / 3 + lastX, (y - lastY) * 2 / 3 + lastY, 0));
                        controlPoints.Add(new float3(x, y, 0));

                        lastX = x;
                        lastY = y;
                        break;
                    case 'H':
                        x = c.arguments[0];
                        y = lastY;
                        controlPoints.Add(new float3((x - lastX) / 3 + lastX, (y - lastY) / 3 + lastY, 0));
                        controlPoints.Add(new float3((x - lastX) * 2 / 3 + lastX, (y - lastY) * 2 / 3 + lastY, 0));
                        controlPoints.Add(new float3(x, y, 0));

                        lastX = x;
                        lastY = y;
                        break;
                    case 'V':
                        x = lastX;
                        y = c.arguments[0];
                        controlPoints.Add(new float3((x - lastX) / 3 + lastX, (y - lastY) / 3 + lastY, 0));
                        controlPoints.Add(new float3((x - lastX) * 2 / 3 + lastX, (y - lastY) * 2 / 3 + lastY, 0));
                        controlPoints.Add(new float3(x, y, 0));

                        lastX = x;
                        lastY = y;
                        break;
                    case 'Z':
                        Shapes.Add(controlPoints);
                        controlPoints = new List<float3>();
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
        
    }
}
