// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Occlusion" {
	Properties {
		_MainTex ("Main Texture", 2D) = "black" {}
		_Gamma ("Gamma", Float) = 1
	}
	SubShader {
		Tags { "Queue" = "Overlay"  "PreviewType" = "Plane" }
		ZTest Always ZWrite Off Cull Off Fog { Mode Off }
		
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float _Gamma;

			struct Input {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
			};

			Input vert(Input IN) {
				Input OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.uv = IN.uv;
				OUT.uv2 = IN.uv2;
				return OUT;
			}
			
			float4 frag(Input IN) : COLOR {
				float4 c = tex2D(_MainTex, IN.uv);

				float2 w = smoothstep(0.0, 1.0, IN.uv2);
				c *= pow(w.x * w.y, _Gamma);
				return c;
			}
			ENDCG
		}
	} 
}
