using UnityEngine;
using System.Collections;
using nobnak.GUI;
using Newtonsoft.Json;
using System.IO;

namespace nobnak.Blending {

	public class Blender : MonoBehaviour {
		public const int LAYER_BLEND = 30;
		public const int LAYER_MASK = 31;
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
		public static readonly float[] GAMMA_VALUE = new float[]{ 1 / 2.2f, 1f, 2.2f };

		public string config = "Blending.txt";
		public Data data;
		public Material blendMat;
		public KeyCode debugKey = KeyCode.E;

		private Capture _capture;
		private GameObject _blendCam;
		private GameObject _blendGo;
		private Mesh _blendMesh;

		private int _debugMode = 0;
		private UIInt _uiN;
		private UIInt _uiM;
		private UIFloat[] _uiHBlendings;
		private UIFloat[] _uiVBlendings;
		private int _selectedGamma = 0;

		void OnDisable() {
			Destroy(_capture.gameObject);
			Destroy(_blendCam);
			Destroy(_blendMesh);
			Destroy(_blendGo);
		}
		void OnEnable() {
			Load();
			CheckInit();
			UpdateMesh();

			_uiN = new UIInt(data.ColOverlaps.Length + 1);
			_uiM = new UIInt(data.RowOverlaps.Length + 1);
			_uiHBlendings = new UIFloat[0];
			_uiVBlendings = new UIFloat[0];
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
				//UpdateGUI();
			}

			blendMat.SetFloat(SHADER_GAMMA, data.Gamma);
		}
		void OnGUI() {
			if (_debugMode == 0)
				return;

			var uiSize = new Vector2(300f, 400f);
			GUILayout.BeginArea(new Rect(0.5f * (Screen.width - uiSize.x), 0.5f * (Screen.height - uiSize.y), uiSize.x, uiSize.y));

			GUILayout.BeginHorizontal();
			GUILayout.Label("Monitor N x M");
			_uiN.StrValue = GUILayout.TextField(_uiN.StrValue, TEXT_WIDTH);
			_uiM.StrValue = GUILayout.TextField(_uiM.StrValue, TEXT_WIDTH);
			GUILayout.EndHorizontal();

			GUILayout.Label("Gamma Correction");
			_selectedGamma = GUILayout.SelectionGrid(_selectedGamma, GAMMA_SELECT, GAMMA_SELECT.Length);

			GUILayout.Label("Horizontal Blending");
			GUILayout.BeginHorizontal();
			for (var i = 0; i < _uiHBlendings.Length; i++)
				_uiHBlendings[i].StrValue = GUILayout.TextField(_uiHBlendings[i].StrValue, TEXT_WIDTH);
			GUILayout.EndHorizontal();

			GUILayout.Label("Vertical Blending");
			GUILayout.BeginHorizontal();
			for (var i = 0; i < _uiVBlendings.Length; i++)
				_uiVBlendings[i].StrValue = GUILayout.TextField(_uiVBlendings[i].StrValue, TEXT_WIDTH);
			GUILayout.EndHorizontal();

			GUILayout.EndArea();
		}

		void CheckInit () {
			if (_capture == null) {
				var captureGo = new GameObject ("Capture Camera", typeof(Camera), typeof(Capture));
				captureGo.transform.parent = transform;
				_capture = captureGo.GetComponent<Capture> ();
			}
			blendMat.mainTexture = _capture.GetTarget ();
			if (_blendCam == null) {
				_blendCam = new GameObject ("Blend Camera", typeof(Camera));
				_blendCam.transform.parent = transform;
				_blendCam.transform.localPosition = new Vector3 (0f, 0f, -1f);
				_blendCam.transform.localRotation = Quaternion.identity;
				_blendCam.camera.depth = 100;
				_blendCam.camera.orthographic = true;
				_blendCam.camera.orthographicSize = 0.5f;
				_blendCam.camera.aspect = 1f;
				_blendCam.camera.clearFlags = CameraClearFlags.SolidColor;
			}
			var layerFlags = (1 << LAYER_BLEND) | (1 << LAYER_MASK);
			foreach (var cam in Camera.allCameras)
				cam.cullingMask &= ~layerFlags;
			_blendCam.camera.cullingMask = 1 << LAYER_BLEND;
			if (_blendGo == null) {
				_blendGo = new GameObject ("Blend");
				_blendGo.transform.parent = transform;
				_blendGo.transform.localPosition = new Vector3 (-0.5f, -0.5f, 0f);
				_blendGo.transform.localRotation = Quaternion.identity;
				_blendGo.transform.localScale = Vector3.one;
				_blendGo.layer = LAYER_BLEND;
				_blendGo.AddComponent<MeshRenderer> ().sharedMaterial = blendMat;
				_blendGo.AddComponent<MeshFilter> ().sharedMesh = _blendMesh = new Mesh ();
				_blendMesh.MarkDynamic ();
			}
		}

