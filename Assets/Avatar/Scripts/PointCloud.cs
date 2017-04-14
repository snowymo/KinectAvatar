﻿using UnityEngine;
using System.Collections;
using Windows.Kinect;

// public enum DepthViewMode
// {
//     SeparateSourceReaders,
//     MultiSourceReader,
//
public class PointCloud : MonoBehaviour {

    public DepthViewMode ViewMode = DepthViewMode.SeparateSourceReaders;

    public GameObject ColorSourceManager;
    public GameObject DepthSourceManager;
    public GameObject MultiSourceManager;

    private KinectSensor _Sensor;
    private CoordinateMapper _Mapper;
    private Mesh _Mesh;

    // Only works at 4 right now
    private const int _DownsampleSize = 4;
    private const double _DepthScale = 0.1f;
    private const int _Speed = 50;

    private MultiSourceManager _MultiManager;
    private ColorSourceManager _ColorManager;
    private DepthSourceManager _DepthManager;

    private GameObject[] points;
    public GameObject prefab;

    void Start()
    {
        _Sensor = KinectSensor.GetDefault();
        if (_Sensor != null)
        {
            _Mapper = _Sensor.CoordinateMapper;
            var frameDesc = _Sensor.DepthFrameSource.FrameDescription;

            // Downsample to lower resolution
            //CreateMesh(frameDesc.Width / _DownsampleSize, frameDesc.Height / _DownsampleSize);

            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }

            CreatePoints(frameDesc.Width, frameDesc.Height);
        }
    }

    void CreatePoints(int width, int height)
    {
        points = new GameObject[width * height];
        for (int y = 0; y < height; y++){
            for (int x = 0; x < width; x++){
                int index = (y * width) + x;
                points[index] = GameObject.Instantiate(prefab);
                points[index].transform.localPosition = new Vector3(x, y, 0);
                points[index].transform.parent = transform;
            }
        }
    }

