using UnityEngine;
using System.Collections;
using nobnak.GUI;
using Newtonsoft.Json;
using System.IO;
using nobnak.Json;

namespace nobnak.Blending {

	public class Blender : MonoBehaviour {
		public const int LAYER_BLEND = 30;
		public const int LAYER_MASK = 31;
		public const int DEPTH_CAPTURE = 90;
		public const int DEPTH_BLEND = 91;
		public const int DEPTH_MASK = 92;
		public const string SHADER_GAMMA = "_Gamma";
		public static readonly GUILayoutOption TEXT_WIDTH = GUILayout.Width(60f);

		public static readonly int[] SCREEN_INDICES = new int[]{
			0,  5, 1, 0,  4,  5, 1,  6,  2, 1,  5,  6,  2,  7,  3,  2,  6,  7,
			4,  9, 5, 4,  8,  9, 5, 10,  6, 5,  9, 10,  6, 11,  7,  6, 10, 11,
			8, 13, 9, 8, 12, 13, 9, 14, 10, 9, 13, 14, 10, 15, 11, 10, 14, 15 };
		public static readonly Vector2[] UV2 = new Vector2[]{
			Vector2.zero, new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.zero,
			new Vector2(0f, 1f), Vector2.one, Vector2.one, new Vector2(0f, 1f),
			new Vector2(0f, 1f), Vector2.one, Vector2.one, new Vector2(0f, 1f),
			Vector2.zero, new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.zero };

		public static readonly string[] GAMMA_SELECT = new string[]{ "sRGB", "Linear", "1/sRGB" };
		public static readonly float[] GAMMA_VALUE = new float[]{ (float)(1 / 2.2), 1f, 2.2f };

		public string config = "Blending.txt";
		public Data data;
		public Material blendMat;
		public Material maskMat;
		public KeyCode debugKey = KeyCode.E;

		private Capture _capture;
		private Capture _blend;
		private GameObject _blendObj;
		private Mesh _blendMesh;
		private GameObject _maskCam;
		private GameObject _maskObj;
		private Mesh _maskMesh;

		private int _debugMode = 0;
		private int _nCols;
		private int _nRows;
		private UIInt _uiN;
		private UIInt _uiM;
		private UIFloat[] _uiHBlendings;
		private UIFloat[] _uiVBlendings;
		private UIFloat _uiGamma;
		private UIFloat[] _uiMasks;
		private string[] _maskSelections;
		private int _selectedMask = 0;
		private UIFloat _uiUvU;
		private UIFloat _uiUvV;

