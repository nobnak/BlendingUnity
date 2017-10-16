using Gist.Extensions.Array;
using Gist.GPUBuffer;
using Gist.Scoped;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Blending2 {
    public class LocalToWorldUvMatrix : ViewportMatrixBuffer {

        protected Rect[] viewports;

        #region Input
        public Rect[] Viewports {
            get { return viewports; }
            set {
                invalid = true;
                viewports = value;
            }
        }
        #endregion

        protected Matrix4x4 CreateMatrix(Rect vp) {
            return CreateMatrix(vp.x,vp.y, vp.width, vp.height);
        }
        protected override void UpdateMatrix() {
            matrices.Clear();
            foreach (var vp in viewports)
                matrices.Add(CreateMatrix(vp));
        }

    }
}
