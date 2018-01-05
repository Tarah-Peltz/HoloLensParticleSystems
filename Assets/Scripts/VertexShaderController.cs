using UnityEngine;
using System.Collections;

public class VertexShaderController : MonoBehaviour {
    public int numberOfParticles = 1000;
    public float trianglePointSize = .02f;
    Mesh mesh;
    float circleCounter = 0f;

    private struct Particle
    {
        public Vector3 position;
        public float speed;
        public float radius;
    }

    // Use this for initialization
    void Start () {
 
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        Particle[] particleArray = new Particle[numberOfParticles];
        for (int i = 0; i < numberOfParticles; ++i)
        {
            particleArray[i].position.x = UnityEngine.Random.Range(-5f, 5f);
            particleArray[i].position.y = UnityEngine.Random.Range(-2f, 2f);
            particleArray[i].position.z = UnityEngine.Random.Range(-5f, 5f);

            particleArray[i].speed = UnityEngine.Random.Range(5, 20);

            particleArray[i].radius = UnityEngine.Random.Range(5, 20);
        }
        CreateMesh(particleArray);
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

            colors[i * 3] = new Color(1f, .1f, 0f, 1.0f);
            colors[i * 3 + 1] = new Color(1f, 0f, .1f, 1.0f);
            colors[i * 3 + 2] = new Color(1f, 0f, 0f, 1.0f);
        }

        mesh.vertices = pointArray;
        mesh.colors = colors;
        mesh.SetIndices(indecies, MeshTopology.Triangles, 0);

    }
}