//     void CreateMesh(int width, int height)
//     {
//         _Mesh = new Mesh();
//         GetComponent<MeshFilter>().mesh = _Mesh;
// 
//         _Vertices = new Vector3[width * height];
//         _UV = new Vector2[width * height];
//         _Triangles = new int[6 * ((width - 1) * (height - 1))];
// 
//         int triangleIndex = 0;
//         for (int y = 0; y < height; y++)
//         {
//             for (int x = 0; x < width; x++)
//             {
//                 int index = (y * width) + x;
// 
//                 _Vertices[index] = new Vector3(x, -y, 0);
//                 _UV[index] = new Vector2(((float)x / (float)width), ((float)y / (float)height));
// 
//                 // Skip the last row/col
//                 if (x != (width - 1) && y != (height - 1))
//                 {
//                     int topLeft = index;
//                     int topRight = topLeft + 1;
//                     int bottomLeft = topLeft + width;
//                     int bottomRight = bottomLeft + 1;
// 
//                     _Triangles[triangleIndex++] = topLeft;
//                     _Triangles[triangleIndex++] = topRight;
//                     _Triangles[triangleIndex++] = bottomLeft;
//                     _Triangles[triangleIndex++] = bottomLeft;
//                     _Triangles[triangleIndex++] = topRight;
//                     _Triangles[triangleIndex++] = bottomRight;
//                 }
//             }
//         }
// 
//         _Mesh.vertices = _Vertices;
//         _Mesh.uv = _UV;
//         _Mesh.triangles = _Triangles;
//         _Mesh.RecalculateNormals();
//     }

    void OnGUI()
    {
        GUI.BeginGroup(new Rect(0, 0, Screen.width, Screen.height));
        GUI.TextField(new Rect(Screen.width - 250, 10, 250, 20), "DepthMode: " + ViewMode.ToString());
        GUI.EndGroup();
    }

    void handleKeyboard()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            if (ViewMode == DepthViewMode.MultiSourceReader)
            {
                ViewMode = DepthViewMode.SeparateSourceReaders;
            }
            else
            {
                ViewMode = DepthViewMode.MultiSourceReader;
            }
        }

        float yVal = Input.GetAxis("Horizontal");
        float xVal = -Input.GetAxis("Vertical");

        transform.Rotate(
            (xVal * Time.deltaTime * _Speed),
            (yVal * Time.deltaTime * _Speed),
            0,
            Space.Self);
    }

    void Update()
    {
        if (_Sensor == null)
        {
            return;
        }

        handleKeyboard();

        if (ViewMode == DepthViewMode.SeparateSourceReaders){
            if (ColorSourceManager == null){
                return;
            }

            _ColorManager = ColorSourceManager.GetComponent<ColorSourceManager>();
            if (_ColorManager == null){
                return;
            }

            if (DepthSourceManager == null){
                return;
            }

            _DepthManager = DepthSourceManager.GetComponent<DepthSourceManager>();
            if (_DepthManager == null){
                return;
            }

            //gameObject.GetComponent<Renderer>().material.mainTexture = _ColorManager.GetColorTexture();
            RefreshData(_DepthManager.GetData(),
                _ColorManager.ColorWidth,
                _ColorManager.ColorHeight);
        }
        else{
            if (MultiSourceManager == null){
                return;
            }

            _MultiManager = MultiSourceManager.GetComponent<MultiSourceManager>();
            if (_MultiManager == null){
                return;
            }

            //gameObject.GetComponent<Renderer>().material.mainTexture = _MultiManager.GetColorTexture();

            RefreshData(_MultiManager.GetDepthData(),
                        _MultiManager.ColorWidth,
                        _MultiManager.ColorHeight);
        }
    }

    private void RefreshData(ushort[] depthData, int colorWidth, int colorHeight)
    {
        var frameDesc = _Sensor.DepthFrameSource.FrameDescription;

        ColorSpacePoint[] colorSpace = new ColorSpacePoint[depthData.Length];
        _Mapper.MapDepthFrameToColorSpace(depthData, colorSpace);

//         for (int y = 0; y < frameDesc.Height; y += _DownsampleSize){
//             for (int x = 0; x < frameDesc.Width; x += _DownsampleSize){
//                 int indexX = x / _DownsampleSize;
//                 int indexY = y / _DownsampleSize;
//                 int smallIndex = (indexY * (frameDesc.Width / _DownsampleSize)) + indexX;
// 
//                 double avg = GetAvg(depthData, x, y, frameDesc.Width, frameDesc.Height);
// 
//                 avg = avg * _DepthScale;
// 
//                 _Vertices[smallIndex].z = (float)avg;
// 
//                 // Update UV mapping with CDRP
//                 var colorSpacePoint = colorSpace[(y * frameDesc.Width) + x];
//                 _UV[smallIndex] = new Vector2(colorSpacePoint.X / colorWidth, colorSpacePoint.Y / colorHeight);
//             }
//         }
        for(int i = 0; i < points.Length; i++)
        {
            int x = i % (frameDesc.Width);
            int y = i / (frameDesc.Width);
            double avg = GetAvg(depthData, x, y, frameDesc.Width, frameDesc.Height);
            avg = avg * _DepthScale;
            //ushort depth = depthData[x + y * frameDesc.Width];
            points[i].transform.localPosition = new Vector3(points[i].transform.localPosition.x, points[i].transform.localPosition.y, (float)avg);
        }

//         _Mesh.vertices = _Vertices;
//         _Mesh.uv = _UV;
//         _Mesh.triangles = _Triangles;
//         _Mesh.RecalculateNormals();
    }

    private double GetAvg(ushort[] depthData, int x, int y, int width, int height)
    {
        double sum = 0.0;
        int count = 0;
        for (int y1 = y; y1 < y + 4; y1++){
            for (int x1 = x; x1 < x + 4; x1++){
                int fullIndex = (y1 * width) + x1;

                if(fullIndex < depthData.Length)
                {
                    if (depthData[fullIndex] == 0)
                        sum += 4500;
                    else
                        sum += depthData[fullIndex];
                    ++count;
                }
            }
        }

        return sum / count;
    }

    void OnApplicationQuit()
    {
        if (_Mapper != null){
            _Mapper = null;
        }

        if (_Sensor != null){
            if (_Sensor.IsOpen){
                _Sensor.Close();
            }

            _Sensor = null;
        }
    }

}
