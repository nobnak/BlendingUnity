using UnityEngine;

namespace nobnak.Blending
{
    [RequireComponent(typeof(Camera))]
    public class BlenderPerCamera : BlenderBase
    {
        Camera _camera;

        protected override void Start()
        {
            configFileName = "Blending_" + name + ".txt";

            base.Start();

            _camera = GetComponent<Camera>();
            var tex = _camera.targetTexture;
            _blendCamera.targetTexture = new RenderTexture(tex.width, tex.height, tex.depth);
            _maskCamera.targetTexture = new RenderTexture(tex.width, tex.height, tex.depth);
            _occlusionCamera.targetTexture = new RenderTexture(tex.width, tex.height, tex.depth);
        }

        #region abstract

        protected override Texture GetCaptureTex()
        {
            return _camera.targetTexture;
        }

        protected override Texture GetBlendedTex()
        {
            return _blendCamera.targetTexture;
        }

        protected override Texture GetMaskedTex()
        {
            return _maskCamera.targetTexture;
        }

        #endregion


        // 最終出力
        public RenderTexture GetTexture()
        {
            return _occlusionCamera.targetTexture;
        }
    }
}