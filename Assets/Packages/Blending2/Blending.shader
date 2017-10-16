Shader "Hidden/Blending" {
	Properties  {
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader {
		Cull Off ZWrite Off ZTest Always

		Pass {
			CGPROGRAM
			#pragma target 4.5
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata {
				uint vid : SV_VertexID;
				uint iid : SV_InstanceID;
			};

			struct v2f {
				float2 uv : TEXCOORD0;
				float2 edge : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			static const int _Indices[54] = {
				0,5,1, 0,4,5,   1,6,2, 1,5,6,  2,7,3, 2,6,7, 
				4,9,5, 4,8,9,   5,10,6, 5,9,10,  6,11,7, 6,10,11,
				8,13,9, 8,12,13,  9,14,10, 9,13,14,  10,15,11, 10,14,15
			};
			static const float2 _Uvs[16] = { 
				float2(0,0), float2(0,0), float2(1,0), float2(1,0),
				float2(0,0), float2(0,0), float2(1,0), float2(1,0),
				float2(0,1), float2(0,1), float2(1,1), float2(1,1),
				float2(0,1), float2(0,1), float2(1,1), float2(1,1)
			};
			static const float2 _Edges[16] = {
				float2(0,0), float2(1,0), float2(1,0), float2(0,0),
				float2(0,1), float2(1,1), float2(1,1), float2(0,1),
				float2(0,1), float2(1,1), float2(1,1), float2(0,1),
				float2(0,0), float2(1,0), float2(1,0), float2(0,0)
			};

			sampler2D _MainTex;
			float4x4 _WorldToScreenMatrix;
			StructuredBuffer<float4x4> _UVToWorldMatrices;
			StructuredBuffer<float4x4> _EdgeToLocalUVMatrices;
			StructuredBuffer<float4x4> _LocalToWorldUVMatrices;

			v2f vert (appdata v) {
				v2f o;
				
				int vindex = _Indices[v.vid];
				float2 uv = _Uvs[vindex];
				float2 edge = _Edges[vindex];

				float4x4 worldMat = _UVToWorldMatrices[v.iid];
				float4x4 edgeMat = _EdgeToLocalUVMatrices[v.iid];
				float4x4 uvMat = _LocalToWorldUVMatrices[v.iid];

				uv = lerp(uv, mul(edgeMat, float4(uv, 0, 1)).xy, edge);
				float2 worldUv = mul(uvMat, float4(uv, 0, 1)).xy;

				float2 tensionx = float2(1 - uv.x, uv.x);
				float4 tension = float4(tensionx * (1 - uv.y), tensionx * uv.y);
				float4 pos = float4(mul(worldMat, tension).xy, 0, 1);

				pos = float4(mul(_WorldToScreenMatrix, float4(pos.xyz, 1)).xyz, 1);

				o.vertex = pos;
				o.uv = worldUv;
				o.edge = edge;
				return o;
			}

			float4 frag (v2f i) : SV_Target {
				float4 col = tex2D(_MainTex, i.uv);

				#if defined(OUTPUT_CORNER_UV)

				return float4(frac(i.uv), 0, 1);

				#else

				float g = (i.edge.x * i.edge.y);
				#ifdef UNITY_COLORSPACE_GAMMA
				g = LinearToGammaSpace(g);
				#endif
				return col * g;

				#endif
			}
			ENDCG
		}
	}
}
