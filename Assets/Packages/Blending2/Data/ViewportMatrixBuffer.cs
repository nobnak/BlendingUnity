using Gist.Extensions.Array;
using Gist.GPUBuffer;
using Gist.Scoped;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Blending2 {
    public abstract class ViewportMatrixBuffer : MatrixBuffer {

        protected Matrix4x4 CreateMatrix(float x, float y, float w, float h) {
            var m = Matrix4x4.zero;
            m[0] = w;   m[12] = x;
            m[5] = h;   m[13] = y;
            m[15] = 1f;
            return m;
        }
    }
}