		void OnDisable() {
			Destroy(_capture.gameObject);
			Destroy(_blend.gameObject);
			Destroy(_blendObj);
			Destroy(_blendMesh);
			Destroy(_maskCam);
			Destroy(_maskObj);
			Destroy(_maskMesh);
		}
		void OnEnable() {
			Load();
			CheckInit();
			UpdateMesh();
		}
		void Update() {
			if (Input.GetKeyDown(debugKey)) { 
				_debugMode = ++_debugMode % 2;
				Screen.showCursor = (_debugMode != 0);
				if (_debugMode == 0)
					Save();
			}

			if (_debugMode > 0) {
				CheckInit();
				UpdateMesh();
				UpdateGUI();
			}

			blendMat.SetFloat(SHADER_GAMMA, 1f / data.Gamma);
		}
		void OnGUI() {
			if (_debugMode == 0)
				return;

			var uiSize = new Vector2(600f, 400f);
			GUILayout.BeginArea(new Rect(0.5f * (Screen.width - uiSize.x), 0.5f * (Screen.height - uiSize.y), uiSize.x, uiSize.y));
			GUILayout.BeginHorizontal();

			GUILayout.BeginVertical(GUILayout.Width(uiSize.x * 0.49f));
			GUILayout.Label("---- Blending ----");
			GUILayout.BeginHorizontal();
			GUILayout.Label(" N x M");
			_uiN.StrValue = GUILayout.TextField(_uiN.StrValue, TEXT_WIDTH);
			_uiM.StrValue = GUILayout.TextField(_uiM.StrValue, TEXT_WIDTH);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Gamma Correction");
			_uiGamma.StrValue = GUILayout.TextField(_uiGamma.StrValue, TEXT_WIDTH);
			GUILayout.EndHorizontal();

			GUILayout.Label("Horizontal Blends");
			GUILayout.BeginHorizontal();
			for (var i = 0; i < _uiHBlendings.Length; i++)
				_uiHBlendings[i].StrValue = GUILayout.TextField(_uiHBlendings[i].StrValue, TEXT_WIDTH);
			GUILayout.EndHorizontal();

			GUILayout.Label("Vertical Blends");
			GUILayout.BeginHorizontal();
			for (var i = 0; i < _uiVBlendings.Length; i++)
				_uiVBlendings[i].StrValue = GUILayout.TextField(_uiVBlendings[i].StrValue, TEXT_WIDTH);
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();

			GUILayout.BeginVertical(GUILayout.Width(uiSize.x * 0.49f));
			GUILayout.Label("---- Mask ----");
			GUILayout.Label("Select Screen");
			var tmpSelectedMask = GUILayout.SelectionGrid(_selectedMask, _maskSelections, _nCols);
			if (tmpSelectedMask != _selectedMask) {
				_selectedMask = tmpSelectedMask;
				var selScreen = SelectedScreen();
				LoadMask(selScreen);
			}

			GUILayout.BeginHorizontal();
			GUILayout.Label("Bottom Left");
			_uiMasks[0].StrValue = GUILayout.TextField(_uiMasks[0].StrValue, TEXT_WIDTH);
			_uiMasks[1].StrValue = GUILayout.TextField(_uiMasks[1].StrValue, TEXT_WIDTH);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label("Bottom Right");
			_uiMasks[2].StrValue = GUILayout.TextField(_uiMasks[2].StrValue, TEXT_WIDTH);
			_uiMasks[3].StrValue = GUILayout.TextField(_uiMasks[3].StrValue, TEXT_WIDTH);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label("Top Left");
			_uiMasks[4].StrValue = GUILayout.TextField(_uiMasks[4].StrValue, TEXT_WIDTH);
			_uiMasks[5].StrValue = GUILayout.TextField(_uiMasks[5].StrValue, TEXT_WIDTH);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label("Top Right");
			_uiMasks[6].StrValue = GUILayout.TextField(_uiMasks[6].StrValue, TEXT_WIDTH);
			_uiMasks[7].StrValue = GUILayout.TextField(_uiMasks[7].StrValue, TEXT_WIDTH);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("UV Offset");
			_uiUvU.StrValue = GUILayout.TextField(_uiUvU.StrValue, TEXT_WIDTH);
			_uiUvV.StrValue = GUILayout.TextField(_uiUvV.StrValue, TEXT_WIDTH);
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();

			GUILayout.EndHorizontal();
			GUILayout.EndArea();
		}

