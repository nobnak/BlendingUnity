using UnityEngine;
using System.Collections;
using System.IO;
using nobnak.GUI;
using System.Xml.Serialization;
using System;
using System.Linq;
using UnityEngine.Assertions;

namespace nobnak.Blending {

    public class Blender : BlenderBase {
        public const int DEPTH_CAPTURE = 90;

        GameObject _captureCam;
        Capture _capture;
        Capture _blend;
        Capture _mask;

        protected override void Start () {
            _captureCam = new GameObject ("Capture Camera", typeof(Camera), typeof(Capture));
            _captureCam.transform.SetParent (transform, false);
            _captureCam.GetComponent<Camera> ().depth = DEPTH_CAPTURE;

            base.Start ();

            _capture = _captureCam.GetComponent<Capture> ();
            _blend = _blendCamera.gameObject.GetComponent<Capture> ();
            _mask = _maskCamera.gameObject.GetComponent<Capture> ();

            _blendCamera.clearFlags = CameraClearFlags.SolidColor;
            _maskCamera.clearFlags = CameraClearFlags.SolidColor;
            _occlusionCamera.clearFlags = CameraClearFlags.SolidColor;

        }

        protected override Texture GetCaptureTex () {
            return _capture.GetTarget ();
        }

        protected override Texture GetBlendedTex () {
            return _blend.GetTarget ();
        }


        protected override Texture GetMaskedTex () {
            return _mask.GetTarget ();
        }

        protected override void OnDisable () {
            Destroy (_blend.gameObject);

            base.OnDisable ();
        }
    }



    public abstract class BlenderBase : MonoBehaviour {
        public const int LAYER_BLEND = 29;
        public const int LAYER_MASK = 30;
        public const int LAYER_OCCLUSION = 31;

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

        public enum ConfigFolderEnum {
            StreamingAssets = 0,
            MyDocuments

        }

        public ConfigFolderEnum configFolder;
        public string configFileName = "Blending.txt";

        int windowId { get { return gameObject.GetInstanceID (); } }

        public Data data;
        public Material blendMat;
        public Material maskMat;
        public Material occlusionMat;
        public Material vcolorMat;
        public KeyCode debugKey = KeyCode.E;
        public bool isExceedRightBottom = true;
        // ブレンド分右下部分が画面外にはみ出す。 false時は画面内に収まるが歪む

        protected Camera _blendCamera;
        Renderer _blendObjRenderer;
        Mesh _blendMesh;
        protected Camera _maskCamera;
        GameObject _maskObj;
        Mesh _maskMesh;
        protected Camera _occlusionCamera;
        GameObject _occlusionObj;
        Mesh _occlusionMesh;
        Vector4[] _rects;
        string[] _rectNames;


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
        UIFloat _uiOcclutionGamma;

        bool _maskImageToggle;
        bool _maskImageLoading = false;
        string _maskImagePath;
        Texture _maskImageTex;
        System.DateTime _maskImageWriteTime = System.DateTime.MinValue;

        public enum DebugMode {
            None = 0,
            ViewColor,
            Basic
        }

        DebugMode _debugMode;

        public DebugMode debugMode {
            get { return _debugMode; }
            set {
                if (value != _debugMode) {
                    _debugMode = value;
                    if (_debugMode == DebugMode.None)
                        Save ();
                }
            }
        }

        #region abstract

        protected abstract Texture GetCaptureTex ();

        protected abstract Texture GetBlendedTex ();

        protected abstract Texture GetMaskedTex ();

        #endregion


        protected virtual void OnDisable () {
            Destroy (_blendCamera.gameObject);
            Destroy (_blendObjRenderer.gameObject);
            Destroy (_blendMesh);
            Destroy (_maskCamera.gameObject);
            Destroy (_maskObj);
            Destroy (_maskMesh);
            Destroy (_occlusionCamera.gameObject);
            Destroy (_occlusionObj);
            Destroy (_maskImageTex);
        }

