using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Blending2 {
        
    public class UVQuad {
        public readonly static Vector3[] QUAD_VERTICES = new Vector3[] {
            new Vector3 (0f, 0f),
            new Vector3 (1f, 0f),
            new Vector3 (0f, 1f),
            Vector3.one
        };
        public readonly static Vector2[] QUAD_UVS = new Vector2[] {
            new Vector2 (0f, 0f),
            new Vector2 (1f, 0f),
            new Vector2 (0f, 1f),
            Vector2.one
        };
        public readonly static int[] QUAD_INDICES = new int[]{ 0, 3, 1, 0, 2, 3 };

        public static Mesh Generate(Mesh mesh = null) {
            if (mesh == null)
                mesh = new Mesh ();
            
            mesh.vertices = QUAD_VERTICES;
            mesh.uv = QUAD_UVS;
            mesh.triangles = QUAD_INDICES;
            mesh.RecalculateBounds ();
            return mesh;
        }
    }
}
