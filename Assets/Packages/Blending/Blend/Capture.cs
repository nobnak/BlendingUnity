using UnityEngine;
using System.Collections;

namespace nobnak.Blending {

	[RequireComponent(typeof(Camera))]
	public class Capture : MonoBehaviour {

        private int? _width;
        private int? _height;
        public int width { get { return _width ?? Screen.width; } set { _width = value; } }
        public int height { get { return _height ?? Screen.height; } set { _height = value; } }

        private RenderTexture _rtex;

		void OnDisable() {
			Destroy(_rtex);
			_rtex = null;
		}
		void Awake() {
			GetComponent<Camera>().cullingMask = 0;
			GetComponent<Camera>().clearFlags = CameraClearFlags.Nothing;
		}
		void OnRenderImage(RenderTexture src, RenderTexture dst) {
			if (_rtex == null)
				return;
			_rtex.DiscardContents();
			Graphics.Blit(src, _rtex);
		}

		public RenderTexture GetTarget() {
			if (_rtex == null || _rtex.width != width || _rtex.height != height) {
				Destroy (_rtex);
				_rtex = new RenderTexture (width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
				_rtex.wrapMode = TextureWrapMode.Clamp;
			}
			return _rtex;
		}
	}
}
