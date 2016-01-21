using UnityEngine;
using System.Collections;

namespace nobnak.GUI {

	public struct StrInt {
		private int _value;
		private string _str;
		private bool _invalidate;
		
		public static implicit operator StrInt(int initValue) {
			var gi = default(StrInt);
			gi.Init(initValue);
			return gi;
		}
		
		public void Init(int initValue) {
			_invalidate = false;
			_value = initValue;
			_str = _value.ToString();
		}
		
		public string Str {
			get { return _str; }
			set {
				_invalidate = true;
				_str = value;
			}
		}
		public int Value {
			get {
				if (_invalidate && int.TryParse(_str, out _value))
					_invalidate = false;
				return _value;
			}
			set { Init(value); }
		}
	}

	public struct StrFloat {
		private float _value;
		private string _str;
		private bool _invalidate;

		public static implicit operator StrFloat(float initValue) {
			var gf = default(StrFloat);
			gf.Init(initValue);
			return gf;
		}

		public void Init(float initValue) {
			_invalidate = false;
			_value = initValue;
			_str = _value.ToString();
		}

		public string Str {
			get { return _str; }
			set {
				_invalidate = true;
				_str = value;
			}
		}

		public float Value {
			get {
				if (_invalidate && float.TryParse(_str, out _value))
					_invalidate = false;
				return _value;
			}
			set { Init(value); }
		}
	}

	public struct StrVector {
		private StrFloat _x, _y, _z, _w;

		public static implicit operator StrVector(Vector2 initValue) {
			return (StrVector)((Vector4)initValue);
		}
		public static implicit operator StrVector(Vector3 initValue) {
			return (StrVector)((Vector4)initValue);
		}
		public static implicit operator StrVector(Vector4 initValue) {
			var gv = default(StrVector);
			gv.Init(initValue);
			return gv;
		}

		public void Init(Vector4 initValue) {
			_x = initValue.x;
			_y = initValue.y;
			_z = initValue.z;
			_w = initValue.w;
		}

		public string X {
			get { return _x.Str; }
			set { _x.Str = value; }
		}
		public string Y {
			get { return _y.Str; }
			set { _y.Str = value; }
		}
		public string Z {
			get { return _z.Str; }
			set { _z.Str = value; }
		}
		public string W {
			get { return _w.Str; }
			set { _w.Str = value; }
		}

		public Vector4 Value {
			get { return new Vector4(_x.Value, _y.Value, _z.Value, _w.Value); }
			set { Init (value); }
		}
	}
}