		void CheckInit () {
			if (_capture == null) {
				var captureCam = new GameObject ("Capture Camera", typeof(Camera), typeof(Capture));
				captureCam.transform.parent = transform;
				captureCam.camera.depth = DEPTH_CAPTURE;
				_capture = captureCam.GetComponent<Capture> ();
			}

			if (_blend == null) {
				var blendCam = new GameObject ("Blend Camera", typeof(Camera), typeof(Capture));
				blendCam.transform.parent = transform;
				blendCam.transform.localPosition = new Vector3 (0f, 0f, -1f);
				blendCam.transform.localRotation = Quaternion.identity;
				blendCam.camera.depth = DEPTH_BLEND;
				blendCam.camera.orthographic = true;
				blendCam.camera.orthographicSize = 0.5f;
				blendCam.camera.aspect = 1f;
				blendCam.camera.clearFlags = CameraClearFlags.SolidColor;
				blendCam.camera.backgroundColor = Color.clear;
				_blend = blendCam.GetComponent<Capture>();
			}
			if (_blendObj == null) {
				_blendObj = new GameObject ("Blend Obj");
				_blendObj.transform.parent = transform;
				_blendObj.transform.localPosition = new Vector3 (-0.5f, -0.5f, 0f);
				_blendObj.transform.localRotation = Quaternion.identity;
				_blendObj.transform.localScale = Vector3.one;
				_blendObj.layer = LAYER_BLEND;
				_blendObj.AddComponent<MeshRenderer> ().sharedMaterial = blendMat;
				_blendObj.AddComponent<MeshFilter> ().sharedMesh = _blendMesh = new Mesh ();
				_blendMesh.MarkDynamic ();
			}

			if (_maskCam == null) {
				_maskCam = new GameObject("Mask Camera", typeof(Camera));
				_maskCam.transform.parent = transform;
				_maskCam.transform.localPosition = new Vector3(0f, 0f, -1f);
				_maskCam.transform.localRotation = Quaternion.identity;
				_maskCam.camera.depth = DEPTH_MASK;
				_maskCam.camera.orthographic = true;
				_maskCam.camera.orthographicSize = 0.5f;
				_maskCam.camera.aspect = 1f;
				_maskCam.camera.clearFlags = CameraClearFlags.SolidColor;
				_maskCam.camera.backgroundColor = Color.clear;
			}
			if (_maskObj == null) {
				_maskObj = new GameObject("Mask Obj");
				_maskObj.transform.parent = transform;
				_maskObj.transform.localPosition = new Vector3(-0.5f, -0.5f, 0f);
				_maskObj.transform.localRotation = Quaternion.identity;
				_maskObj.transform.localScale = Vector3.one;
				_maskObj.layer = LAYER_MASK;
				_maskObj.AddComponent<MeshRenderer>().sharedMaterial = maskMat;
				_maskObj.AddComponent<MeshFilter>().sharedMesh = _maskMesh = new Mesh();
				_maskMesh.MarkDynamic();
			}

			data.CheckInit();

			blendMat.mainTexture = _capture.GetTarget();
			maskMat.mainTexture = _blend.GetTarget();

			var layerFlags = (1 << LAYER_BLEND) | (1 << LAYER_MASK);
			foreach (var cam in Camera.allCameras)
				cam.cullingMask &= ~layerFlags;
			_blend.camera.cullingMask = 1 << LAYER_BLEND;
			_maskCam.camera.cullingMask = 1 << LAYER_MASK;

			_nCols = data.ColOverlaps.Length + 1;
			_nRows = data.RowOverlaps.Length + 1;
			if (_uiN == null)
				_uiN = new UIInt(_nCols);
			if (_uiM == null)
				_uiM = new UIInt(_nRows);
			if (_uiHBlendings == null)
				_uiHBlendings = new UIFloat[0];
			if (_uiVBlendings == null)
				_uiVBlendings = new UIFloat[0];
			if (_uiGamma == null)
				_uiGamma = new UIFloat(data.Gamma);
			if (_uiMasks == null) {
				_uiMasks = new UIFloat[8];
				LoadMask(SelectedScreen());
			}

		}

		void UpdateMesh() {
			UpdateBlendMesh();
			UpdateMaskMesh();
		}

