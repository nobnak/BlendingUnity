using UnityEngine;
using System.Collections;
using nobnak.GUI;

public class Blender : MonoBehaviour {
	public const int LAYER_BLEND = 31;

	public static readonly int[] SCREEN_INDICES = new int[]{
		0,  5, 1, 0,  4,  5, 1,  6,  2, 1,  5,  6,  2,  7,  3,  2,  6,  7,
		4,  9, 5, 4,  8,  9, 5, 10,  6, 5,  9, 10,  6, 11,  7,  6, 10, 11,
		8, 13, 9, 8, 12, 13, 9, 14, 10, 9, 13, 14, 10, 15, 11, 10, 14, 15 };
	public static readonly Vector2[] UV2 = new Vector2[]{
		Vector2.zero, new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.zero,
		new Vector2(0f, 1f), Vector2.one, Vector2.one, new Vector2(0f, 1f),
		new Vector2(0f, 1f), Vector2.one, Vector2.one, new Vector2(0f, 1f),
		Vector2.zero, new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.zero };

	public Data data;
	public Material blendMat;

	private Capture _capture;
	private GameObject _blendCam;
	private GameObject _blendGo;
	private Mesh _blendMesh;

	void OnDestroy() {
		Destroy(_capture.gameObject);
		Destroy(_blendCam);
		Destroy(_blendMesh);
		Destroy(_blendGo);
	}
	void Update() {
		if (_capture == null) {
			var captureGo = new GameObject("Capture Camera", typeof(Camera), typeof(Capture));
			captureGo.transform.parent = transform;
			_capture = captureGo.GetComponent<Capture>();
		}
		blendMat.mainTexture = _capture.Target;

		if (_blendCam == null) {
			_blendCam = new GameObject("Blend Camera", typeof(Camera));
			_blendCam.transform.parent = transform;
			_blendCam.transform.localPosition = new Vector3(0f, 0f, -1f);
			_blendCam.transform.localRotation = Quaternion.identity;
			_blendCam.camera.depth = 100;
			_blendCam.camera.orthographic = true;
			_blendCam.camera.orthographicSize = 0.5f;
			_blendCam.camera.aspect = 1f;
			_blendCam.camera.clearFlags = CameraClearFlags.SolidColor;
		}
		foreach (var cam in Camera.allCameras)
			cam.cullingMask &= ~(1 << LAYER_BLEND);
		_blendCam.camera.cullingMask = 1 << LAYER_BLEND;

		if (_blendGo == null) {
			_blendGo = new GameObject("Blend");
			_blendGo.transform.parent = transform;
			_blendGo.transform.localPosition = new Vector3(-0.5f, -0.5f, 0f);
			_blendGo.transform.localRotation = Quaternion.identity;
			_blendGo.transform.localScale = Vector3.one;
			_blendGo.layer = LAYER_BLEND;
			_blendGo.AddComponent<MeshRenderer>().sharedMaterial = blendMat;
			_blendGo.AddComponent<MeshFilter>().sharedMesh = _blendMesh = new Mesh();
			_blendMesh.MarkDynamic();
		}
		var nRows = data.RowOffsets.Length;
		var nCols = data.ColOffsets.Length;
		var nScreens = nRows * nCols;
		var nVertices = 16 * nScreens;
		var nIndices = 54 * nScreens;
		if (_blendMesh.vertexCount != nVertices) {
			_blendMesh.Clear();
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
		var screenSize = new Vector2(1f / nCols, 1f / nRows);
		for (var y = 0; y < nRows; y++) {
			var yFirst = (y == 0);
			var yLast = (y + 1 == nRows);
			for (var x = 0; x < nCols; x++) {
				var xFirst = (x == 0);
				var xLast = (x + 1 == nCols);
				var b0 = new Vector2((xFirst ? 0f : Mathf.Max(0f, screenSize.x - (data.ColOffsets[x] - data.ColOffsets[x-1]))),
				                     (yFirst ? 0f : Mathf.Max(0f, screenSize.y - (data.RowOffsets[y] - data.RowOffsets[y-1]))));
				var b1 = new Vector2((xLast ? 0f : Mathf.Max(0f, screenSize.x - (data.ColOffsets[x+1] - data.ColOffsets[x]))),
				                     (yLast ? 0f : Mathf.Max(0f, screenSize.y - (data.RowOffsets[y+1] - data.RowOffsets[y]))));
				var vertexIndexBase = iScreen * 16;

				var vb = new Vector3(x * screenSize.x, y * screenSize.y, 0f);
				float x0 = vb.x, x1 = vb.x + b0.x, x2 = vb.x + screenSize.x - b1.x, x3 = vb.x + screenSize.x;
				float y0 = vb.y, y1 = vb.y + b0.y, y2 = vb.y + screenSize.y - b1.y, y3 = vb.y + screenSize.y;
				System.Array.Copy(new Vector3[]{
						new Vector3(x0, y0, 0f), new Vector3(x1, y0, 0f), new Vector3(x2, y0, 0f), new Vector3(x3, y0, 0f),
						new Vector3(x0, y1, 0f), new Vector3(x1, y1, 0f), new Vector3(x2, y1, 0f), new Vector3(x3, y1, 0f),
						new Vector3(x0, y2, 0f), new Vector3(x1, y2, 0f), new Vector3(x2, y2, 0f), new Vector3(x3, y2, 0f),
						new Vector3(x0, y3, 0f), new Vector3(x1, y3, 0f), new Vector3(x2, y3, 0f), new Vector3(x3, y3, 0f)},
					0, vertices, vertexIndexBase, 16);

				var uvb = new Vector2(data.ColOffsets[x], data.RowOffsets[y]);
				x0 = uvb.x; x1 = uvb.x + b0.x; x2 = uvb.x + screenSize.x - b1.x; x3 = uvb.x + screenSize.x;
				y0 = uvb.y; y1 = uvb.y + b0.y; y2 = uvb.y + screenSize.y - b1.y; y3 = uvb.y + screenSize.y;
				System.Array.Copy(new Vector2[]{
						new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x2, y0), new Vector2(x3, y0),
						new Vector2(x0, y1), new Vector2(x1, y1), new Vector2(x2, y1), new Vector2(x3, y1),
						new Vector2(x0, y2), new Vector2(x1, y2), new Vector2(x2, y2), new Vector2(x3, y2),
						new Vector2(x0, y3), new Vector2(x1, y3), new Vector2(x2, y3), new Vector2(x3, y3)},
					0, uv, vertexIndexBase, 16);

				for (var i = 0; i < UV2.Length; i++)
					uv2[vertexIndexBase + i] = UV2[i];
				foreach (var i in SCREEN_INDICES)
					triangles[iTriangle++] = vertexIndexBase + i;

				iScreen++;
			}
		}
		_blendMesh.vertices = vertices;
		_blendMesh.uv = uv;
		_blendMesh.uv2 = uv2;
		_blendMesh.triangles = triangles;
	}

	[System.Serializable]
	public class Data {
		public float[] RowOffsets;
		public float[] ColOffsets;

		public void CheckInit() {
			if (RowOffsets == null || RowOffsets.Length == 0)
				RowOffsets = new float[]{ 0f };
			if (ColOffsets == null || ColOffsets.Length == 0)
				ColOffsets = new float[]{ 0f };
		}
	}
}
