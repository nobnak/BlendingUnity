using UnityEngine;
using System.Collections;

namespace nobnak.Blending
{

    public class TextureSetter : MonoBehaviour
    {
        public Camera _targetCamera;
        public int width = 1920;
        public int height = 1080;

        BlenderPerCamera _blender;
        Material _material;

        void Awake()
        {
            _targetCamera.targetTexture = new RenderTexture(width, height, 0);
            _blender = _targetCamera.GetComponent<BlenderPerCamera>();
            _material = GetComponent<Renderer>().material;
        }

        public void Update()
        {
            _material.mainTexture = _blender.GetTexture();
        }

        public void OnDestroy()
        {
            Destroy(_material);
        }
    }

}
