using UnityEngine;
using System.Collections;
using System.IO;
using nobnak.GUI;
using System.Xml.Serialization;

namespace nobnak.Blending {

	public class Blender : MonoBehaviour {
        public enum ConfigFolderEnum { StreamingAssets = 0, MyDocuments }

		public const int LAYER_BLEND = 29;
		public const int LAYER_MASK = 30;
		public const int LAYER_OCCLUSION = 31;
		public const int DEPTH_CAPTURE = 90;
		public const int DEPTH_BLEND = 91;
		public const int DEPTH_MASK = 92;
		public const int DEPTH_OCCLUSION = 93;
		public const int NUM_RECTS = 4;

		public const string SHADER_MASK_TEX = "_MaskTex";
		public const string SHADER_RECTS = "_Rects";
		public static readonly Vector2 WINDOW_SIZE = new Vector2 (500f, 300f);
		public static readonly GUILayoutOption TEXT_WIDTH = GUILayout.Width (60f);
		public static readonly GUILayoutOption TEXT_WIDTH2 = GUILayout.Width (120f);

		public static readonly int[] SCREEN_INDICES = new int[] {
			0,  5, 1, 0,  4,  5, 1,  6,  2, 1,  5,  6,  2,  7,  3,  2,  6,  7,
			4,  9, 5, 4,  8,  9, 5, 10,  6, 5,  9, 10,  6, 11,  7,  6, 10, 11,
			8, 13, 9, 8, 12, 13, 9, 14, 10, 9, 13, 14, 10, 15, 11, 10, 14, 15
		};
		public static readonly Vector2[] UV2 = new Vector2[] {
			Vector2.zero, new Vector2 (1f, 0f), new Vector2 (1f, 0f), Vector2.zero,
			new Vector2 (0f, 1f), Vector2.one, Vector2.one, new Vector2 (0f, 1f),
			new Vector2 (0f, 1f), Vector2.one, Vector2.one, new Vector2 (0f, 1f),
			Vector2.zero, new Vector2 (1f, 0f), new Vector2 (1f, 0f), Vector2.zero
		};
		public static readonly Color[] COLORS = new Color[] {
			Color.white, Color.green, Color.white, Color.red, Color.black, Color.cyan, Color.white, Color.magenta, Color.white
		};

        public ConfigFolderEnum configFolder;
		public string config = "Blending.txt";
		public Data data;
		public Material blendMat;
		public Material maskMat;
		public Material occlusionMat;
		public Material vcolorMat;
		public KeyCode debugKey = KeyCode.E;

		Capture _capture;
		Capture _blend;
		Capture _mask;
		GameObject _blendObj;
		Mesh _blendMesh;
		GameObject _maskObj;
		Mesh _maskMesh;
		GameObject _occlusionCam;
		GameObject _occlusionObj;
		Mesh _occlusionMesh;
		Vector4[] _rects;
		string[] _rectNames;

		int _debugMode = 0;
		int _nCols;
		int _nRows;
		Rect _guiWindowPos;
		UIInt _uiN;
		UIInt _uiM;
		UIFloat[] _uiHBlendings;
		UIFloat[] _uiVBlendings;
		UIFloat[] _uiMasks;
		string[] _maskSelections;
		int _selectedMask = 0;
		UIFloat _uiUvU;
		UIFloat _uiUvV;
		GUIVector[] _guiRects;
		GUIVector _guiOcclusion;

        bool _maskImageToggle;
        bool _maskImageLoading = false;
        string _maskImagePath;
        Texture _maskImageTex;
        System.DateTime _maskImageWriteTime = System.DateTime.MinValue;

		void OnDisable () {
            if (_capture != null)
			    Destroy (_capture.gameObject);
            if (_blend != null)
			    Destroy (_blend.gameObject);
			Destroy (_blendObj);
			Destroy (_blendMesh);
            if (_mask != null)
			    Destroy (_mask.gameObject);
			Destroy (_maskObj);
			Destroy (_maskMesh);
			Destroy (_occlusionCam);
			Destroy (_occlusionObj);
            Destroy (_maskImageTex);
		}

