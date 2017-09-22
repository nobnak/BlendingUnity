using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Blending2 {
    
    [ExecuteInEditMode]
    public class TestMatrixOnRenderImage : MonoBehaviour {
        public const string PROP_MATRIX = "_Matrix";
        
        [SerializeField]
        Material mat;

        Mesh mesh;

        void OnEnable() {
            mesh = UVQuad.Generate (mesh);
        }
        void OnRenderImage(RenderTexture src, RenderTexture dst) {
            using (new ScopedRenderTexture (dst)) {
                if (dst != null)
                    dst.DiscardContents ();
                GL.Clear (true, true, Color.clear);

                mat.SetMatrix (PROP_MATRIX, UVMatrix());
                mat.SetPass (0);
                Graphics.DrawMeshNow (mesh, Matrix4x4.identity);
            }
        }
        void OnDisable() {
            ObjectDestruction.Release (mesh);
        }

        Matrix4x4 UVMatrix() {
            var m = Matrix4x4.zero;
            m [0] = 2f;     m [4] = 0f;     m [8] = 0f;     m [12] = -1f;
            m [1] = 0f;     m [5] = 2f;     m [9] = 0f;     m [13] = -1f;
            m [2] = 0f;     m [6] = 0f;     m [10] = 1f;    m [14] = 0f;
            m [3] = 0f;     m [7] = 0f;     m [11] = 0f;    m [15] = 1f;
            return m;
        }
    }
}
