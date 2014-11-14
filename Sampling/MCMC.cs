using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using nobnak.Texture;

namespace nobnak.Sampling {

	public class MCMC {
		public readonly CPUTexture ProbTex;
		public readonly float StdDev;
		public readonly float Aspect;
		public readonly float Height;
		public readonly float Epsilon;
		public readonly Rect EffectiveArea;

		private Vector2 _curr;
		private float _currDensity;
		private Vector2 _stddevAspect;

		public MCMC(CPUTexture probTex, float stddev, float aspect) 
		: this(probTex, stddev, aspect, 1f, 1e-6f, new Rect(0f, 0f, 1f, 1f)) {}
		public MCMC(CPUTexture probTex, float stddev, float aspect, float height, float epsilon)
		: this(probTex, stddev, aspect, height, epsilon, new Rect(0f, 0f, 1f, 1f)) {}
			public MCMC(CPUTexture probTex, float stddev, float aspect, float height, float epsilon, Rect area) {
				this.ProbTex = probTex;
			this.Aspect = aspect;
			this.Height = height;
			this.Epsilon = epsilon;
			this.StdDev = stddev;

			area.xMin = Mathf.Clamp01(area.xMin);
			area.xMax = Mathf.Clamp01(area.xMax);
			area.yMin = Mathf.Clamp01(area.yMin);
			area.yMax = Mathf.Clamp01(area.yMax);
			this.EffectiveArea = area;
		}

		public IEnumerable<Vector2> Sequence(int nInitialize, int limit) {
			return Sequence(nInitialize, limit, 0);
		}
		public IEnumerable<Vector2> Sequence(int nInitialize, int limit, int nSkip) {
			_curr = new Vector2(Random.value, Random.value);
			_currDensity = Density(_curr);
			_stddevAspect = new Vector2(StdDev, StdDev / Aspect);

			for (var i = 0; i < nInitialize; i++)
				Next();

			for (var i = 0; i < limit; i++) {
				for (var j = 0; j < nSkip; j++)
					Next();
				yield return _curr;
				Next ();
			}
		}
		
		void Next() {
			var next = Vector2.Scale(_stddevAspect, BoxMuller.Gaussian()) + _curr;
			next = Repeat(next);
			
			var densityNext = Density(next);
			if (Mathf.Min(1f, densityNext / _currDensity) >= Random.value) {
				_curr = next;
				_currDensity = densityNext;
			}
		}
		float Density(Vector2 curr) {
			return Height * ProbTex[curr.x, curr.y] + Epsilon;
		}
		Vector2 Repeat(Vector2 v) {
			v.x -= Mathf.Floor((v.x - EffectiveArea.xMin) / EffectiveArea.width) * EffectiveArea.width;
			v.y -= Mathf.Floor((v.y - EffectiveArea.yMin) / EffectiveArea.height) * EffectiveArea.height;
			return Clamp(v);
		}
		Vector2 Clamp( Vector2 v) {
			v.x = Mathf.Clamp(v.x, EffectiveArea.xMin, EffectiveArea.xMax);
			v.y = Mathf.Clamp(v.y, EffectiveArea.yMin, EffectiveArea.yMax);
			return v;
		}
	}
}