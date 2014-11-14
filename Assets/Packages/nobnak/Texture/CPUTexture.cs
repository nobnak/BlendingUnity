using UnityEngine;
using System.Collections;

namespace nobnak.Texture {

	public class CPUTexture {
		public readonly int Width;
		public readonly int Height;

		public readonly float[] Values;

		public CPUTexture(int width, int height) {
			Width = width;
			Height = height;
			Values = new float[width * height];
		}

		public float this[int x, int y] {
			get {
				x = x < 0 ? 0 : (x >= Width ? Width-1 : x);
				y = y < 0 ? 0 : (y >= Height ? Height-1 : y);
				return Values[x + y * Width];
			}
			set {
				x = x < 0 ? 0 : (x >= Width ? Width-1 : x);
				y = y < 0 ? 0 : (y >= Height ? Height-1 : y);
				Values[x + y * Width] = value;
			}
		}
		public float this[float u, float v] {
			get {
				var x = u * Width;
				var y = v * Height;
				var ix = (int)x;
				var iy = (int)y;
				var s = x - ix;
				var t = y - iy;

				ix = ix < 0 ? 0 : (ix >= Width ? Width-1 : ix);
				iy = iy < 0 ? 0 : (iy >= Height ? Height-1 : iy);
				var ix1 = ix + 1;
				var iy1 = iy + 1;
				ix1 = ix1 < 0 ? 0 : (ix1 >= Width ? Width-1 : ix1);
				iy1 = iy1 < 0 ? 0 : (iy1 >= Height ? Height-1 : iy1);
				
				return (1f-t) * ((1f-s) * Values[ix+iy*Width] + s * Values[ix1+iy*Width]) 
					+ t * ((1f-s) * Values[ix+iy1*Width] + s * Values[ix1+iy1*Width]);
			}
		}
	}
}