		void OnEnable () {
			Load ();
			CheckInit ();
			UpdateMesh ();
            UpdateImage();
		}

		void Update () {
			if (Input.GetKeyDown (debugKey)) { 
				_debugMode = ++_debugMode % 3;
				if (_debugMode == 0)
					Save ();
			}

			if (_debugMode > 0) {
				CheckInit ();
				UpdateMesh ();
                UpdateImage();
				UpdateGUI ();
			}

			blendMat.mainTexture = _capture.GetTarget ();
            blendMat.SetTexture (SHADER_MASK_TEX, data.MaskImageToggle ? _maskImageTex : null);
			_blendObj.GetComponent<Renderer> ().sharedMaterial = (_debugMode == 1 ? vcolorMat : blendMat);
			maskMat.mainTexture = _blend.GetTarget ();
            //maskMat.SetTexture (SHADER_MASK_TEX, data.MaskImageToggle ? _maskImageTex : null);
			for (var i = 0; i < _rects.Length; i++)
				maskMat.SetVector (_rectNames [i], _rects [i]);
			occlusionMat.mainTexture = _mask.GetTarget ();
            //occlusionMat.SetTexture (SHADER_MASK_TEX, data.MaskImageToggle ? _maskImageTex : null);
		}

        #region GUI
		void OnGUI () {
			if (_debugMode == 0)
				return;

            _guiWindowPos = GUILayout.Window (GetInstanceID(), _guiWindowPos, DrawWindow, "Blending & Masking");
		}

		void DrawWindow (int id) {
			GUILayout.BeginHorizontal ();

			GUILayout.BeginVertical (GUILayout.Width (WINDOW_SIZE.x * 0.45f));
			GUILayout.Label ("---- Blending ----");
			GUILayout.BeginHorizontal ();
			GUILayout.Label (" N x M");
			_uiN.StrValue = GUILayout.TextField (_uiN.StrValue, TEXT_WIDTH);
			_uiM.StrValue = GUILayout.TextField (_uiM.StrValue, TEXT_WIDTH);
			GUILayout.EndHorizontal ();

			GUILayout.Label ("Horizontal Blends");
			GUILayout.BeginHorizontal ();
			for (var i = 0; i < _uiHBlendings.Length; i++)
				_uiHBlendings [i].StrValue = GUILayout.TextField (_uiHBlendings [i].StrValue, TEXT_WIDTH);
			GUILayout.EndHorizontal ();

			GUILayout.Label ("Vertical Blends");
			GUILayout.BeginHorizontal ();
			for (var i = 0; i < _uiVBlendings.Length; i++)
				_uiVBlendings [i].StrValue = GUILayout.TextField (_uiVBlendings [i].StrValue, TEXT_WIDTH);
			GUILayout.EndHorizontal ();

            GUILayout.Label ("---- Image Mask ----");
            _maskImageToggle = GUILayout.Toggle (_maskImageToggle, "Enabled");
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("Image Path");
            _maskImagePath = GUILayout.TextField (_maskImagePath, TEXT_WIDTH2);
            GUILayout.EndHorizontal ();

			GUILayout.Label ("---- Rect Mask ----");
			for (var i = 0; i < _guiRects.Length; i++)
				_rects [i] = _guiRects [i].Draw ();
			GUILayout.EndVertical ();

			GUILayout.FlexibleSpace ();

			GUILayout.BeginVertical (GUILayout.Width (WINDOW_SIZE.x * 0.45f));
			GUILayout.Label ("---- Boundary Mask ----");
			GUILayout.Label ("Select Screen");
			var tmpSelectedMask = GUILayout.SelectionGrid (_selectedMask, _maskSelections, _nCols);
			if (tmpSelectedMask != _selectedMask) {
				_selectedMask = tmpSelectedMask;
				var selScreen = SelectedScreen ();
				LoadScreenData (selScreen);
			}

			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Bottom Left");
			_uiMasks [0].StrValue = GUILayout.TextField (_uiMasks [0].StrValue, TEXT_WIDTH);
			_uiMasks [1].StrValue = GUILayout.TextField (_uiMasks [1].StrValue, TEXT_WIDTH);
			GUILayout.EndHorizontal ();
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Bottom Right");
			_uiMasks [2].StrValue = GUILayout.TextField (_uiMasks [2].StrValue, TEXT_WIDTH);
			_uiMasks [3].StrValue = GUILayout.TextField (_uiMasks [3].StrValue, TEXT_WIDTH);
			GUILayout.EndHorizontal ();
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Top Left");
			_uiMasks [4].StrValue = GUILayout.TextField (_uiMasks [4].StrValue, TEXT_WIDTH);
			_uiMasks [5].StrValue = GUILayout.TextField (_uiMasks [5].StrValue, TEXT_WIDTH);
			GUILayout.EndHorizontal ();
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Top Right");
			_uiMasks [6].StrValue = GUILayout.TextField (_uiMasks [6].StrValue, TEXT_WIDTH);
			_uiMasks [7].StrValue = GUILayout.TextField (_uiMasks [7].StrValue, TEXT_WIDTH);
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			GUILayout.Label ("UV Offset");
			_uiUvU.StrValue = GUILayout.TextField (_uiUvU.StrValue, TEXT_WIDTH);
			_uiUvV.StrValue = GUILayout.TextField (_uiUvV.StrValue, TEXT_WIDTH);
			GUILayout.EndHorizontal ();

			_guiOcclusion.Draw ();

			GUILayout.EndVertical ();
			GUILayout.EndHorizontal ();

			UnityEngine.GUI.DragWindow ();
		}
        #endregion

