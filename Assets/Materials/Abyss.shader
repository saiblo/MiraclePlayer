Shader "Custom/Abyss"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,0)
		_Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "Transparent" }
        LOD 200


		Pass {
			Tags { "LightMode" = "ForwardBase" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Lighting.cginc"

			fixed4 _Color;
			fixed _Cutoff;
        
			float4 vert(float4 v : POSITION) : SV_POSITION {
				return UnityObjectToClipPos(v);
			}

			fixed4 frag() : SV_Target {
				clip(_Color.a - _Cutoff);
				return _Color;
			}

			ENDCG
		}
    }
    FallBack "Diffuse"
}
