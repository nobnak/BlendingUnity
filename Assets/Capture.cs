using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class Capture : MonoBehaviour {
	public int depth = 90;

	private RenderTexture _rtex;

	void OnDisable() {
		Destroy(_rtex);
	}
	void OnEnable() {
		camera.cullingMask = 0;
		camera.depth = depth;
		camera.clearFlags = CameraClearFlags.Nothing;
	}
	void OnRenderImage(RenderTexture src, RenderTexture dst) {
		if (_rtex == null)
			return;
		_rtex.DiscardContents();
		Graphics.Blit(src, _rtex);
	}

	public RenderTexture GetTarget() {
		if (_rtex == null || _rtex.width != Screen.width || _rtex.height != Screen.height) {
			Destroy (_rtex);
			_rtex = new RenderTexture (Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
			_rtex.wrapMode = TextureWrapMode.Clamp;
		}
		return _rtex;
	}
}
