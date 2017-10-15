using Gist.Extensions.Array;
using Gist.GPUBuffer;
using Gist.Scoped;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Blending2 {
    public class UvToWorldMatrix : System.IDisposable {
        
        protected bool invalid;

        protected Int2 screens;
        protected Pivot[] pivots;

        protected GPUList<Matrix4x4> matrices;

        public UvToWorldMatrix() {
            invalid = true;
            matrices = new GPUList<Matrix4x4>();
        }

        public void Dispose() {
            if (matrices != null) {
                matrices.Dispose();
                matrices = null;
            }
        }

        #region Input
        public Int2 Screns {
            get { return screens;  }
            set {
                invalid = true;
                screens = value;
            }
        }
        public Pivot[] Pivots {
            get { return pivots; }
            set {
                invalid = true;
                pivots = value;
            }
        }
        #endregion

        #region Output
        public GPUList<Matrix4x4> Matrices {
            get {
                if (invalid) {
                    invalid = false;
                    ValidateInputData();
                    UpdateCornerMatrices();
                    matrices.Upload();
                }
                return matrices;
            }
        }
        public ComputeBuffer Buffer {
            get {
                return Matrices.Buffer;
            }
        }
        #endregion

        protected void ValidateInputData() {
            screens.x = Mathf.Max(1, screens.x);
            screens.y = Mathf.Max(1, screens.y);
            System.Array.Resize(ref pivots, screens.x * screens.y);
        }

        protected Matrix4x4 CreateCornerMatrix(int x, int y, Vector2 p00, Vector2 p10, Vector2 p01, Vector2 p11) {
            var m = Matrix4x4.zero;
            m[0] = x + p00.x;      m[4] = x + p10.x + 1f;     m[8] = x + p01.x;         m[12] = x + p11.x + 1f;
            m[1] = y + p00.y;      m[5] = y + p10.y;            m[9] = y + p01.y + 1f;  m[13] = y + p11.y + 1f;
            m[15] = 1f;
            return m;
        }
        protected Matrix4x4 CreateCornerMatrix(int x, int y, Pivot pp) {
            return CreateCornerMatrix(x, y, pp.p00, pp.p10, pp.p01, pp.p11);
        }

        protected void UpdateCornerMatrices() {
            matrices.Clear();
            
            for (var y = 0; y < screens.y; y++)
                for (var x = 0; x < screens.x; x++)
                    matrices.Add(CreateCornerMatrix(x, y, pivots[x + y * screens.x]));
        }

        [System.Serializable]
        public struct Pivot {
            public readonly Vector2 p00;
            public readonly Vector2 p10;
            public readonly Vector2 p01;
            public readonly Vector2 p11;

            public Pivot(Vector2 p00, Vector2 p10, Vector2 p01, Vector2 p11) {
                this.p00 = p00;
                this.p10 = p10;
                this.p01 = p01;
                this.p11 = p11;
            }
        }
    }
}