        protected virtual void Awake () {
            // 名付け済みレイヤーチェック（ほかの目的で使ってることが予想される）
            Assert.IsFalse (new[] { LAYER_BLEND, LAYER_MASK, LAYER_OCCLUSION }.Any (l => !string.IsNullOrEmpty (LayerMask.LayerToName (l))));

            // 複数インスタンスを許容するためマテリアルの自分用に作る
            blendMat = new Material (blendMat);
            maskMat = new Material (maskMat);
            occlusionMat = new Material (occlusionMat);
        }

        public void OnDestroy () {
            Destroy (blendMat);
            Destroy (maskMat);
            Destroy (occlusionMat);
        }

        protected virtual void Start () {
            Load ();
            CheckInit ();
            UpdateMesh ();
            UpdateMaterial ();
            UpdateImage ();
        }

        void Update () {

            if (Input.GetKeyDown (debugKey)) {
                int d = (int)debugMode;
                d = ++d % Enum.GetValues (typeof(DebugMode)).Length;
                debugMode = (DebugMode)d;
            }

            if (debugMode > 0) {
                CheckInit ();
                UpdateMesh ();
                UpdateMaterial ();
                UpdateImage ();
                UpdateGUI ();
            }

            blendMat.mainTexture = GetCaptureTex ();
            _blendObjRenderer.sharedMaterial = (debugMode == DebugMode.ViewColor ? vcolorMat : blendMat);
            maskMat.mainTexture = GetBlendedTex ();
            maskMat.SetTexture (SHADER_MASK_TEX, data.MaskImageToggle ? _maskImageTex : null);
            #if UNITY_5_4_OR_NEWER
            maskMat.SetVectorArray (SHADER_RECTS, _rects);
            #else
            for (var i = 0; i < _rects.Length; i++)
                maskMat.SetVector(_rectNames[i], _rects[i]);
            #endif
            occlusionMat.mainTexture = GetMaskedTex ();
        }

