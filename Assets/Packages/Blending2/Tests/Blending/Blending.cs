using Gist.Extensions.Array;
using Gist.GPUBuffer;
using Gist.Scoped;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Blending2 {

    [ExecuteInEditMode]
    public class Blending : MonoBehaviour {
        public const string PROP_MAIN_TEX = "_MainTex";
        public const string PROP_INDICES = "_Indices";
        public const string PROP_UVS = "_Uvs";
        public const string PROP_CORNER_TO_WORLD_MATRICES = "_CornerToWorldMatrices";
        public const string PROP_WORLD_TO_SCREEN_MATRIX = "_WorldToScreenMatrix";

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
            cornerMatrices = new GPUList<Matrix4x4>();
        }

        void OnRenderImage(RenderTexture src, RenderTexture dst) {
            using (new ScopedRenderTextureActivator (dst)) {
                 UpdateCornerMatrices(ref cornerMatrices);

                indices.Upload();
                uvs.Upload();
                cornerMatrices.Upload();

                mat.Data.SetTexture(PROP_MAIN_TEX, src);
                mat.Data.SetBuffer(PROP_INDICES, indices.Buffer);
                mat.Data.SetBuffer(PROP_UVS, uvs.Buffer);
                mat.Data.SetBuffer(PROP_CORNER_TO_WORLD_MATRICES, cornerMatrices.Buffer);
                mat.Data.SetMatrix(PROP_WORLD_TO_SCREEN_MATRIX, CreateWorldToScreenMatrix());

                var screen = data.screens;
                var screenCount = screen.x * screen.y;
                mat.Data.SetPass(0);
                Graphics.DrawProcedural(MeshTopology.Triangles, 6, screenCount);
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
        private Matrix4x4 CreateCornerMatrix(int x, int y, Vector2 p00, Vector2 p10, Vector2 p01, Vector2 p11) {
            var m = Matrix4x4.zero;
            m[0] = x + p00.x;      m[4] = x + p10.x + 1f;     m[8] = x + p01.x;         m[12] = x + p11.x + 1f;
            m[1] = y + p00.y;      m[5] = y + p10.y;            m[9] = y + p01.y + 1f;  m[13] = y + p11.y + 1f;
            m[15] = 1f;
            return m;
        }
        private void UpdateCornerMatrices(ref GPUList<Matrix4x4> matrices) {
            var length = 1;
            if (matrices == null)
                matrices = new GPUList<Matrix4x4>(length);
            matrices.Clear();

            var screens = data.screens;
            for (var y = 0; y < screens.y; y++)
                for (var x = 0; x < screens.x; x++)
                    matrices.Add(CreateCornerMatrix(x, y, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero));
        }
        private Matrix4x4 CreateWorldToScreenMatrix() {
            var screens = data.screens;
            var m = Matrix4x4.zero;
            m[0] = 2f / screens.x; m[12] = -1f;
            m[5] = 2f / screens.y; m[13] = -1f;
            m[15] = 1f;
            return m;
        }
    }
}
