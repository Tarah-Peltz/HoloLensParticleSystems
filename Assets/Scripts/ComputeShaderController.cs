using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ComputeShaderController : MonoBehaviour {
    ComputeBuffer particleBuffer;
    int kernelID;
    public ComputeShader compute;
    public int numberOfParticles = 1000;
    public int particleSize = 24;
    public int threadGroupsX = 8;
    public float trianglePointSize = .02f;
    //public Material material;
    private Mesh mesh;
    Particle[] output;
    //public TextMesh displayText;


    private struct Particle
    {
        public Vector3 position;
        public float speed;
        public float radius;
    }

    // Use this for initialization
    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        particleBuffer = new ComputeBuffer(numberOfParticles, particleSize);
        Particle[] particleArray = new Particle[numberOfParticles];
        output = new Particle[numberOfParticles];

        for (int i = 0; i < numberOfParticles; ++i)
        {
            particleArray[i].position.x = UnityEngine.Random.Range(-5f, 5f);
            particleArray[i].position.y = UnityEngine.Random.Range(-2f, 2f);
            particleArray[i].position.z = UnityEngine.Random.Range(-5f, 5f);

            particleArray[i].speed = UnityEngine.Random.Range(5, 20);

            particleArray[i].radius = UnityEngine.Random.Range(5, 20);
        }
        particleBuffer.SetData(particleArray);
        kernelID = compute.FindKernel("CSMain");
        compute.SetBuffer(kernelID, "particleBuffer", particleBuffer);
        CreateMesh(particleArray);
    }

    // Update is called once per frame
    void Update()
    {
        //In theory, setting time from C# is the same as the built in time value of the vertex shader: https://forum.unity.com/threads/global-shader-variables-in-compute-shaders.471211/

        compute.SetFloat("_Time", Time.realtimeSinceStartup);
        compute.Dispatch(kernelID, 1, 1, 1);
        particleBuffer.GetData(output);
        List<Vector3> pointArray = new List<Vector3>();
        for (int i = 0; i < numberOfParticles; i++)
        {
            pointArray.Add(new Vector3(output[i].position.x, output[i].position.y + trianglePointSize, output[i].position.z));
            pointArray.Add(new Vector3(output[i].position.x + trianglePointSize, output[i].position.y - trianglePointSize, output[i].position.z));
            pointArray.Add(new Vector3(output[i].position.x - trianglePointSize, output[i].position.y - trianglePointSize, output[i].position.z));
        }
        mesh.SetVertices(pointArray);
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

            colors[i * 3] = new Color(.8f, .9f, 1.0f, 1.0f);
            colors[i * 3 + 1] = new Color(.9f, 1.0f, 1.0f, 1.0f);
            colors[i * 3 + 2] = new Color(.8f, .8f, 1f, 1.0f);
        }

        mesh.vertices = pointArray;
        mesh.colors = colors;
        mesh.SetIndices(indecies, MeshTopology.Triangles, 0);

    }
}