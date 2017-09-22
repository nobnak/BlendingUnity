using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Blending2 {

    public class Blending : MonoBehaviour {

        [SerializeField]
        Shader shader;

        protected Material mat;

        void OnEnable() {
            mat = new Material (shader);
        }
        void OnRenderImage(RenderTexture src, RenderTexture dst) {
            using (new ScopedRenderTexture (dst)) {
                
            }
        }
        void OnDisable() {
            ObjectDestruction.Release (mat);
        }
    }
}
