using Gist.Extensions.Array;
using Gist.GPUBuffer;
using Gist.Scoped;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Blending2 {

    [ExecuteInEditMode]
    public class Blending : MonoBehaviour {
        public const string PROP_INDICES = "_Indices";
        public const string PROP_UVS = "_Uvs";
        public const string PROP_CORNER_MATRICES = "_CornerMatrices";

        public Data data;

        [SerializeField]
        protected Shader shader;

        protected ScopedObject<Material> mat;
        protected GPUList<int> indices;
        protected GPUList<Vector2> uvs;
        protected GPUList<Matrix4x4> cornerMatrices;

        #region Unity
        void OnEnable() {
            mat = new Material(shader);

            indices = CreateIndices();
            uvs = CreateUVs();
            cornerMatrices = CreateCornerMatrices();
        }

        void OnRenderImage(RenderTexture src, RenderTexture dst) {
            using (new ScopedRenderTextureActivator (dst)) {
                indices.Upload();
                uvs.Upload();
                cornerMatrices.Upload();

                mat.Data.SetBuffer(PROP_INDICES, indices.Buffer);
                mat.Data.SetBuffer(PROP_UVS, uvs.Buffer);
                mat.Data.SetBuffer(PROP_CORNER_MATRICES, cornerMatrices.Buffer);

                mat.Data.SetPass(0);
                Graphics.DrawProcedural(MeshTopology.Triangles, 6);
            }
        }
        void OnDisable() {
            mat.Dispose();
            indices.Dispose();
            uvs.Dispose();
            cornerMatrices.Dispose();
        }
        #endregion

        private GPUList<int> CreateIndices(GPUList<int> indices = null) {
            var length = 6;
            if (indices == null)
                indices = new GPUList<int>(length);
            indices.Clear();

            indices.Add(0);
            indices.Add(3);
            indices.Add(1);
            indices.Add(0);
            indices.Add(2);
            indices.Add(3);
            return indices;
        }
        private GPUList<Vector2> CreateUVs(GPUList<Vector2> uvs = null) {
            var length = 4;
            if (uvs == null)
                uvs = new GPUList<Vector2>(length);
            uvs.Clear();

            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(0f, 1f));
            uvs.Add(new Vector2(1f, 1f));
            return uvs;
        }
        private Matrix4x4 CreateCornerMatrix() {
            var m = Matrix4x4.zero;
            m[0] = 0f;      m[4] = 1f;     m[8] = 0f;      m[12] = 1f;
            m[1] = 0f;      m[5] = 0f;      m[9] = 1f;      m[13] = 1f;
            return m;
        }
        private GPUList<Matrix4x4> CreateCornerMatrices(GPUList<Matrix4x4> matrices = null) {
            var length = 1;
            if (matrices == null)
                matrices = new GPUList<Matrix4x4>(length);
            matrices.Clear();

            matrices.Add(CreateCornerMatrix());
            return matrices;
        }
    }
}