		void UpdateBlendMesh() {
			var nScreens = _nRows * _nCols;
			var nVertices = 16 * nScreens;
			var nIndices = 54 * nScreens;
			if (_blendMesh.vertexCount != nVertices) {
				_blendMesh.Clear ();
				_blendMesh.vertices = new Vector3[nVertices];
				_blendMesh.uv = new Vector2[nVertices];
				_blendMesh.uv2 = new Vector2[nVertices];
				_blendMesh.triangles = new int[nIndices];
				_blendMesh.colors = new Color[nVertices];
			}
			var vertices = _blendMesh.vertices;
			var uv = _blendMesh.uv;
			var uv2 = _blendMesh.uv2;
			var triangles = _blendMesh.triangles;
			var iTriangle = 0;
			var iScreen = 0;
			var screenSize = new Vector2 (1f / _nCols, 1f / _nRows);
			var uvBase = Vector2.zero;
			for (var y = 0; y < _nRows; y++) {
				var yFirst = (y == 0);
				var yLast = (y + 1 == _nRows);
				uvBase.x = 0f;
				for (var x = 0; x < _nCols; x++) {
					var xFirst = (x == 0);
					var xLast = (x + 1 == _nCols);
					var b0 = new Vector2 (xFirst ? 0f : (data.ColOverlaps [x - 1] * screenSize.x), yFirst ? 0f : (data.RowOverlaps [y - 1] * screenSize.y));
					var b1 = new Vector2 (xLast ? 0f : (data.ColOverlaps [x] * screenSize.x), yLast ? 0f : (data.RowOverlaps [y] * screenSize.y));
					var vertexIndexBase = iScreen * 16;
					var vBase = new Vector3 (x * screenSize.x, y * screenSize.y, 0f);
					float x0 = vBase.x, x1 = vBase.x + b0.x, x2 = vBase.x + screenSize.x - b1.x, x3 = vBase.x + screenSize.x;
					float y0 = vBase.y, y1 = vBase.y + b0.y, y2 = vBase.y + screenSize.y - b1.y, y3 = vBase.y + screenSize.y;
					System.Array.Copy (new Vector3[] {
						new Vector3 (x0, y0, 0f),
						new Vector3 (x1, y0, 0f),
						new Vector3 (x2, y0, 0f),
						new Vector3 (x3, y0, 0f),
						new Vector3 (x0, y1, 0f),
						new Vector3 (x1, y1, 0f),
						new Vector3 (x2, y1, 0f),
						new Vector3 (x3, y1, 0f),
						new Vector3 (x0, y2, 0f),
						new Vector3 (x1, y2, 0f),
						new Vector3 (x2, y2, 0f),
						new Vector3 (x3, y2, 0f),
						new Vector3 (x0, y3, 0f),
						new Vector3 (x1, y3, 0f),
						new Vector3 (x2, y3, 0f),
						new Vector3 (x3, y3, 0f)
					}, 0, vertices, vertexIndexBase, 16);
					x0 = uvBase.x;
					x1 = uvBase.x + b0.x;
					x2 = uvBase.x + screenSize.x - b1.x;
					x3 = uvBase.x + screenSize.x;
					y0 = uvBase.y;
					y1 = uvBase.y + b0.y;
					y2 = uvBase.y + screenSize.y - b1.y;
					y3 = uvBase.y + screenSize.y;
					System.Array.Copy (new Vector2[] {
						new Vector2 (x0, y0),
						new Vector2 (x1, y0),
						new Vector2 (x2, y0),
						new Vector2 (x3, y0),
						new Vector2 (x0, y1),
						new Vector2 (x1, y1),
						new Vector2 (x2, y1),
						new Vector2 (x3, y1),
						new Vector2 (x0, y2),
						new Vector2 (x1, y2),
						new Vector2 (x2, y2),
						new Vector2 (x3, y2),
						new Vector2 (x0, y3),
						new Vector2 (x1, y3),
						new Vector2 (x2, y3),
						new Vector2 (x3, y3)
					}, 0, uv, vertexIndexBase, 16);
					for (var i = 0; i < UV2.Length; i++)
						uv2 [vertexIndexBase + i] = UV2 [i];
					foreach (var i in SCREEN_INDICES)
						triangles [iTriangle++] = vertexIndexBase + i;
					iScreen++;
					uvBase += new Vector2 (screenSize.x - b1.x, xLast ? (screenSize.y - b1.y) : 0f);
				}
			}
			_blendMesh.vertices = vertices;
			_blendMesh.uv = uv;
			_blendMesh.uv2 = uv2;
			_blendMesh.triangles = triangles;
			_blendMesh.RecalculateBounds ();
		}