        void OnGUI () {
            if (debugMode == DebugMode.None)
                return;

            _guiWindowPos = GUILayout.Window (windowId, _guiWindowPos, DrawWindow, configFileName);
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

            GUILayout.Label ("---- Occlusion Gamma ----");
            _uiOcclutionGamma.StrValue = GUILayout.TextField (_uiOcclutionGamma.StrValue, TEXT_WIDTH);

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

        void _SetOrthoCameraParams (Camera camera, int depth) {
            camera.depth = depth;
            camera.orthographic = true;
            camera.orthographicSize = 0.5f;
            camera.aspect = 1f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
        }

        void CheckInit () {
            if (_blendCamera == null) {
                var blendCamObj = new GameObject ("Blend Camera", typeof(Camera), typeof(Capture));
                blendCamObj.transform.parent = transform;
                blendCamObj.transform.localPosition = new Vector3 (0f, 0f, 0f);
                blendCamObj.transform.localRotation = Quaternion.identity;

                _blendCamera = blendCamObj.GetComponent<Camera> ();
                _SetOrthoCameraParams (_blendCamera, DEPTH_BLEND);
            }

            if (_blendObjRenderer == null) {
                var blendObj = new GameObject ("Blend Obj");
                blendObj.transform.parent = transform;
                blendObj.transform.localPosition = new Vector3 (-0.5f, -0.5f, 1f);
                blendObj.transform.localRotation = Quaternion.identity;
                blendObj.transform.localScale = Vector3.one;
                blendObj.layer = LAYER_BLEND;
                _blendObjRenderer = blendObj.AddComponent<MeshRenderer> ();
                _blendObjRenderer.sharedMaterial = blendMat;
                blendObj.AddComponent<MeshFilter> ().sharedMesh = _blendMesh = new Mesh ();
                _blendMesh.MarkDynamic ();
            }

            if (_maskCamera == null) {
                var maskCamObj = new GameObject ("Mask Camera", typeof(Camera), typeof(Capture));
                maskCamObj.transform.parent = transform;
                maskCamObj.transform.localPosition = new Vector3 (2f, 0f, 0f);
                maskCamObj.transform.localRotation = Quaternion.identity;

                _maskCamera = maskCamObj.GetComponent<Camera> ();
                _SetOrthoCameraParams (_maskCamera, DEPTH_MASK);
            }

            if (_maskObj == null) {
                _maskObj = new GameObject ("Mask Obj");
                _maskObj.transform.parent = transform;
                _maskObj.transform.localPosition = new Vector3 (1.5f, -0.5f, 1f);
                _maskObj.transform.localRotation = Quaternion.identity;
                _maskObj.transform.localScale = Vector3.one;
                _maskObj.layer = LAYER_MASK;
                _maskObj.AddComponent<MeshRenderer> ().sharedMaterial = maskMat;
                _maskObj.AddComponent<MeshFilter> ().sharedMesh = _maskMesh = new Mesh ();
                _maskMesh.MarkDynamic ();
            }

            if (_occlusionCamera == null) {
                var go = new GameObject ("Occulusion Camera", typeof(Camera));
                go.transform.parent = transform;
                go.transform.localPosition = new Vector3 (4f, 0f, 0f);
                go.transform.localRotation = Quaternion.identity;

                _occlusionCamera = go.GetComponent<Camera> ();
                _SetOrthoCameraParams (_occlusionCamera, DEPTH_OCCLUSION);
            }

            if (_occlusionObj == null) {
                _occlusionObj = new GameObject ("Occulusion Obj");
                _occlusionObj.transform.parent = transform;
                _occlusionObj.transform.localPosition = new Vector3 (3.5f, -0.5f, 1f);
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


            var layerBlend = 1 << LAYER_BLEND;
            var layerMask = 1 << LAYER_MASK;
            var layerOccclusion = 1 << LAYER_OCCLUSION;
            var layers = new[] { layerBlend, layerMask, layerOccclusion };

            var layerFlags = layerBlend | layerMask | layerOccclusion;

            Camera.allCameras
                .Where (c => !layers.Any (layer => c.cullingMask == layer))
                .ToList ()
                .ForEach (c => c.cullingMask &= ~layerFlags);

            _blendCamera.cullingMask = 1 << LAYER_BLEND;
            _maskCamera.cullingMask = 1 << LAYER_MASK;
            _occlusionCamera.cullingMask = 1 << LAYER_OCCLUSION;
        }

        void UpdateMaterial () {
            UpdateOcclusionMaterial ();
        }

        void UpdateOcclusionMaterial () {
            occlusionMat.SetFloat ("_Gamma", data.OcclusionGamma);
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

            {
                var screenSize = new Vector2 (1f / _nCols, 1f / _nRows);

                var Create9SlicePlane = new Func<Vector2, Vector2, Vector2, Vector2[]> ((basePos, overlapLT, overlapRB) => {
                    float x0 = basePos.x;
                    float x1 = basePos.x + overlapLT.x;
                    float x2 = basePos.x + screenSize.x - overlapRB.x;
                    float x3 = basePos.x + screenSize.x;
                    float y0 = basePos.y;
                    float y1 = basePos.y + overlapLT.y;
                    float y2 = basePos.y + screenSize.y - overlapRB.y;
                    float y3 = basePos.y + screenSize.y;
                    return new[] {
                        new Vector2 (x0, y0), new Vector2 (x1, y0), new Vector2 (x2, y0), new Vector2 (x3, y0),
                        new Vector2 (x0, y1), new Vector2 (x1, y1), new Vector2 (x2, y1), new Vector2 (x3, y1),
                        new Vector2 (x0, y2), new Vector2 (x1, y2), new Vector2 (x2, y2), new Vector2 (x3, y2),
                        new Vector2 (x0, y3), new Vector2 (x1, y3), new Vector2 (x2, y3), new Vector2 (x3, y3)
                    };
                });

                var uvBase = Vector2.zero;
                var iTriangle = 0;
                for (var y = 0; y < _nRows; y++) {
                    var yFirst = (y == 0);
                    var yLast = (y + 1 == _nRows);

                    var overlapTop = yFirst ? 0f : (data.RowOverlaps [y - 1] * screenSize.y);
                    var overlapBottom = yLast ? 0f : (data.RowOverlaps [y] * screenSize.y);

                    for (var x = 0; x < _nCols; x++) {
                        var xFirst = (x == 0);
                        var xLast = (x + 1 == _nCols);

                        var overlapLeftTop = new Vector2 (xFirst ? 0f : (data.ColOverlaps [x - 1] * screenSize.x), overlapTop);
                        var overlapRightBottom = new Vector2 (xLast ? 0f : (data.ColOverlaps [x] * screenSize.x), overlapBottom);

                        var screenVertices = Create9SlicePlane (Vector2.Scale (new Vector2 (x, y), screenSize), overlapLeftTop, overlapRightBottom);
                        var screenUv = Create9SlicePlane (uvBase, overlapLeftTop, overlapRightBottom);

                        for (var j = 0; j < SCREEN_INDICES.Length; j++) {
                            var i = SCREEN_INDICES [j];
                            vertices [iTriangle] = screenVertices [i];
                            uv [iTriangle] = screenUv [i];
                            uv2 [iTriangle] = UV2 [i];
                            colors [iTriangle] = COLORS [j / 6];
                            triangles [iTriangle] = iTriangle;
                            iTriangle++;
                        }

                        uvBase.x += screenSize.x - overlapRightBottom.x;
                    }

                    uvBase.x = 0f;
                    uvBase.y += screenSize.y - overlapBottom;
                }

                // normalize uv
                if (!isExceedRightBottom) {
                    var uvMaxInv = new Vector2 (
                                       1f / uv.Select (v => v.x).Max (),
                                       1f / uv.Select (v => v.y).Max ()
                                   );

                    uv = uv.Select (v => Vector2.Scale (v, uvMaxInv)).ToArray ();
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
                //_guiWindowPos = new Rect (0.5f * (Screen.width - WINDOW_SIZE.x), 0.5f * (Screen.height - WINDOW_SIZE.y), WINDOW_SIZE.x, WINDOW_SIZE.y);

                _uiN = new UIInt (_nCols);
                _uiM = new UIInt (_nRows);
                _uiHBlendings = new UIFloat[0];
                _uiVBlendings = new UIFloat[0];
                _maskImageToggle = data.MaskImageToggle;
                _maskImagePath = data.MaskImagePath;
                _uiOcclutionGamma = new UIFloat (data.OcclusionGamma);
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

            data.OcclusionGamma = _uiOcclutionGamma.Value;

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
            if (!_maskImageLoading && !string.IsNullOrEmpty (data.MaskImagePath))
                StartCoroutine (LoadMaskImage ());
        }

        #region Save/Load

        void Load () {
            var path = MakePath (configFileName);
            if (File.Exists (path)) {
                var serializer = Data.GetXmlSerializer ();
                using (var reader = new StreamReader (path)) {
                    data = (Data)serializer.Deserialize (reader);
                }
            }
            data.CheckInit ();
        }

        void Save () {
            using (var writer = new StreamWriter (MakePath (configFileName))) {
                var serializer = Data.GetXmlSerializer ();
                serializer.Serialize (writer, data);
            }
        }

        string MakePath (string file) {
            var dir = Application.streamingAssetsPath;
            switch (configFolder) {
            case ConfigFolderEnum.MyDocuments:
                dir = System.Environment.GetFolderPath (System.Environment.SpecialFolder.MyDocuments);
                break;
            }
            return Path.Combine (dir, file);
        }

        #endregion

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

        IEnumerator LoadMaskImage () {
            _maskImageLoading = true;
            var path = MakePath (data.MaskImagePath);

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

        [System.Serializable]
        public class Data {
            public float[] RowOverlaps;
            public float[] ColOverlaps;
            public string MaskImagePath;
            public bool MaskImageToggle;

            public Mask[] Masks;
            public float[] Rects;
            public Occlusion[] Occlusions;
            public float OcclusionGamma;

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

                OcclusionGamma = 0.454f;
            }

            public static XmlSerializer GetXmlSerializer () {
                return new XmlSerializer (typeof(Data));
            }

            [System.Serializable]
            public class Mask {
                public Vector2 bl, br, tl, tr;
                public Vector2 uvOffset;

                public Mask () {
                }

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
    }
}
