using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class SPHController : MonoBehaviour {
    ComputeBuffer particleBuffer;
    public Texture2D heightMap;
    public GameObject heightMappedObject;
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
    public int mode;

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
        //For memory efficiency, the w component of position shall be density
        public Vector4 positionAndDensity;
        //For memory efficiency, the w component of velocity shall be pressure
        public Vector4 velocityAndPressure;
        public Vector4 forceAndIsStatic;
    }

    Bounds b;

    // Use this for initialization
    void Start()
    {
        heightMap = rotateTexture(heightMap, true);
        heightMap = rotateTexture(heightMap, true);
        b = heightMappedObject.GetComponent<Renderer>().bounds;
        heightMapBottomLeft = new Vector3(b.center.x - b.extents.x, 0f, b.center.z - b.extents.z);
        heightMapBottomRight = new Vector3(b.center.x + b.extents.x, 0f, b.center.z - b.extents.z);
        heightMapUpperLeft = new Vector3(b.center.x - b.extents.x, 0f, b.center.z + b.extents.z);
        heightMapUpperRight = new Vector3(b.center.x + b.extents.x, 0f, b.center.z + b.extents.z);
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        particleBuffer = new ComputeBuffer(numberOfParticles, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Particle)));
        Particle[] particleArray = new Particle[numberOfParticles];
        Vector4[] vertexArray = new Vector4[numberOfParticles * 3];
        output = new Particle[numberOfParticles];

        for (int i = 0; i < numberOfParticles; ++i)
        {
            /*particleArray[i].positionAndDensity.x = UnityEngine.Random.Range(-3f, 3f);
            particleArray[i].positionAndDensity.y = UnityEngine.Random.Range(2f, 5f);
            particleArray[i].positionAndDensity.z = UnityEngine.Random.Range(-3f, 3f);
            particleArray[i].positionAndDensity.w = 1;*/

            particleArray[i].positionAndDensity.x = UnityEngine.Random.Range((float)(heightMapBottomLeft.x), (float)(heightMapBottomRight.x));
            particleArray[i].positionAndDensity.y = 1f;
            particleArray[i].positionAndDensity.z = UnityEngine.Random.Range((float)(heightMapBottomLeft.z), (float)(heightMapUpperLeft.z));
            particleArray[i].positionAndDensity.w = 1;

            particleArray[i].velocityAndPressure.x = UnityEngine.Random.Range(0f, 200f);
            particleArray[i].velocityAndPressure.y = 0f;
            particleArray[i].velocityAndPressure.z = UnityEngine.Random.Range(0f, 200f);
            particleArray[i].velocityAndPressure.w = 0;

            particleArray[i].forceAndIsStatic.x = 1000f;
            particleArray[i].forceAndIsStatic.y = 0f;
            particleArray[i].forceAndIsStatic.z = UnityEngine.Random.Range(.01f, .1f);
            particleArray[i].forceAndIsStatic.w = 0.0f;
        }

        heightMappedObject.transform.position = new Vector3(heightMappedObject.transform.position.x, heightMappedObject.transform.position.y, heightMappedObject.transform.position.z + 3f);

        particleBuffer.SetData(particleArray);
        kernelID1 = densityPressureCompute.FindKernel("CSMain");
        kernelID2 = forceCompute.FindKernel("CSMain");
        kernelID3 = integrateCompute.FindKernel("CSMain");
        densityPressureCompute.SetBuffer(kernelID1, "particleBuffer", particleBuffer);
        forceCompute.SetBuffer(kernelID2, "particleBuffer", particleBuffer);
        integrateCompute.SetBuffer(kernelID3, "particleBuffer", particleBuffer);
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
        integrateCompute.SetFloat("textureXLength", 2 * b.extents.x);
        integrateCompute.SetFloat("textureZLength", 2 * b.extents.z);
        integrateCompute.SetVector("textureCenter", b.center);
        integrateCompute.SetInt("mode", mode);

        integrateCompute.Dispatch(kernelID1, numberOfParticles / 8, 1, 1);

    }


    //https://answers.unity.com/questions/951835/rotate-the-contents-of-a-texture.html
    Texture2D rotateTexture(Texture2D originalTexture, bool clockwise)
    {
        Color32[] original = originalTexture.GetPixels32();
        Color32[] rotated = new Color32[original.Length];
        int w = originalTexture.width;
        int h = originalTexture.height;

        int iRotated, iOriginal;

        for (int j = 0; j < h; ++j)
        {
            for (int i = 0; i < w; ++i)
            {
                iRotated = (i + 1) * h - j - 1;
                iOriginal = clockwise ? original.Length - 1 - (j * w + i) : j * w + i;
                rotated[iRotated] = original[iOriginal];
            }
        }

        Texture2D rotatedTexture = new Texture2D(h, w);
        rotatedTexture.SetPixels32(rotated);
        rotatedTexture.Apply();
        return rotatedTexture;
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
        integrateCompute.SetFloat("textureXLength", 2 * b.extents.x);
        integrateCompute.SetFloat("textureZLength", 2 * b.extents.z);
        integrateCompute.SetVector("textureCenter", b.center);
        integrateCompute.SetInt("mode", mode);
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
