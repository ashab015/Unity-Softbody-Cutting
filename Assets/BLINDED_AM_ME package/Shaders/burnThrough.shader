Shader "Custom/burnThrough" {
	Properties {
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_MainAlpha ("Alpha (A)", 2D) = "white" {}
		_BurningTex ("Burning (RGB)", 2D) = "white" {}
		_Cut("Alpha cut", Range(0,1)) = 0.5
		_CutDamper("Alpha Cut Damper", Range(0.01,0.1)) = 0.01
		_Burn("Burn cutoff", Range(0,1)) = 0.1
		_BurnDamper("Burn Damper", Range(0.01,1)) = 0.01
		
	}
	SubShader {
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows alpha:fade
		
		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _MainAlpha;
		sampler2D _BurningTex;
		
		float _Cut;
		float _CutDamper;
		float _Burn;
		float _BurnDamper;

		struct Input {
			float2 uv_MainTex;
			float2 uv_MainAlpha;
			float2 uv_BurningTex;
		};

		void surf (Input IN, inout SurfaceOutputStandard o) {
			
			// this acts like alphaTest
			//max(0, sign(alphacolor.a - _Cut));
			
			fixed4 maincolor = tex2D (_MainTex, IN.uv_MainTex);
			fixed4 burningcolor = tex2D (_BurningTex, IN.uv_BurningTex);
			float   alphaAmount = tex2D (_MainAlpha, IN.uv_MainAlpha).a;
			
			float burning_Mask = (alphaAmount - (1-_Burn))/(_BurnDamper);
			burning_Mask = lerp(0,1,burning_Mask);
			burning_Mask = clamp(burning_Mask, 0, 1);
			
			
			float alpha_Mask = (alphaAmount - (1-_Cut))/(_CutDamper);
			alpha_Mask = 1 - lerp(0,1,alpha_Mask);
			alpha_Mask = clamp(alpha_Mask, 0, 1);
			
			o.Albedo = maincolor.rgb + (burning_Mask * burningcolor.rgb);
			o.Emission = burningcolor.rgb * burning_Mask * alpha_Mask;
			o.Alpha = alpha_Mask;
			
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
