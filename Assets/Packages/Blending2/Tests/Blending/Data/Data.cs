using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Blending2 {

    [System.Serializable]
    public class Data {
        [SerializeField]
        protected bool invalid;

        [SerializeField]
        protected Int2 screens = new Int2(1, 1);

        [SerializeField]
        protected UvToWorldMatrix.Pivot[] pivots;

        [SerializeField]
        protected Vector4[] edges;

        [SerializeField]
        protected Rect[] viewports;

        public bool MakeValidated() {
            if (invalid) {
                invalid = false;
                Validate();
                return true;
            }
            return false;
        }

        protected void Validate() {
            screens.x = Mathf.Max(screens.x, 1);
            screens.y = Mathf.Max(screens.y, 1);
            var screenCount = screens.x * screens.y;
            System.Array.Resize(ref pivots, screenCount);
            System.Array.Resize(ref edges, screenCount);
            System.Array.Resize(ref viewports, screenCount);
        }

        #region Interface
        public Int2 Screens {
            get { return screens; }
            set {
                invalid = true;
                screens = value;
            }
        }

        public UvToWorldMatrix.Pivot[] Pivots {
            get { return pivots; }
            set {
                invalid = true;
                pivots = value;
            }
        }

        public Vector4[] Edges {
            get { return edges; }
            set {
                invalid = true;
                edges = value;
            }
        }
        public Rect[] Viewports {
            get { return viewports; }
            set {
                invalid = true;
                viewports = value;
            }
        }
        #endregion
    }
}
