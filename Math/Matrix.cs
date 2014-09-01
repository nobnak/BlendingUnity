using UnityEngine;
using System.Collections;

namespace nobnak.Math {

	public static class Matrix {
		public static Matrix4x4 ToHLSL(this Matrix4x4 m, float[] buf, int i) {
			// HLSL matrix is column major
			buf[i   ] = m.m00;	buf[i+ 4] = m.m01;	buf[i+ 8] = m.m02;	buf[i+12] = m.m03;
			buf[i+ 1] = m.m10;	buf[i+ 5] = m.m11;	buf[i+ 9] = m.m12;	buf[i+13] = m.m13;
			buf[i+ 2] = m.m20;	buf[i+ 6] = m.m21;	buf[i+10] = m.m22;	buf[i+14] = m.m23;
			buf[i+ 3] = m.m30;	buf[i+ 7] = m.m31;	buf[i+11] = m.m32;	buf[i+15] = m.m33;
			return m;
		}

		public static Matrix4x4 Rotation(Vector3 t, Vector3 b, Vector3 n) {
			var m = new Matrix4x4();
			m.m00 = t.x;	m.m01 = b.x;	m.m02 = n.x;	m.m03 = 0f;
			m.m10 = t.y;	m.m11 = b.y;	m.m12 = n.y;	m.m13 = 0f;
			m.m20 = t.z;	m.m21 = b.z;	m.m22 = n.z;	m.m23 = 0f;
			m.m30 =  0f;	m.m31 =  0f;	m.m32 =  0f;	m.m33 = 1f;
			return m;
		}
	}
}