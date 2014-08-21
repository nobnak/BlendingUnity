using UnityEngine;
using System.Collections;

namespace nobnak.Timer {

	public class RealDeltaTime {
		private float _prevTime;

		public RealDeltaTime() {
			this._prevTime = Time.realtimeSinceStartup;
		}

		public float DeltaTime() {
			var realTime = Time.realtimeSinceStartup;
			var dt = realTime - _prevTime;
			_prevTime = realTime;
			return dt;
		}
	}
}