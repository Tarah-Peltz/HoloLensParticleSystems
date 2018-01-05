using UnityEngine;
using System.Collections;

public class RotatingParticlesVertexShaderSceneManager : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        if (Time.timeSinceLevelLoad >= 10)
        {
            Application.LoadLevel("RotatingObjectComputeShader");
        }
    }
}
