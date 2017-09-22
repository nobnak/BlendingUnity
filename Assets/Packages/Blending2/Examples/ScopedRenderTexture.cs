using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Blending2 {

    public class ScopedRenderTexture : Scoped {
        protected RenderTexture prev;

        public ScopedRenderTexture(RenderTexture next) {
            prev = RenderTexture.active;
            RenderTexture.active = next;
        }

        #region IDisposable implementation
        public override void Dispose () {
            RenderTexture.active = prev;
        }
        #endregion
    }

    public abstract class Scoped : System.IDisposable {
        #region IDisposable implementation
        public abstract void Dispose ();
        #endregion
        
    }
}