        #region Update
		void CheckInit () {
			if (_capture == null) {
				var captureCam = new GameObject ("Capture Camera", typeof(Camera), typeof(Capture));
				captureCam.transform.SetParent (transform, false);
				captureCam.GetComponent<Camera> ().depth = DEPTH_CAPTURE;
				_capture = captureCam.GetComponent<Capture> ();
			}

			if (_blend == null) {
				var blendCam = new GameObject ("Blend Camera", typeof(Camera), typeof(Capture));
				blendCam.transform.parent = transform;
				blendCam.transform.localPosition = new Vector3 (0f, 0f, -1f);
				blendCam.transform.localRotation = Quaternion.identity;
				blendCam.GetComponent<Camera> ().depth = DEPTH_BLEND;
				blendCam.GetComponent<Camera> ().orthographic = true;
				blendCam.GetComponent<Camera> ().orthographicSize = 0.5f;
				blendCam.GetComponent<Camera> ().aspect = 1f;
				blendCam.GetComponent<Camera> ().clearFlags = CameraClearFlags.SolidColor;
				blendCam.GetComponent<Camera> ().backgroundColor = Color.clear;
				_blend = blendCam.GetComponent<Capture> ();
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

			if (_mask == null) {
				var maskCam = new GameObject ("Mask Camera", typeof(Camera), typeof(Capture));
				maskCam.transform.parent = transform;
				maskCam.transform.localPosition = new Vector3 (2f, 0f, -1f);
				maskCam.transform.localRotation = Quaternion.identity;
				maskCam.GetComponent<Camera> ().depth = DEPTH_MASK;
				maskCam.GetComponent<Camera> ().orthographic = true;
				maskCam.GetComponent<Camera> ().orthographicSize = 0.5f;
				maskCam.GetComponent<Camera> ().aspect = 1f;
				maskCam.GetComponent<Camera> ().clearFlags = CameraClearFlags.SolidColor;
				maskCam.GetComponent<Camera> ().backgroundColor = Color.clear;
				_mask = maskCam.GetComponent<Capture> ();
			}
			if (_maskObj == null) {
				_maskObj = new GameObject ("Mask Obj");
				_maskObj.transform.parent = transform;
				_maskObj.transform.localPosition = new Vector3 (1.5f, -0.5f, 0f);
				_maskObj.transform.localRotation = Quaternion.identity;
				_maskObj.transform.localScale = Vector3.one;
				_maskObj.layer = LAYER_MASK;
				_maskObj.AddComponent<MeshRenderer> ().sharedMaterial = maskMat;
				_maskObj.AddComponent<MeshFilter> ().sharedMesh = _maskMesh = new Mesh ();
				_maskMesh.MarkDynamic ();
			}
			if (_occlusionCam == null) {
				_occlusionCam = new GameObject ("Occulusion Camera", typeof(Camera));
				_occlusionCam.transform.parent = transform;
				_occlusionCam.transform.localPosition = new Vector3 (4f, 0f, -1f);
				_occlusionCam.transform.localRotation = Quaternion.identity;
				_occlusionCam.GetComponent<Camera> ().depth = DEPTH_OCCLUSION;
				_occlusionCam.GetComponent<Camera> ().orthographic = true;
				_occlusionCam.GetComponent<Camera> ().orthographicSize = 0.5f;
				_occlusionCam.GetComponent<Camera> ().aspect = 1f;
				_occlusionCam.GetComponent<Camera> ().clearFlags = CameraClearFlags.SolidColor;
				_occlusionCam.GetComponent<Camera> ().backgroundColor = Color.clear;
			}
			if (_occlusionObj == null) {
				_occlusionObj = new GameObject ("Occulusion Obj");
				_occlusionObj.transform.parent = transform;
				_occlusionObj.transform.localPosition = new Vector3 (3.5f, -0.5f, 0f);
				_occlusionObj.transform.localRotation = Quaternion.identity;
				_occlusionObj.transform.localScale = Vector3.one;
				_occlusionObj.layer = LAYER_OCCLUSION;
				_occlusionObj.AddComponent<MeshRenderer> ().sharedMaterial = occlusionMat;
				_occlusionObj.AddComponent<MeshFilter> ().sharedMesh = _occlusionMesh = new Mesh ();
				_occlusionMesh.MarkDynamic ();
			}

			if (_rects == null) {
				_rects = new Vector4[NUM_RECTS];
				_rectNames = new string[NUM_RECTS];
				for (var i = 0; i < _rectNames.Length; i++) {
					var j = 4 * i;
					_rects [i] = new Vector4 (data.Rects [j], data.Rects [j + 1], data.Rects [j + 2], data.Rects [j + 3]);
					_rectNames [i] = string.Format ("{0}{1:d}", SHADER_RECTS, i);
				}
			}

			_nCols = data.ColOverlaps.Length + 1;
			_nRows = data.RowOverlaps.Length + 1;

			var layerFlags = (1 << LAYER_BLEND) | (1 << LAYER_MASK) | (1 << LAYER_OCCLUSION);
			foreach (var cam in Camera.allCameras)
				cam.cullingMask &= ~layerFlags;
			_blend.GetComponent<Camera> ().cullingMask = 1 << LAYER_BLEND;
			_mask.GetComponent<Camera> ().cullingMask = 1 << LAYER_MASK;
			_occlusionCam.GetComponent<Camera> ().cullingMask = 1 << LAYER_OCCLUSION;
		}

		void UpdateMesh () {
			UpdateBlendMesh ();
			UpdateMaskMesh ();
			UpdateOcclusionMesh ();
		}

		void UpdateBlendMesh () {
			var nScreens = _nRows * _nCols;
			var nIndices = 54 * nScreens;
			if (_blendMesh.vertexCount != nIndices) {
				_blendMesh.Clear ();
				_blendMesh.vertices = new Vector3[nIndices];
				_blendMesh.uv = new Vector2[nIndices];
				_blendMesh.uv2 = new Vector2[nIndices];
				_blendMesh.triangles = new int[nIndices];
				_blendMesh.colors = new Color[nIndices];
			}
			var vertices = _blendMesh.vertices;
			var uv = _blendMesh.uv;
			var uv2 = _blendMesh.uv2;
			var triangles = _blendMesh.triangles;
			var colors = _blendMesh.colors;
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
					var vBase = new Vector3 (x * screenSize.x, y * screenSize.y, 0f);
					float x0 = vBase.x, x1 = vBase.x + b0.x, x2 = vBase.x + screenSize.x - b1.x, x3 = vBase.x + screenSize.x;
					float y0 = vBase.y, y1 = vBase.y + b0.y, y2 = vBase.y + screenSize.y - b1.y, y3 = vBase.y + screenSize.y;
					var screenVertices = new Vector3[] {
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
					};
					x0 = uvBase.x;
					x1 = uvBase.x + b0.x;
					x2 = uvBase.x + screenSize.x - b1.x;
					x3 = uvBase.x + screenSize.x;
					y0 = uvBase.y;
					y1 = uvBase.y + b0.y;
					y2 = uvBase.y + screenSize.y - b1.y;
					y3 = uvBase.y + screenSize.y;
					var screenUv = new Vector2[] {
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
					};
					for (var j = 0; j < SCREEN_INDICES.Length; j++) {
						var i = SCREEN_INDICES [j];
						vertices [iTriangle] = screenVertices [i];
						uv [iTriangle] = screenUv [i];
						uv2 [iTriangle] = UV2 [i];
						colors [iTriangle] = COLORS [j / 6];
						triangles [iTriangle] = iTriangle;
						iTriangle++;
					}
					iScreen++;
					uvBase += new Vector2 (screenSize.x - b1.x, xLast ? (screenSize.y - b1.y) : 0f);
				}
			}
			_blendMesh.vertices = vertices;
			_blendMesh.uv = uv;
			_blendMesh.uv2 = uv2;
			_blendMesh.colors = colors;
			_blendMesh.triangles = triangles;
			_blendMesh.RecalculateBounds ();
		}

