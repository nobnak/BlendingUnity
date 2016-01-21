using UnityEngine;
using System.Collections;

namespace nobnak.GUI {

	public class UIFloat {
		private float _value = 0;
		private string _strValue = "0";
		private bool _changed = true;

		public UIFloat(float exisitingValue) {
			Value = exisitingValue;
		}

		public string StrValue {
			get {
				return _strValue;
			}
			set {
				if (value != null && value != _strValue) {
					_strValue = value;
					_changed = true;
				}
			}
		}
		public float Value {
			get {
				if (_changed && float.TryParse(_strValue, out _value))
					_changed = false;
				return _value;
			}
			set {
				if (_value != value) {
					_value = value;
					_strValue = _value.ToString();
					_changed = false;
				}
			}
		}
	}

	public class UIInt {
		private int _value = 0;
		private string _strValue = "0";
		private bool _changed = true;

		public UIInt(int existing) {
			Value = existing;
		}

		public string StrValue {
			get {
				return _strValue;
			}
			set {
				_strValue = value;
				_changed = true;
			}
		}

		public int Value {
			get {
				if (_changed && int.TryParse(_strValue, out _value))
					_changed = false;
				return _value;
			}
			set {
				if (_value != value) {
					_value = value;
					_strValue = _value.ToString();
					_changed = false;
				}
			}
		}
	}

	public class UIVector {
		private UIFloat _x = new UIFloat(0);
		private UIFloat _y = new UIFloat(0);
		private UIFloat _z = new UIFloat(0);
		private UIFloat _w = new UIFloat(0);
		private Vector4 _value = Vector4.zero;

		public UIVector(Vector4 existing) {
			Value = existing;
		}

		public string StrX {
			get { return _x.StrValue; }
			set { _x.StrValue = value; }
		}
		public string StrY {
			get { return _y.StrValue; }
			set { _y.StrValue = value; }
		}
		public string StrZ {
			get { return _z.StrValue; }
			set { _z.StrValue = value; }
		}
		public string StrW {
			get { return _w.StrValue; }
			set { _w.StrValue = value; }
		}

		public Vector4 Value {
			get {
				_value.Set(_x.Value, _y.Value, _z.Value, _w.Value);
				return _value;
			}
			set {
				_value = value;
				_x.Value = _value.x;
				_y.Value = _value.y;
				_z.Value = _value.z;
				_w.Value = _value.w;
			}
		}
	}
}