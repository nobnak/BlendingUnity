using UnityEngine;
using System.Collections;

namespace nobnak.Math {

	public static class Matrix {
		public static void ToHLSL(this Matrix4x4 m, float[] buf, int i) {
			// HLSL matrix is column major
			buf[i   ] = m.m00;	buf[i+ 4] = m.m01;	buf[i+ 8] = m.m02;	buf[i+12] = m.m03;
			buf[i+ 1] = m.m10;	buf[i+ 5] = m.m11;	buf[i+ 9] = m.m12;	buf[i+13] = m.m13;
			buf[i+ 2] = m.m20;	buf[i+ 6] = m.m21;	buf[i+10] = m.m22;	buf[i+14] = m.m23;
			buf[i+ 3] = m.m30;	buf[i+ 7] = m.m31;	buf[i+11] = m.m32;	buf[i+15] = m.m33;
		}
	}
}