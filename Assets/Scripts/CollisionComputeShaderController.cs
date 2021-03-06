﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CollisionComputeShaderController : MonoBehaviour {
    ComputeBuffer particleBuffer;
    int kernelID;
    public ComputeShader compute;
    public int numberOfParticles = 1000;
    public float trianglePointSize = .02f;
    public float spherePositionX = 0f;
    public float spherePositionY = 0f;
    public float spherePositionZ = 0f;
    public float sphereRadius = 1f;
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
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        particleBuffer = new ComputeBuffer(numberOfParticles, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Particle)));
        Particle[] particleArray = new Particle[numberOfParticles];
        output = new Particle[numberOfParticles];

        for (int i = 0; i < numberOfParticles; ++i)
        {
            particleArray[i].position.x = UnityEngine.Random.Range((float)(spherePositionX - sphereRadius - .1), (float)(spherePositionX + sphereRadius + .1));
            particleArray[i].position.y = UnityEngine.Random.Range((float)(spherePositionY + 2*sphereRadius), (float)(spherePositionY + 3*sphereRadius));
            particleArray[i].position.z = UnityEngine.Random.Range((float)(spherePositionZ - sphereRadius - .1), (float)(spherePositionZ + sphereRadius + .1));
            particleArray[i].position.w = 1;

            particleArray[i].velocity.x = UnityEngine.Random.Range(.01f, .1f);
            particleArray[i].velocity.y = UnityEngine.Random.Range(.01f, .1f);
            particleArray[i].velocity.z = UnityEngine.Random.Range(.01f, .1f);
            particleArray[i].velocity.w = 0;
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

        compute.SetFloat("_time", Time.realtimeSinceStartup);
        compute.SetFloat("_SpherePositionX", spherePositionX);
        compute.SetFloat("_SpherePositionY", spherePositionY);
        compute.SetFloat("_SpherePositionZ", spherePositionZ);
        compute.SetFloat("_SphereRadius", sphereRadius);
        compute.Dispatch(kernelID, numberOfParticles/8, 1, 1);
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