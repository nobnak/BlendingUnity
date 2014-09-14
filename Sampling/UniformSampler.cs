using UnityEngine;
using System.Collections;
using nobnak.Search;

namespace nobnak.Sampling {
    public static class UniformSampler {
		public static float[] Samples(Vector2 range, int size) {
			var samples = new float[size];
			for (var i = 0; i < size; i++)
				samples[i] = Random.Range(range.x, range.y);
			return samples;
		}
		public static float[] SamplesInv(Vector2 range, int size) {
			var samples = Samples(range, size);
			for (var i = 0; i < size; i++)
				samples[i] = 1f / samples[i];
			return samples;
		}
    }
}