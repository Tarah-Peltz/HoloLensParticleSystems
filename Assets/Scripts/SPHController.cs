using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class SPHController : MonoBehaviour {
    ComputeBuffer particleBuffer;
    ComputeBuffer vertexBuffer;
    CommandBuffer commandBuffer;
    int kernelID1;
    int kernelID2;
    int kernelID3;
    public ComputeShader densityPressureCompute;
    public ComputeShader forceCompute;
    public ComputeShader integrateCompute;
    public int numberOfParticles = 1000;
    public float trianglePointSize = .02f;
    public float restingDensity = 1000f;
    public float mass = .02f;
    public float smoothingLengthMultiplier = 4f;
    public float stiffness = 2000f;
    public float viscosity = 3000f;
    public float boundaryDamping = .3f;
    public Material waterMaterial;
    //public Material material;
    private Mesh mesh;
    Particle[] output;
    //public TextMesh displayText;


    private struct Particle
    {
        //For memory efficiency, the w component of position shall be density
        public Vector4 positionAndDensity;
        //For memory efficiency, the w component of velocity shall be pressure
        public Vector4 velocityAndPressure;
        public Vector4 force;
    }

    // Use this for initialization
    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        particleBuffer = new ComputeBuffer(numberOfParticles, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Particle)));
        vertexBuffer = new ComputeBuffer(numberOfParticles * 3, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector4)));
        Particle[] particleArray = new Particle[numberOfParticles];
        Vector4[] vertexArray = new Vector4[numberOfParticles * 3];
        output = new Particle[numberOfParticles];

        for (int i = 0; i < numberOfParticles; ++i)
        {
            particleArray[i].positionAndDensity.x = UnityEngine.Random.Range(-3f, 3f);
            particleArray[i].positionAndDensity.y = UnityEngine.Random.Range(2f, 5f);
            particleArray[i].positionAndDensity.z = UnityEngine.Random.Range(-3f, 3f);
            particleArray[i].positionAndDensity.w = 1;

            vertexArray[i * 3] = new Vector4((float)(particleArray[i].positionAndDensity.x), (float)(particleArray[i].positionAndDensity.y + trianglePointSize), (float)(particleArray[i].positionAndDensity.z), 1.0f);
            vertexArray[i * 3 + 1] = new Vector4((float)(particleArray[i].positionAndDensity.x + trianglePointSize), (float)(particleArray[i].positionAndDensity.y - trianglePointSize), (float)(particleArray[i].positionAndDensity.z), 1.0f);
            vertexArray[i * 3 + 2] = new Vector4((float)(particleArray[i].positionAndDensity.x - trianglePointSize), (float)(particleArray[i].positionAndDensity.y - trianglePointSize), (float)(particleArray[i].positionAndDensity.z), 1.0f);

            particleArray[i].velocityAndPressure.x = UnityEngine.Random.Range(0f, 200f);
            particleArray[i].velocityAndPressure.y = 0f;
            particleArray[i].velocityAndPressure.z = UnityEngine.Random.Range(0f, 200f);
            particleArray[i].velocityAndPressure.w = 0;

            particleArray[i].force.x = 1000f;
            particleArray[i].force.y = 0f;
            particleArray[i].force.z = UnityEngine.Random.Range(.01f, .1f);
            particleArray[i].force.w = 1;
        }
        particleBuffer.SetData(particleArray);
        vertexBuffer.SetData(vertexArray);
        kernelID1 = densityPressureCompute.FindKernel("CSMain");
        kernelID2 = forceCompute.FindKernel("CSMain");
        kernelID3 = integrateCompute.FindKernel("CSMain");
        densityPressureCompute.SetBuffer(kernelID1, "particleBuffer", particleBuffer);
        forceCompute.SetBuffer(kernelID2, "particleBuffer", particleBuffer);
        integrateCompute.SetBuffer(kernelID3, "particleBuffer", particleBuffer);
        integrateCompute.SetBuffer(kernelID3, "vertexBuffer", vertexBuffer);
        CreateMesh(particleArray);

        densityPressureCompute.SetFloat("_time", Time.realtimeSinceStartup);
        densityPressureCompute.SetFloat("_restingDensity", restingDensity);
        densityPressureCompute.SetFloat("_particleMass", mass);
        densityPressureCompute.SetFloat("_smoothingLength", (float)smoothingLengthMultiplier * trianglePointSize);
        densityPressureCompute.SetFloat("_stiffness", stiffness);
        densityPressureCompute.SetInt("_numberOfParticles", numberOfParticles);
        densityPressureCompute.Dispatch(kernelID1, numberOfParticles / 8, 1, 1);

        forceCompute.SetFloat("_time", Time.realtimeSinceStartup);
        forceCompute.SetFloat("_particleMass", mass);
        forceCompute.SetFloat("_smoothingLength", (float)smoothingLengthMultiplier * trianglePointSize);
        forceCompute.SetInt("_numberOfParticles", numberOfParticles);
        forceCompute.SetFloat("_viscosity", viscosity);
        forceCompute.Dispatch(kernelID1, numberOfParticles / 8, 1, 1);

        integrateCompute.SetFloat("_timeStep", .0001f);
        integrateCompute.SetInt("_numberOfParticles", numberOfParticles);
        integrateCompute.SetFloat("_boundaryDamping", boundaryDamping);
        integrateCompute.SetFloat("_particleSize", trianglePointSize);
        integrateCompute.Dispatch(kernelID1, numberOfParticles / 8, 1, 1);
    }

    // Update is called once per frame
    void Update()
    {
        //In theory, setting time from C# is the same as the built in time value of the vertex shader: https://forum.unity.com/threads/global-shader-variables-in-compute-shaders.471211/

        densityPressureCompute.SetFloat("_time", Time.realtimeSinceStartup);
        densityPressureCompute.SetFloat("_restingDensity", restingDensity);
        densityPressureCompute.SetFloat("_particleMass", mass);
        densityPressureCompute.SetFloat("_smoothingLength", (float)smoothingLengthMultiplier*trianglePointSize);
        densityPressureCompute.SetFloat("_stiffness", stiffness);
        densityPressureCompute.SetInt("_numberOfParticles", numberOfParticles);
        densityPressureCompute.Dispatch(kernelID1, numberOfParticles / 8, 1, 1);

        forceCompute.SetFloat("_time", Time.realtimeSinceStartup);
        forceCompute.SetFloat("_particleMass", mass);
        forceCompute.SetFloat("_smoothingLength", (float)smoothingLengthMultiplier * trianglePointSize);
        forceCompute.SetInt("_numberOfParticles", numberOfParticles);
        forceCompute.SetFloat("_viscosity", viscosity);
        forceCompute.Dispatch(kernelID1, numberOfParticles / 8, 1, 1);

        integrateCompute.SetFloat("_timeStep", .0001f);
        integrateCompute.SetInt("_numberOfParticles", numberOfParticles);
        integrateCompute.SetFloat("_boundaryDamping", boundaryDamping);
        integrateCompute.SetFloat("_particleSize", trianglePointSize);
        integrateCompute.Dispatch(kernelID1, numberOfParticles / 8, 1, 1);

        if (Time.frameCount % 10 == 0) particleBuffer.GetData(output);
        List<Vector3> pointArray = new List<Vector3>();
        for (int i = 0; i < numberOfParticles; i++)
        {
            pointArray.Add(new Vector3(output[i].positionAndDensity.x, output[i].positionAndDensity.y + trianglePointSize, output[i].positionAndDensity.z));
            pointArray.Add(new Vector3(output[i].positionAndDensity.x + trianglePointSize, output[i].positionAndDensity.y - trianglePointSize, output[i].positionAndDensity.z));
            pointArray.Add(new Vector3(output[i].positionAndDensity.x - trianglePointSize, output[i].positionAndDensity.y - trianglePointSize, output[i].positionAndDensity.z));
        }
        mesh.SetVertices(pointArray);
        Matrix4x4 transformationMatrix = Matrix4x4.identity;
        commandBuffer = new CommandBuffer(); //Example online creates a new one each pass. Is this necessary? Does it impact efficiency?
        commandBuffer.DrawProceduralIndirect(transformationMatrix, waterMaterial, -1, MeshTopology.Triangles, vertexBuffer, 0, null);
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
            pointArray[i * 3] = new Vector3((float)(points[i].positionAndDensity.x), (float)(points[i].positionAndDensity.y + trianglePointSize), (float)(points[i].positionAndDensity.z));
            pointArray[i * 3 + 1] = new Vector3((float)(points[i].positionAndDensity.x + trianglePointSize), (float)(points[i].positionAndDensity.y - trianglePointSize), (float)(points[i].positionAndDensity.z));
            pointArray[i * 3 + 2] = new Vector3((float)(points[i].positionAndDensity.x - trianglePointSize), (float)(points[i].positionAndDensity.y - trianglePointSize), (float)(points[i].positionAndDensity.z));

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
