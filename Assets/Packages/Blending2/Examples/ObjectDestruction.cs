using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Blending2 {

    public static class ObjectDestruction {
        
        public static void Release (Object obj) {
            if (Application.isPlaying)
                Object.Destroy (obj);
            else
                Object.DestroyImmediate (obj);
        }
    }
}
