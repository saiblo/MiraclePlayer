Shader "Custom/Terrain"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Terrain Texture Array", 2DArray) = "white" {}
		_BumpMap ("Terrain Bump Array", 2DArray) = "bump"{}
		_GridTex ("Grid Texture", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.5

		// 条件编译
		#pragma multi_compile _ GRID_ON

        UNITY_DECLARE_TEX2DARRAY(_MainTex);
		UNITY_DECLARE_TEX2DARRAY(_BumpMap);

        struct Input
		{
			float4 color : COLOR; // 将颜色数据添加到其输入结构中
			float3 worldPos;
			float3 terrain;
        };

		void vert(inout appdata_full v, out Input data) {
			UNITY_INITIALIZE_OUTPUT(Input, data);
			data.terrain = v.texcoord2.xyz;
		}

		sampler2D _GridTex;
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

		float4 GetTerrainColor(Input IN, int index) {
			float3 uvw = float3(IN.worldPos.xz * 0.02, IN.terrain[index]); // uv由位置决定，w由terrain的第index分量决定
			float4 c = UNITY_SAMPLE_TEX2DARRAY(_MainTex, uvw); // 采样
			return c * IN.color[index];
		}

		float3 GetTerrainBump(Input IN, int index) {
			float3 uvw = float3(IN.worldPos.xz * 0.02, IN.terrain[index]);
			return UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_BumpMap, uvw)) * IN.color[index];
		}

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
			// Albedo comes from a texture tinted by color
			fixed4 c = 
				GetTerrainColor(IN, 0) + 
				GetTerrainColor(IN, 1) + 
				GetTerrainColor(IN, 2); // 混合三种颜色

			fixed3 b = 
				GetTerrainBump(IN, 0) + 
				GetTerrainBump(IN, 1) + 
				GetTerrainBump(IN, 2);

			fixed grid = 1;
			#if defined(GRID_ON)
				float2 gridUV = IN.worldPos.xz;
				gridUV.x *= 1 / (4 * 8.66025404);
				gridUV.y *= 1 / (2 * 15.0);
				grid = tex2D(_GridTex, gridUV); // 在纹理模式图上采样
			#endif

            o.Albedo = c.rgb * grid * _Color; // 将这种颜色乘上反照率？为什么要乘_Color
			o.Normal = b;
			// Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