		void UpdateMesh() {
			var nRows = data.RowOverlaps.Length + 1;
			var nCols = data.ColOverlaps.Length + 1;
			var nScreens = nRows * nCols;
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
			var screenSize = new Vector2 (1f / nCols, 1f / nRows);
			var uvBase = Vector2.zero;
			for (var y = 0; y < nRows; y++) {
				var yFirst = (y == 0);
				var yLast = (y + 1 == nRows);
				uvBase.x = 0f;
				for (var x = 0; x < nCols; x++) {
					var xFirst = (x == 0);
					var xLast = (x + 1 == nCols);
					var b0 = new Vector2 (xFirst ? 0f : (data.ColOverlaps [x - 1] * screenSize.x), yFirst ? 0f : (data.RowOverlaps [y - 1] * screenSize.y));
					var b1 = new Vector2 (xLast ? 0f : (data.ColOverlaps [x] * screenSize.x), yLast ? 0f : (data.RowOverlaps [y] * screenSize.y));
					var vertexIndexBase = iScreen * 16;
					var vBase = new Vector3 (x * screenSize.x, y * screenSize.y, 0f);
					float x0 = vBase.x, x1 = vBase.x + b0.x, x2 = vBase.x + screenSize.x - b1.x, x3 = vBase.x + screenSize.x;
					float y0 = vBase.y, y1 = vBase.y + b0.y, y2 = vBase.y + screenSize.y - b1.y, y3 = vBase.y + screenSize.y;
					System.Array.Copy (new Vector3[] {
						new Vector3 (x0, y0, 0f), new Vector3 (x1, y0, 0f), new Vector3 (x2, y0, 0f), new Vector3 (x3, y0, 0f),
						new Vector3 (x0, y1, 0f), new Vector3 (x1, y1, 0f), new Vector3 (x2, y1, 0f), new Vector3 (x3, y1, 0f),
						new Vector3 (x0, y2, 0f), new Vector3 (x1, y2, 0f), new Vector3 (x2, y2, 0f), new Vector3 (x3, y2, 0f),
						new Vector3 (x0, y3, 0f), new Vector3 (x1, y3, 0f), new Vector3 (x2, y3, 0f), new Vector3 (x3, y3, 0f)
					}, 0, vertices, vertexIndexBase, 16);

					x0 = uvBase.x; x1 = uvBase.x + b0.x; x2 = uvBase.x + screenSize.x - b1.x; x3 = uvBase.x + screenSize.x;
					y0 = uvBase.y; y1 = uvBase.y + b0.y; y2 = uvBase.y + screenSize.y - b1.y; y3 = uvBase.y + screenSize.y;
					System.Array.Copy (new Vector2[] {
						new Vector2 (x0, y0), new Vector2 (x1, y0), new Vector2 (x2, y0), new Vector2 (x3, y0),
						new Vector2 (x0, y1), new Vector2 (x1, y1), new Vector2 (x2, y1), new Vector2 (x3, y1),
						new Vector2 (x0, y2), new Vector2 (x1, y2), new Vector2 (x2, y2), new Vector2 (x3, y2),
						new Vector2 (x0, y3), new Vector2 (x1, y3), new Vector2 (x2, y3), new Vector2 (x3, y3)
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

		void UpdateGUI () {
			var nX = data.ColOverlaps.Length + 1;
			var nY = data.RowOverlaps.Length + 1;
			if (_uiN.Value != nX || _uiM.Value != nY) {
				_uiN.Value = nX = Mathf.Max (1, _uiN.Value);
				_uiM.Value = nY = Mathf.Max (1, _uiM.Value);
				data.Reset (_uiN.Value, _uiM.Value);
			}
			if (_uiHBlendings.Length != (nX - 1)) {
				_uiHBlendings = new UIFloat[nX - 1];
				for (var i = 0; i < _uiHBlendings.Length; i++)
					_uiHBlendings [i] = new UIFloat (data.ColOverlaps [i]);
			}
			if (_uiVBlendings.Length != (nY - 1)) {
				_uiVBlendings = new UIFloat[nY - 1];
				for (var i = 0; i < _uiVBlendings.Length; i++)
					_uiVBlendings [i] = new UIFloat (data.RowOverlaps [i]);
			}
			for (var i = 0; i < _uiHBlendings.Length; i++)
				data.ColOverlaps [i] = _uiHBlendings [i].Value;
			for (var i = 0; i < _uiVBlendings.Length; i++)
				data.RowOverlaps [i] = _uiVBlendings [i].Value;

			data.Gamma = GAMMA_VALUE[_selectedGamma];
		}

		void Load() {
			var path = Path.Combine(Application.streamingAssetsPath, config);

			data.CheckInit();
			if (File.Exists(path))
				JsonConvert.PopulateObject(File.ReadAllText(path), data);
		}
		void Save() {
			using (var writer = new StreamWriter(Path.Combine(Application.streamingAssetsPath, config)))
				writer.Write(JsonConvert.SerializeObject(data, Formatting.Indented));
		}

		[System.Serializable]
		public class Data {
			public float[] RowOverlaps;
			public float[] ColOverlaps;
			public float Gamma;

			public Data() {
				Reset(1, 1);
			}

			public void CheckInit() {
				if (RowOverlaps == null || ColOverlaps == null)
					Reset(1, 1);
			}
			public void Reset(int nCols, int nRows) {
				ColOverlaps = new float[nCols - 1];
				RowOverlaps = new float[nRows - 1];
			}
		}
	}
}