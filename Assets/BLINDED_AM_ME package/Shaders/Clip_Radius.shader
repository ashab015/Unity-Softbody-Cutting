Shader "Clip/Radius" {
    Properties {
    
      _Radius ("Radius", float) = 0.25
      _Origin ("Origin", Vector) = (0,0,0,0)
      _ConvertEmission ("Convert Emission", Range(0,1)) = 0.5
      _ConvertDistance ("Conversion Distance", float) = 0.1
      _Conversion ("Conversion (RGB)", 2D) = "white" {}
    
      _MainTex ("Main Texture", 2D) = "white" {}
      _BumpMap ("Bumpmap", 2D) = "bump" {}
      _Glossiness ("Smoothness", Range(0,1)) = 0.5
	  _Metallic ("Metallic", Range(0,1)) = 0.0
	  
    }
    SubShader {
      Tags { "RenderType" = "Opaque" }
      Cull Off
      CGPROGRAM
      // Physically based Standard lighting model, and enable shadows on all light types
	  #pragma surface surf Standard fullforwardshadows

	  // Use shader model 3.0 target, to get nicer looking lighting
	  #pragma target 3.0
		
      struct Input {
          float2 uv_MainTex;
          float2 uv_Conversion;
          float2 uv_BumpMap;
          float3 worldPos;
      };
      
      half _Glossiness;
	  half _Metallic;
	  half _ConvertDistance;
	  half _ConvertEmission;
      half _Radius;
      
      float3 _Origin;
      
      sampler2D _MainTex;
      sampler2D _BumpMap;
      sampler2D _Conversion;
      
      void surf (Input IN, inout SurfaceOutputStandard o) {
      
          half dist = distance( IN.worldPos, _Origin);
         //clip (frac((IN.worldPos.y+IN.worldPos.z*0.1) * 5) - 0.5);
          clip (dist - _Radius);
          
          // min = 0 // value = dist // max = _ConvertDistance
          float convert_mask = (dist - _Radius)/ _ConvertDistance;
		  convert_mask = clamp(convert_mask, 0, 1);
          
          fixed4 albedo = tex2D (_MainTex, IN.uv_MainTex);
          albedo *= convert_mask;
          
          fixed4 convert = tex2D (_Conversion, IN.uv_Conversion);
          convert *= 1.0 - convert_mask;
          
          o.Albedo = albedo.rgb + convert.rgb;
          o.Emission = convert.rgb * _ConvertEmission;
          o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_BumpMap));
          o.Metallic = _Metallic;
		  o.Smoothness = _Glossiness;
		  o.Alpha = albedo.a + convert.a;
          
      }
      ENDCG
    } 
    Fallback "Diffuse"
  }