		void UpdateMaskMesh() {
			var nScreens = data.Masks.Length;
			var nVertices = 4 * nScreens;
			var nIndices = 6 * nScreens;
			if (_maskMesh.vertexCount != nVertices) {
				_maskMesh.Clear ();
				_maskMesh.vertices = new Vector3[nVertices];
				_maskMesh.uv = new Vector2[nVertices];
				_maskMesh.triangles = new int[nIndices];
			}
			var vertices = _maskMesh.vertices;
			var uv = _maskMesh.uv;
			var triangles = _maskMesh.triangles;
			var screenSize = new Vector2(1f / _nCols, 1f / _nRows);
			for (var y = 0; y < _nRows; y++) {
				for (var x = 0; x < _nCols; x++) {
					var i = x + y * _nCols;
					var iv = 4 * i;
					var it = 6 * i;
					var mask = data.Masks [i];
					var offset = new Vector2(x * screenSize.x, y * screenSize.y);
					var v0 = offset + Vector2.Scale(mask.bl, screenSize);
					var v1 = offset + Vector2.Scale(mask.br, screenSize);
					var v2 = offset + Vector2.Scale(mask.tl, screenSize);
					var v3 = offset + Vector2.Scale(mask.tr, screenSize);
					vertices [iv    ] = v0;	uv [iv    ] = v0 + mask.uvOffset;
					vertices [iv + 1] = v1;	uv [iv + 1] = v1 + mask.uvOffset;
					vertices [iv + 2] = v2;	uv [iv + 2] = v2 + mask.uvOffset;
					vertices [iv + 3] = v3;	uv [iv + 3] = v3 + mask.uvOffset;
					triangles [it] = iv;
					triangles [it + 1] = iv + 3;
					triangles [it + 2] = iv + 1;
					triangles [it + 3] = iv;
					triangles [it + 4] = iv + 2;
					triangles [it + 5] = iv + 3;
				}
			}
			_maskMesh.vertices = vertices;
			_maskMesh.uv = uv;
			_maskMesh.triangles = triangles;
			_maskMesh.RecalculateBounds ();
		}

		void UpdateGUI () {
			if (_uiN.Value != _nCols || _uiM.Value != _nRows) {
				_uiN.Value = _nCols = Mathf.Max (1, _uiN.Value);
				_uiM.Value = _nRows = Mathf.Max (1, _uiM.Value);
				data.Reset (_uiN.Value, _uiM.Value);
			}

			if (_uiHBlendings.Length != (_nCols - 1)) {
				_uiHBlendings = new UIFloat[_nCols - 1];
				for (var i = 0; i < _uiHBlendings.Length; i++)
					_uiHBlendings [i] = new UIFloat (data.ColOverlaps [i]);
			}
			if (_uiVBlendings.Length != (_nRows - 1)) {
				_uiVBlendings = new UIFloat[_nRows - 1];
				for (var i = 0; i < _uiVBlendings.Length; i++)
					_uiVBlendings [i] = new UIFloat (data.RowOverlaps [i]);
			}
			for (var i = 0; i < _uiHBlendings.Length; i++)
				data.ColOverlaps [i] = _uiHBlendings [i].Value;
			for (var i = 0; i < _uiVBlendings.Length; i++)
				data.RowOverlaps [i] = _uiVBlendings [i].Value;

			data.Gamma = _uiGamma.Value;

			SaveMask(SelectedScreen());

			var nScreens = _nCols * _nRows;
			if (_maskSelections == null || _maskSelections.Length != nScreens) {
				_maskSelections = new string[nScreens];
				var i = 0;
				for (var y = _nRows - 1; y >= 0; y--)
					for (var x = 0; x < _nCols; x++)
						_maskSelections[i++] = string.Format("{0},{1}", x, y);
			}
		}

