// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/UvColor" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;

			struct Input {
				float4 vertex : POSITION;
				float4 color : COlor;
			};
			
			Input vert(Input IN) {
				Input OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.color = IN.color;
				return OUT;
			}
			float4 frag(Input IN) : COLOR {
				return IN.color;
			}
			ENDCG
		}
	} 
}