		void UpdateMaskMesh () {
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
			var screenSize = new Vector2 (1f / _nCols, 1f / _nRows);
			for (var y = 0; y < _nRows; y++) {
				for (var x = 0; x < _nCols; x++) {
					var i = x + y * _nCols;
					var iv = 4 * i;
					var it = 6 * i;
					var mask = data.Masks [i];
					var offset = new Vector2 (x * screenSize.x, y * screenSize.y);
					var v0 = offset + Vector2.Scale (mask.bl, screenSize);
					var v1 = offset + Vector2.Scale (mask.br, screenSize);
					var v2 = offset + Vector2.Scale (mask.tl, screenSize);
					var v3 = offset + Vector2.Scale (mask.tr, screenSize);
					vertices [iv] = v0;
					uv [iv] = v0 + mask.uvOffset;
					vertices [iv + 1] = v1;
					uv [iv + 1] = v1 + mask.uvOffset;
					vertices [iv + 2] = v2;
					uv [iv + 2] = v2 + mask.uvOffset;
					vertices [iv + 3] = v3;
					uv [iv + 3] = v3 + mask.uvOffset;
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

		void UpdateOcclusionMesh () {
			var nScreens = _nRows * _nCols;
			var nVertices = 16 * nScreens;
			var nIndices = 54 * nScreens;
			if (_occlusionMesh.vertexCount != nVertices) {
				_occlusionMesh.Clear ();
				_occlusionMesh.vertices = new Vector3[nVertices];
				_occlusionMesh.uv = new Vector2[nVertices];
				_occlusionMesh.uv2 = new Vector2[nVertices];
				_occlusionMesh.triangles = new int[nIndices];
			}

			var vertices = _occlusionMesh.vertices;
			var uv = _occlusionMesh.uv;
			var uv2 = _occlusionMesh.uv2;
			var triangles = _occlusionMesh.triangles;
			var screenSize = new Vector2 (1f / _nCols, 1f / _nRows);
			for (var y = 0; y < _nRows; y++) {
				for (var x = 0; x < _nCols; x++) {
					var iv = 16 * (x + y * _nCols);
					var it = 54 * (x + y * _nCols);
					var iScreen = x + y * _nCols;
					var occlusion = data.Occlusions [iScreen];
					var inside = occlusion.inside;
					var offset = new Vector2 (x * screenSize.x, y * screenSize.y);
					var uvsScreen = new Vector2[] {
						new Vector2 (0f, 0f),       new Vector2 (inside.x, 0f),       new Vector2 (inside.z, 0f),       new Vector2 (1f, 0f),
						new Vector2 (0f, inside.y), new Vector2 (inside.x, inside.y), new Vector2 (inside.z, inside.y), new Vector2 (1f, inside.y),
						new Vector2 (0f, inside.w), new Vector2 (inside.x, inside.w), new Vector2 (inside.z, inside.w), new Vector2 (1f, inside.w),
						new Vector2 (0f, 1f),       new Vector2 (inside.x, 1f),       new Vector2 (inside.z, 1f),       new Vector2 (1f, 1f)
					};
					for (var i = 0; i < 16; i++) {
						var currUv = offset + Vector2.Scale (uvsScreen [i], screenSize);
						vertices [iv + i] = (Vector3)currUv;
						uv [iv + i] = currUv;
						uv2 [iv + i] = UV2 [i];
					}
					for (var i = 0; i < 54; i++)
						triangles [it + i] = iv + SCREEN_INDICES [i];
				}
			}

			_occlusionMesh.vertices = vertices;
			_occlusionMesh.uv = uv;
			_occlusionMesh.uv2 = uv2;
			_occlusionMesh.triangles = triangles;
			_occlusionMesh.RecalculateBounds ();
			_occlusionMesh.RecalculateNormals ();
		}

		void UpdateGUI () {
			if (_uiN == null) {
				_guiWindowPos = new Rect (0.5f * (Screen.width - WINDOW_SIZE.x), 0.5f * (Screen.height - WINDOW_SIZE.y), WINDOW_SIZE.x, WINDOW_SIZE.y);

				_uiN = new UIInt (_nCols);
				_uiM = new UIInt (_nRows);
				_uiHBlendings = new UIFloat[0];
				_uiVBlendings = new UIFloat[0];
                _maskImageToggle = data.MaskImageToggle;
				_maskImagePath = data.MaskImagePath;
				_uiMasks = new UIFloat[8];
				LoadScreenData (SelectedScreen ());
				_guiRects = new GUIVector[_rects.Length];
				for (var i = 0; i < _guiRects.Length; i++)
					_guiRects [i].InitOnce (string.Format ("{0}", i), _rects [i]);
			}

			_uiN.Value = Mathf.Clamp (_uiN.Value, 1, 10);
			_uiM.Value = Mathf.Clamp (_uiM.Value, 1, 10);
			if (_uiN.Value != _nCols || _uiM.Value != _nRows) {
				_uiN.Value = _nCols = _uiN.Value;
				_uiM.Value = _nRows = _uiM.Value;
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

            data.MaskImageToggle = _maskImageToggle;
			data.MaskImagePath = _maskImagePath;

			SaveScreenData (SelectedScreen ());

			var nScreens = _nCols * _nRows;
			if (_maskSelections == null || _maskSelections.Length != nScreens) {
				_maskSelections = new string[nScreens];
				var i = 0;
				for (var y = _nRows - 1; y >= 0; y--)
					for (var x = 0; x < _nCols; x++)
						_maskSelections [i++] = string.Format ("{0},{1}", x, y);
			}

			for (var i = 0; i < _rects.Length; i++) {
				var rect = _rects [i];
				for (var j = 0; j < 4; j++)
					data.Rects [4 * i + j] = rect [j];
			}
		}

        void UpdateImage () {
            if (!_maskImageLoading)
                StartCoroutine (LoadMaskImage ());
        }
        #endregion

        #region Save/Load
		void Load () {
            var path = MakePath (config);
			if (File.Exists (path)) {
				var serializer = Data.GetXmlSerializer ();
				using (var reader = new StreamReader (path)) {
					data = (Data)serializer.Deserialize (reader);
				}
			}
			data.CheckInit ();
		}
		void Save () {
            using (var writer = new StreamWriter (MakePath(config))) {
				var serializer = Data.GetXmlSerializer ();
				serializer.Serialize (writer, data);
			}
		}
        string MakePath(string file) {
            var dir = Application.streamingAssetsPath;
            switch (configFolder) {
            case ConfigFolderEnum.MyDocuments:
                dir = System.Environment.GetFolderPath (System.Environment.SpecialFolder.MyDocuments);
                break;
            }
            return Path.Combine (dir, file);
        }
        #endregion

        #region Screen
		int SelectedScreen () {
			var selX = _selectedMask % _nCols;
			var selY = (_nRows - 1) - _selectedMask / _nCols;
			var selScreen = Mathf.Clamp (selX + selY * _nCols, 0, (_nCols * _nRows - 1));
			return selScreen;
		}

		void SaveScreenData (int selScreen) {
			var mask = data.Masks [selScreen];

			for (var i = 0; i < _uiMasks.Length; i++)
				_uiMasks [i].Value = Mathf.Clamp01 (_uiMasks [i].Value);

			mask.bl = new Vector2 (_uiMasks [0].Value, _uiMasks [1].Value);
			mask.br = new Vector2 (_uiMasks [2].Value, _uiMasks [3].Value);
			mask.tl = new Vector2 (_uiMasks [4].Value, _uiMasks [5].Value);
			mask.tr = new Vector2 (_uiMasks [6].Value, _uiMasks [7].Value);

			mask.uvOffset = new Vector2 (_uiUvU.Value, _uiUvV.Value);

			data.Masks [selScreen] = mask;

			var occlusion = data.Occlusions [selScreen];
			occlusion.inside = _guiOcclusion.Data.Value;
		}

		void LoadScreenData (int selScreen) {
			var mask = data.Masks [selScreen];
			_uiMasks [0] = new UIFloat (mask.bl.x);
			_uiMasks [1] = new UIFloat (mask.bl.y);
			_uiMasks [2] = new UIFloat (mask.br.x);
			_uiMasks [3] = new UIFloat (mask.br.y);
			_uiMasks [4] = new UIFloat (mask.tl.x);
			_uiMasks [5] = new UIFloat (mask.tl.y);
			_uiMasks [6] = new UIFloat (mask.tr.x);
			_uiMasks [7] = new UIFloat (mask.tr.y);

			_uiUvU = new UIFloat (mask.uvOffset.x);
			_uiUvV = new UIFloat (mask.uvOffset.y);

			var occlusion = data.Occlusions [selScreen];
			_guiOcclusion.Invalidate ();
			_guiOcclusion.InitOnce ("Occlusion", occlusion.inside);
		}
        #endregion

		IEnumerator LoadMaskImage () {
            _maskImageLoading = true;
            var path = MakePath(data.MaskImagePath);

            if (!File.Exists (path)) {
                Debug.LogFormat ("Mask Image not found at {0}", path);
                Destroy (_maskImageTex);
            } else {
                var lastWriteTime = File.GetLastWriteTime (path);
                if (lastWriteTime != _maskImageWriteTime) {
                    Debug.LogFormat ("Load Mask Image at : {0}", path);
                    _maskImageWriteTime = lastWriteTime;

                    var www = new WWW ("file://" + path);
                    yield return www;

                    Destroy (_maskImageTex);
                    _maskImageTex = www.textureNonReadable;
                    _maskImageTex.wrapMode = TextureWrapMode.Clamp;
                    _maskImageTex.filterMode = FilterMode.Bilinear;
                }
            }

            _maskImageLoading = false;
		}

        #region Classes
		[System.Serializable]
		public class Data {
			public float[] RowOverlaps;
			public float[] ColOverlaps;
			public string MaskImagePath;
			public bool MaskImageToggle;

			public Mask[] Masks;
			public float[] Rects;
			public Occlusion[] Occlusions;

			public Data () {
				Reset (1, 1);
			}

			public void CheckInit () {
				if (RowOverlaps == null || ColOverlaps == null)
					Reset (1, 1);

				var nCols = ColOverlaps.Length + 1;
				var nRows = RowOverlaps.Length + 1;
				if (Masks == null || Masks.Length != (nCols * nRows)
				    || Rects == null || Rects.Length != 4 * NUM_RECTS
				    || Occlusions == null || Occlusions.Length != (nCols * nRows))
						Reset (nCols, nRows);
			}

			public void Reset (int nCols, int nRows) {
				var oldColOverlaps = ColOverlaps;
				var oldRowOverlaps = RowOverlaps;
				var oldMasks = Masks;
				var oldRects = Rects;
				var oldOcclusions = Occlusions;

				ColOverlaps = new float[nCols - 1];
				RowOverlaps = new float[nRows - 1];

				MaskImagePath = "MaskImage.png";
				MaskImageToggle = false;

				Masks = new Mask[nCols * nRows];
				for (var i = 0; i < Masks.Length; i++)
					Masks [i] = new Mask (Vector2.zero, Vector2.one);

				Rects = new float[4 * NUM_RECTS];

				Occlusions = new Occlusion[nCols * nRows];
				for (var i = 0; i < Occlusions.Length; i++)
					Occlusions [i] = new Occlusion ();

				if (oldColOverlaps != null)
						System.Array.Copy (oldColOverlaps, ColOverlaps, Mathf.Min (oldColOverlaps.Length, ColOverlaps.Length));
				if (oldRowOverlaps != null)
						System.Array.Copy (oldRowOverlaps, RowOverlaps, Mathf.Min (oldRowOverlaps.Length, RowOverlaps.Length));
				if (oldMasks != null)
						System.Array.Copy (oldMasks, Masks, Mathf.Min (oldMasks.Length, Masks.Length));
				if (oldRects != null)
						System.Array.Copy (oldRects, Rects, Mathf.Min (oldRects.Length, Rects.Length));
				if (oldOcclusions != null)
						System.Array.Copy (oldOcclusions, Occlusions, Mathf.Min (oldOcclusions.Length, Occlusions.Length));
			}

			public static XmlSerializer GetXmlSerializer () {
				return new XmlSerializer (typeof(Data));
			}

			[System.Serializable]
			public class Mask {
				public Vector2 bl, br, tl, tr;
				public Vector2 uvOffset;

				public Mask () {}

				public Mask (Vector2 bottomLeft, Vector2 screenSize) {
					this.bl = bottomLeft;
					this.br = bottomLeft + new Vector2 (screenSize.x, 0f);
					this.tl = bottomLeft + new Vector2 (0f, screenSize.y);
					this.tr = bottomLeft + screenSize;

					this.uvOffset = Vector2.zero;
				}
			}

			[System.Serializable]
			public class Occlusion {
				public Vector4 inside;

				public Occlusion () {
						this.inside = new Vector4 (0f, 0f, 1f, 1f);
				}
			}
		}
        #endregion
	}
}