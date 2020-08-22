Shader "Minecraft/Transparent Blocks" {

	Properties{

		_MainTex("BlockTextureAtlas", 2D) = "white" {}
	}

	SubShader{

		Tags{"Queue"="AlphaTest" "IgnoreProjector"="true" "RenderType"="TransparentCutout"}
		LOD 100
		Lighting Off

		Pass {

			CGPROGRAM
				#pragma vertex vertFunction
				#pragma fragment fragFunction
				#pragma target 2.0

				#include "UnityCG.cginc"

				struct appdata {
				
					float4 vertx : POSITION;
					float2 uv : TEXCOORD0;
					float4 color : COLOR;
				};

				struct v2f {
					
					float4 vertx : SV_POSITION;
					float2 uv : TEXCOORD0;
					float4 color : COLOR;
					
				};

				sampler2D _MainTex;
				float GlobalLightLevel;
				float MinGlobalLightLevel;
				float MaxGlobalLightLevel;

				v2f vertFunction (appdata v) {

					v2f o;

					o.vertx = UnityObjectToClipPos(v.vertx);
					o.uv = v.uv;
					o.color = v.color;

					return o;
				}

				fixed4 fragFunction (v2f i) : SV_Target {
					
					fixed4 col = tex2D (_MainTex, i.uv);

					float shadow = (MaxGlobalLightLevel - MinGlobalLightLevel) * GlobalLightLevel + MinGlobalLightLevel;
					shadow *= i.color.a;
					shadow = clamp(1 - shadow, MinGlobalLightLevel, MaxGlobalLightLevel);

					clip(col.a - 1);
					col = lerp(col, float4(0, 0, 0, 1), shadow);
					
					return col;											
				} 
			ENDCG
		}
	}
}