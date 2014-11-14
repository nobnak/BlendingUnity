using UnityEngine;
using System.Collections;

namespace nobnak.Render {

	[RequireComponent(typeof(Camera))]
	public class BlitReader : MonoBehaviour {
		public int mipLevel = 0;
		public Material blitMat;

		private RenderTexture _rtex;

		public Texture2D Tex { get; private set; }
		
		void OnDestroy() {
			ClearTextures();
		}
		void OnPreRender() {
			CheckTextures();
		}
		void OnRenderImage(RenderTexture src, RenderTexture dst) {
			Graphics.Blit(null, dst, blitMat);
		}
		void OnPostRender() {
			RenderTexture.active = _rtex;
			Tex.ReadPixels(new Rect(0, 0, _rtex.width, _rtex.height), 0, 0);
			Tex.Apply();
			RenderTexture.active = null;

			enabled = false;
		}

		public IEnumerator Render() {
			enabled = true;
			while (enabled)
				yield return null;
		}

		void CheckTextures() {
			var sizeDiv = 1 << mipLevel;
			var width = Screen.width / sizeDiv;
			var height = Screen.height / sizeDiv;

			if (_rtex == null || _rtex.width != width || _rtex.height != height) {
				ClearTextures();
				_rtex = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
				Tex = new Texture2D(width, height, TextureFormat.ARGB32, false, true);

				_rtex.wrapMode = TextureWrapMode.Clamp;
				Tex.wrapMode = TextureWrapMode.Clamp;

				camera.targetTexture = _rtex;
			}
		}
		void ClearTextures() {
			Destroy(_rtex);
			Destroy(Tex);
		}

		public static BlitReader Generate(Transform parent, Material blitMat, int mipLevel) {
			var go = new GameObject();
			go.transform.parent = parent;

			var cam = go.AddComponent<Camera>();
			cam.cullingMask = 0;
			cam.clearFlags = CameraClearFlags.SolidColor;
			cam.backgroundColor = Color.clear;

			var reader = go.AddComponent<BlitReader>();
			reader.blitMat = blitMat;
			reader.mipLevel = Mathf.Max(0, mipLevel);
			reader.enabled = false;
			return reader;
		}
	}
}