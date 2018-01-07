using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HeightMappedComputeShaderController : MonoBehaviour {
    ComputeBuffer particleBuffer;
    int kernelID;
    public ComputeShader compute;
    public Texture2D heightMap;
    public GameObject heightMappedObject;
    public int numberOfParticles = 1000;
    public int particleSize = 24;
    public int threadGroupsX = 8;
    public float trianglePointSize = .02f;

    Vector3 heightMapBottomLeft;
    Vector3 heightMapBottomRight;
    Vector3 heightMapUpperLeft;
    Vector3 heightMapUpperRight;
    //public Material material;
    private Mesh mesh;
    Particle[] output;
    //public TextMesh displayText;


    private struct Particle
    {
        public Vector4 position;
        public Vector4 velocity;
    }

    // Use this for initialization
    void Start()
    {
        Bounds b = heightMappedObject.GetComponent<Renderer>().bounds;
        heightMapBottomLeft = new Vector3(b.center.x - b.extents.x, 0f, b.center.z - b.extents.z);
        heightMapBottomRight = new Vector3(b.center.x + b.extents.x, 0f, b.center.z - b.extents.z);
        heightMapUpperLeft = new Vector3(b.center.x - b.extents.x, 0f, b.center.z + b.extents.z);
        heightMapUpperRight = new Vector3(b.center.x + b.extents.x, 0f, b.center.z + b.extents.z);


        /*Vector3[] heightMappedObjectVertices = heightMappedObject.GetComponent<MeshFilter>().sharedMesh.vertices;
        Transform tr = heightMappedObject.transform;
        heightMapBottomLeft = tr.TransformPoint(heightMappedObjectVertices[0]);
        heightMapBottomRight = tr.TransformPoint(heightMappedObjectVertices[0]);
        heightMapUpperLeft = tr.TransformPoint(heightMappedObjectVertices[0]);
        heightMapUpperRight = tr.TransformPoint(heightMappedObjectVertices[0]);

        for (int i = 1; i < heightMappedObjectVertices.Length; i++)
        {
            if (tr.TransformPoint(heightMappedObjectVertices[i]).x < heightMapBottomLeft.x)
            {
                heightMapBottomLeft = tr.TransformPoint(heightMappedObjectVertices[i]);
            }
            if (tr.TransformPoint(heightMappedObjectVertices[i]).x > heightMapBottomRight.x)
            {
                heightMapBottomRight = tr.TransformPoint(heightMappedObjectVertices[i]);
            }
            if (tr.TransformPoint(heightMappedObjectVertices[i]).z < heightMapUpperLeft.z)
            {
                heightMapUpperLeft = tr.TransformPoint(heightMappedObjectVertices[i]);
            }
            if (tr.TransformPoint(heightMappedObjectVertices[i]).z > heightMapUpperRight.z)
            {
                heightMapUpperRight = tr.TransformPoint(heightMappedObjectVertices[i]);
            }
        }*/
        Debug.Log("Bottom Left: " + heightMapBottomLeft.ToString());
        Debug.Log("Bottom Right: " + heightMapBottomRight.ToString());
        Debug.Log("Top Left: " + heightMapUpperLeft.ToString());
        Debug.Log("Top Right: " + heightMapUpperRight.ToString());

        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        particleBuffer = new ComputeBuffer(numberOfParticles, particleSize);
        Particle[] particleArray = new Particle[numberOfParticles];
        output = new Particle[numberOfParticles];

        for (int i = 0; i < numberOfParticles; ++i)
        {
            particleArray[i].position.x = UnityEngine.Random.Range((float)(heightMapBottomLeft.x), (float)(heightMapBottomRight.x));
            particleArray[i].position.y = UnityEngine.Random.Range(b.center.y + b.extents.y + .1f, b.center.y + 2*b.extents.y);
            particleArray[i].position.z = UnityEngine.Random.Range((float)(heightMapBottomLeft.z), (float)(heightMapUpperLeft.z));
            particleArray[i].position.w = 1;

            particleArray[i].velocity.x = UnityEngine.Random.Range(.01f, .1f);
            particleArray[i].velocity.y = UnityEngine.Random.Range(.01f, .1f);
            particleArray[i].velocity.z = UnityEngine.Random.Range(.01f, .1f);
            particleArray[i].velocity.w = 0;
        }
        particleBuffer.SetData(particleArray);
        kernelID = compute.FindKernel("CSMain");
        compute.SetBuffer(kernelID, "particleBuffer", particleBuffer);
        compute.SetTexture(kernelID, "heightMap", heightMap);
        CreateMesh(particleArray);
    }

    // Update is called once per frame
    void Update()
    {
        Debug.DrawLine(heightMapBottomLeft, heightMapBottomRight, Color.red);
        Debug.DrawLine(heightMapBottomLeft, heightMapUpperLeft, Color.green);
        Debug.DrawLine(heightMapUpperLeft, heightMapUpperRight, Color.blue);
        Debug.DrawLine(heightMapBottomRight, heightMapUpperRight, Color.yellow);
        //In theory, setting time from C# is the same as the built in time value of the vertex shader: https://forum.unity.com/threads/global-shader-variables-in-compute-shaders.471211/

        compute.SetFloat("_time", Time.realtimeSinceStartup);
        compute.Dispatch(kernelID, 1000, 1, 1);
        particleBuffer.GetData(output);
        List<Vector3> pointArray = new List<Vector3>();
        for (int i = 0; i < numberOfParticles; i++)
        {
            pointArray.Add(new Vector3(output[i].position.x, output[i].position.y + trianglePointSize, output[i].position.z));
            pointArray.Add(new Vector3(output[i].position.x + trianglePointSize, output[i].position.y - trianglePointSize, output[i].position.z));
            pointArray.Add(new Vector3(output[i].position.x - trianglePointSize, output[i].position.y - trianglePointSize, output[i].position.z));
        }
        mesh.SetVertices(pointArray);
        //Debug.Log(output[0].position.x);
    }


    void particleUpdateFromCPU()
    {

    }

    void OnDestroy()

    {

        if (particleBuffer != null)

            particleBuffer.Release();

    }

    //Source: http://www.kamend.com/2014/05/rendering-a-point-cloud-inside-unity/
    void CreateMesh(Particle[] points)
    {
        Vector3[] pointArray = new Vector3[numberOfParticles * 3];
        int[] indecies = new int[numberOfParticles * 3];
        Color[] colors = new Color[numberOfParticles * 3];
        for (int i = 0; i < points.Length; ++i)
        {
            pointArray[i * 3] = new Vector3((float)(points[i].position.x), (float)(points[i].position.y + trianglePointSize), (float)(points[i].position.z));
            pointArray[i * 3 + 1] = new Vector3((float)(points[i].position.x + trianglePointSize), (float)(points[i].position.y - trianglePointSize), (float)(points[i].position.z));
            pointArray[i * 3 + 2] = new Vector3((float)(points[i].position.x - trianglePointSize), (float)(points[i].position.y - trianglePointSize), (float)(points[i].position.z));

            indecies[i * 3] = i * 3;
            indecies[i * 3 + 1] = i * 3 + 1;
            indecies[i * 3 + 2] = i * 3 + 2;

            colors[i * 3] = new Color(.0f, .1f, 1.0f, 1.0f);
            colors[i * 3 + 1] = new Color(.1f, 0f, 1.0f, 1.0f);
            colors[i * 3 + 2] = new Color(0f, 0f, 1f, 1.0f);
        }

        mesh.vertices = pointArray;
        mesh.colors = colors;
        mesh.SetIndices(indecies, MeshTopology.Triangles, 0);

    }
}