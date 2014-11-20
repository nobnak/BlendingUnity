using UnityEngine;
using System.Collections;

public class Test : MonoBehaviour {
	public Capture[] captures;
	public RenderTexture[] targets;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		targets = new RenderTexture[captures.Length];
		for (var i = 0; i < captures.Length; i++)
			targets[i] = captures[i].GetTarget();
	}
}