		void Load() {
			var path = Path.Combine(Application.streamingAssetsPath, config);
			if (File.Exists(path))
				JsonConvert.PopulateObject(File.ReadAllText(path), data);
		}
		void Save() {
			using (var writer = new StreamWriter(Path.Combine(Application.streamingAssetsPath, config)))
				writer.Write(JsonConvert.SerializeObject(data, Formatting.Indented));
		}

		int SelectedScreen() {
			var selX = _selectedMask % _nCols;
			var selY = (_nRows - 1) - _selectedMask / _nCols;
			var selScreen = Mathf.Clamp (selX + selY * _nCols, 0, (_nCols * _nRows - 1));
			return selScreen;
		}

		void SaveMask (int selScreen) {
			var mask = data.Masks [selScreen];

			for (var i = 0; i < _uiMasks.Length; i++)
				_uiMasks[i].Value = Mathf.Clamp01(_uiMasks[i].Value);

			mask.bl = new Vector2(_uiMasks[0].Value, _uiMasks[1].Value);
			mask.br = new Vector2(_uiMasks[2].Value, _uiMasks[3].Value);
			mask.tl = new Vector2(_uiMasks[4].Value, _uiMasks[5].Value);
			mask.tr = new Vector2(_uiMasks[6].Value, _uiMasks[7].Value);

			mask.uvOffset = new Vector2(_uiUvU.Value, _uiUvV.Value);

			data.Masks [selScreen] = mask;
		}

		void LoadMask(int selScreen) {
			var mask = data.Masks [selScreen];
			_uiMasks [0] = new UIFloat (mask.bl.x);
			_uiMasks [1] = new UIFloat (mask.bl.y);
			_uiMasks [2] = new UIFloat (mask.br.x);
			_uiMasks [3] = new UIFloat (mask.br.y);
			_uiMasks [4] = new UIFloat (mask.tl.x);
			_uiMasks [5] = new UIFloat (mask.tl.y);
			_uiMasks [6] = new UIFloat (mask.tr.x);
			_uiMasks [7] = new UIFloat (mask.tr.y);

			_uiUvU = new UIFloat(mask.uvOffset.x);
			_uiUvV = new UIFloat(mask.uvOffset.y);
		}

		[System.Serializable]
		public class Data {
			public float[] RowOverlaps;
			public float[] ColOverlaps;
			public float Gamma;

			public Mask[] Masks;

			public Data() { Reset(1, 1); }

			public void CheckInit() {
				if (RowOverlaps == null || ColOverlaps == null)
					Reset(1, 1);

				var nCols = ColOverlaps.Length + 1;
				var nRows = RowOverlaps.Length + 1;
				if (Masks == null || Masks.Length != (nCols * nRows))
					Reset(nCols, nRows);
			}
			public void Reset(int nCols, int nRows) {
				ColOverlaps = new float[nCols - 1];
				RowOverlaps = new float[nRows - 1];

				Gamma = 2.2f;

				Masks = new Mask[nCols * nRows];
				for (var i = 0; i < Masks.Length; i++)
					Masks[i] = new Mask(Vector2.zero, Vector2.one);
			}

			[System.Serializable]
			public class Mask {
				[JsonConverter(typeof(VectorJsonConverter))]
				public Vector2 bl, br, tl, tr;
				[JsonConverter(typeof(VectorJsonConverter))]
				public Vector2 uvOffset;

				public Mask(Vector2 bottomLeft, Vector2 screenSize) {
					this.bl = bottomLeft;
					this.br = bottomLeft + new Vector2(screenSize.x, 0f);
					this.tl = bottomLeft + new Vector2(0f, screenSize.y);
					this.tr = bottomLeft + screenSize;

					this.uvOffset = Vector2.zero;
				}
			}
		}
	}
}