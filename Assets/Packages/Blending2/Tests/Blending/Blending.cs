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
        public const string PROP_WORLD_TO_SCREEN_MATRIX = "_WorldToScreenMatrix";
        public const string PROP_UV_TO_WORLD_MATRICES = "_UVToWorldMatrices";
        public const string PROP_EDGE_TO_LOCAL_UV_MATRICES = "_EdgeToLocalUVMatrices";
        public const string PROP_LOCAL_TO_WORLD_UV_MATRICES = "_LocalToWorldUVMatrices";

        [SerializeField]
        protected Data data;

        [SerializeField]
        protected Shader shader;

        protected ScopedObject<Material> mat;
        protected UvToWorldMatrix worldMatrix;
        protected EdgeToLocalUvMatrix edgeMatrices;
        protected LocalToWorldUvMatrix uvMatrix;

        #region Unity
        void OnEnable() {
            mat = new Material(shader);
            worldMatrix = new UvToWorldMatrix();
            edgeMatrices = new EdgeToLocalUvMatrix();
            uvMatrix = new LocalToWorldUvMatrix();

            UpdateInputData();
        }

        void OnRenderImage(RenderTexture src, RenderTexture dst) {
            using (new ScopedRenderTextureActivator (dst)) {
                if (data.MakeValidated())
                    UpdateInputData();

                mat.Data.SetTexture(PROP_MAIN_TEX, src);
                mat.Data.SetBuffer(PROP_UV_TO_WORLD_MATRICES, worldMatrix.Buffer);
                mat.Data.SetBuffer(PROP_EDGE_TO_LOCAL_UV_MATRICES, edgeMatrices.Buffer);
                mat.Data.SetBuffer(PROP_LOCAL_TO_WORLD_UV_MATRICES, uvMatrix.Buffer);
                mat.Data.SetMatrix(PROP_WORLD_TO_SCREEN_MATRIX, CreateWorldToScreenMatrix());

                var screen = data.Screens;
                var screenCount = screen.x * screen.y;
                mat.Data.SetPass(0);
                Graphics.DrawProcedural(MeshTopology.Triangles, 54, screenCount);
            }
        }

        private void UpdateInputData() {
            worldMatrix.Screens = data.Screens;
            worldMatrix.Pivots = data.Pivots;
            edgeMatrices.Edges = data.Edges;
            uvMatrix.Viewports = data.Viewports;
        }

        void OnDisable() {
            mat.Dispose();
            worldMatrix.Dispose();
            edgeMatrices.Dispose();
            uvMatrix.Dispose();
        }
        #endregion
        
        private Matrix4x4 CreateWorldToScreenMatrix() {
            var screens = data.Screens;
            var m = Matrix4x4.zero;
            m[0] = 2f / screens.x; m[12] = -1f;
            m[5] = 2f / screens.y; m[13] = -1f;
            m[15] = 1f;
            return m;
        }
    }
}
