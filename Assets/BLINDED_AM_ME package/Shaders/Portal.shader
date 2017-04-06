Shader "Unlit/Portal"
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		[HideInInspector] _PortalTex ("", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
 
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 port : TEXCOORD1;
				float4 pos : SV_POSITION;
			};
			float4 _MainTex_ST;
			v2f vert(float4 pos : POSITION, float2 uv : TEXCOORD0)
			{
				v2f o;
				o.pos = mul (UNITY_MATRIX_MVP, pos);
				o.uv = TRANSFORM_TEX(uv, _MainTex);
				o.port = ComputeScreenPos (o.pos);
				return o;
			}
			sampler2D _MainTex;
			sampler2D _PortalTex;
			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 tex = tex2D(_MainTex, i.uv);
				fixed4 port = tex2Dproj(_PortalTex, UNITY_PROJ_COORD(i.port));
				return tex * port;
			}
			ENDCG
	    }
	}
}
