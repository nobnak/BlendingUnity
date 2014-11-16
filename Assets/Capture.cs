using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class Capture : MonoBehaviour {
	public int depth = 90;
	public RenderTexture Target { get; private set; }

	void OnDestroy() {
		Destroy(Target);
	}
	void Awake() {
		CheckInit();
	}
	void Update() {
		CheckInit();
	}
	void OnRenderImage(RenderTexture src, RenderTexture dst) {
		Target.DiscardContents();
		Graphics.Blit(src, Target);
	}

	void CheckInit() {
		if (Target == null || Target.width != Screen.width || Target.height != Screen.height) {
			Destroy (Target);
			Target = new RenderTexture (Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
			Target.wrapMode = TextureWrapMode.Clamp;
		}
		camera.cullingMask = 0;
		camera.depth = depth;
		camera.clearFlags = CameraClearFlags.Nothing;
	}
}
