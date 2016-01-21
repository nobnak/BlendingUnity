using UnityEngine;
using System.Collections;

namespace nobnak.GUI {

	public struct GUIFloat {
		[System.Flags]
		public enum Type { Default = 0, WithSlider }

		public readonly static GUILayoutOption TEXT_FIELD_WIDTH = GUILayout.Width(60f);
		
		private int _initialized;
		private Type _flags;

		private string _title;
		private UIFloat _data;
		private float _min;
		private float _max;
		private GUILayoutOption _textFieldWidth;

		public void InitOnce(string title, float value) {
			InitOnce(title, value, Type.Default, 0f, 0f, TEXT_FIELD_WIDTH);
		}
		public void InitOnce(string title, float value, float min, float max) {
			InitOnce(title, value, Type.WithSlider, min, max, TEXT_FIELD_WIDTH);
		}
		public void InitOnce(string title, float value, Type flags, float min, float max, GUILayoutOption textFieldWidth) {
			if (_initialized != 0)
				return;
			this._initialized = 1;
			this._flags = flags;

			this._title = title;
			this._data = new UIFloat(value);
			this._min = min;
			this._max = max;
			this._textFieldWidth = textFieldWidth;
		}

		public void Invalidate() { _initialized = 0; }

		public float Draw() {
			GUILayout.BeginHorizontal();
			GUILayout.Label(_title);
			_data.StrValue = GUILayout.TextField(_data.StrValue, _textFieldWidth);
			GUILayout.EndHorizontal();
			if ((_flags & Type.WithSlider) != 0)
				_data.Value = GUILayout.HorizontalSlider(_data.Value, _min, _max);
			return _data.Value;
		}
	}

	public struct GUIVector {
		[System.Flags]
		public enum Type { Default = 0, WithSlider, IsColor }

		public UIVector Data { get; private set; }

		private int _initialized;
		private Type _flags;
		
		private string _title;
		private Vector4 _min;
		private Vector4 _max;
		private GUILayoutOption _textFieldWidth;

		public void InitOnce(string title, Vector4 value) {
			InitOnce(title, value, Type.Default, Vector4.zero, Vector4.zero, GUIFloat.TEXT_FIELD_WIDTH);
		}
		public void InitOnce(string title, Color value) {
			InitOnce(title, value, Type.IsColor | Type.WithSlider, Vector4.zero, Vector4.one, GUIFloat.TEXT_FIELD_WIDTH);
		}
		public void InitOnce(string title, Vector4 value, Vector4 min, Vector4 max) {
			InitOnce(title, value, Type.WithSlider, min, max, GUIFloat.TEXT_FIELD_WIDTH);
		}
		public void InitOnce(string title, Vector4 value, Type flags, Vector4 min, Vector4 max, GUILayoutOption textFieldWidth) {
			if (_initialized != 0)
				return;
			_initialized = 1;
			_flags = flags;

			this._title = title;
			this.Data = new UIVector(value);
			this._min = min;
			this._max = max;
			this._textFieldWidth = textFieldWidth;
		}

		public void Invalidate() { _initialized = 0; }

		public Vector4 Draw() {
			GUILayout.BeginHorizontal();
			GUILayout.Label(_title);
			if ((_flags & Type.IsColor) != 0) {
				var prevColor = UnityEngine.GUI.color;
				UnityEngine.GUI.color = Data.Value;
				GUILayout.Label("●▲■");
				UnityEngine.GUI.color = prevColor;
			}
			Data = DrawTextFields(Data);
			GUILayout.EndHorizontal();

			if ((_flags & Type.WithSlider) != 0)
				Data.Value = DrawSliders(Data.Value);
			return Data.Value;
		}

		UIVector DrawTextFields (UIVector data) {
			data.StrX = GUILayout.TextField (data.StrX, _textFieldWidth);
			data.StrY = GUILayout.TextField (data.StrY, _textFieldWidth);
			data.StrZ = GUILayout.TextField (data.StrZ, _textFieldWidth);
			data.StrW = GUILayout.TextField (data.StrW, _textFieldWidth);
			return data;
		}

		Vector4 DrawSliders (Vector4 v) {
			v.x = GUILayout.HorizontalSlider (v.x, _min.x, _max.x);
			v.y = GUILayout.HorizontalSlider (v.y, _min.y, _max.y);
			v.z = GUILayout.HorizontalSlider (v.z, _min.z, _max.z);
			v.w = GUILayout.HorizontalSlider (v.w, _min.w, _max.w);
			return v;
		}
	}
}
