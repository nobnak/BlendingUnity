using UnityEngine;
using System.Collections;

namespace nobnak.Blending {
		
	public class AutoHideCursor : MonoBehaviour {
		public float hideAfterSeconds = 3f;
		public float thresholdInPixels = 3f;

		float _lastTime;
		Vector3 _lastMousePos;

		void Start () {
			_lastTime = Time.timeSinceLevelLoad;
			_lastMousePos = Input.mousePosition;
		}
		
		void Update () {
			var dx = Input.mousePosition - _lastMousePos;
			var move = (dx.sqrMagnitude > (thresholdInPixels * thresholdInPixels));
			_lastMousePos = Input.mousePosition;

			if (move)
				_lastTime = Time.timeSinceLevelLoad;

			Cursor.visible = (Time.timeSinceLevelLoad - _lastTime) < hideAfterSeconds;
		}
	}
}