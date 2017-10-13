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
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;

			StructuredBuffer<int> _Indices;
			StructuredBuffer<float2> _Uvs;
			StructuredBuffer<float4x4> _CornerToWorldMatrices;

			float4x4 _WorldToScreenMatrix;

			v2f vert (appdata v) {
				v2f o;
				
				int vindex = _Indices[v.vid];
				float2 uv = _Uvs[vindex];
				float4x4 cornerMat = _CornerToWorldMatrices[v.iid];

				float2 pairx = float2(1 - uv.x, uv.x);
				float4 nest = float4(pairx * (1 - uv.y), pairx * uv.y);
				float4 pos = float4(mul(cornerMat, nest).xy, 0, 1);

				pos = float4(mul(_WorldToScreenMatrix, float4(pos.xyz, 1)).xyz, 1);

				o.vertex = pos;
				o.uv = uv;
				return o;
			}

			float4 frag (v2f i) : SV_Target {
				float4 col = tex2D(_MainTex, i.uv);
				#if defined(OUTPUT_CORNER_UV)
				return float4(frac(i.uv), 0, 1);
				#else
				return col;
				#endif
			}
			ENDCG
		}
	}
}
