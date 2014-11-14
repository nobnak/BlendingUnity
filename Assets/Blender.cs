using UnityEngine;
using System.Collections;
using nobnak.GUI;

public class Blender : MonoBehaviour {
	public const string SHADER_COLOR = "_Color";
	public const float ALPHA = 0.5f;
	public static readonly Color[] COLORS = new Color[]{ 
		new Color(0.149f, 0.451f, 0.337f, ALPHA), 
		new Color(0.165f, 0.306f, 0.431f, ALPHA),
		new Color(0.667f, 0.482f, 0.224f, ALPHA),
		new Color(0.667f, 0.361f, 0.224f, ALPHA) };

	public Data data;
	public Material screenAreaMat;

	void OnGUI() {
		if (!Event.current.type.Equals(EventType.Repaint))
			return;

		data.CheckInit();
		var size = new Vector2(1f / data.ColOffsets.Length, 1f / data.RowOffsets.Length);

		GL.PushMatrix();
		GL.LoadIdentity();
		GL.LoadOrtho();

		var iColor = 0;
		for (var y = 0; y < data.ColOffsets.Length; y++) {
			for (var x = 0; x < data.RowOffsets.Length; x++) {
				var offset = new Vector2(data.ColOffsets[x], data.RowOffsets[y]);
				var c = COLORS[iColor = ++iColor % COLORS.Length];
				screenAreaMat.SetColor(SHADER_COLOR, c);
				screenAreaMat.SetPass(0);
				GL.Begin(GL.QUADS);
				GL.Vertex3(offset.x, offset.y, 0f);
				GL.Vertex3(offset.x, offset.y + size.y, 0f);
				GL.Vertex3(offset.x + size.x, offset.y + size.y, 0f);
				GL.Vertex3(offset.x + size.x, offset.y, 0f);
				GL.End();
			}
		}

		GL.PopMatrix();
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
