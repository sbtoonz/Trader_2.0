Shader "Custom/LitGui" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Tint", Vector) = (1,1,1,1)
		_Saturation ("Saturation", Range(-1, 1)) = 0
		_Brightness ("Brightness", Range(0, 2)) = 0
		_Lighting ("Lighting", Range(0, 1)) = 1
		_LightingSaturation ("Lighting saturation", Range(-1, 1)) = 0
		_ShadowOffsetX ("ShadowOffsetX", Float) = 0.1
		_ShadowOffsetY ("ShadowOffsetY", Float) = 0.1
		_ShadowIntensity ("Shadow intensity", Range(0, 1)) = 1
		_PixelSize ("PixelSize", Range(0, 1)) = 1
		_PixelIntensity ("Pixel intensity", Range(0, 1)) = 0
		_PixelAlphaIntensity ("Pixel alpha intensity", Range(0, 1)) = 0
		[HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
		[HideInInspector] _Stencil ("Stencil ID", Float) = 0
		[HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
		[HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
		[HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
		[HideInInspector] _ColorMask ("Color Mask", Float) = 15
	}
	SubShader
    {
        LOD 100

        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType"="Plane"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        CGPROGRAM
            #pragma surface surf PPL alpha noshadow novertexlights nolightmap vertex:vert nofog

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                fixed4 color : COLOR;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Input
            {
                float2 uv_MainTex;
                float2 uv2_DetailTex;
                fixed4 color : COLOR;
                float4 worldPosition;
            };

            sampler2D _MainTex;
            sampler2D _MainBump;
            sampler2D _DetailTex;
            sampler2D _DetailBump;

            float4 _DetailTex_TexelSize;
            fixed4 _Color;
            fixed4 _Specular;
            half _Strength;
            half _Shininess;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            void vert (inout appdata_t v, out Input o)
            {
                UNITY_INITIALIZE_OUTPUT(Input, o);
                o.worldPosition = v.vertex;
                v.vertex = o.worldPosition;

                v.texcoord1.xy *= _DetailTex_TexelSize.xy;
                v.color = v.color * _Color;
            }

            void surf (Input IN, inout SurfaceOutput o)
            {
                fixed4 col = tex2D(_MainTex, IN.uv_MainTex) + _TextureSampleAdd;
                fixed4 detail = tex2D(_DetailTex, IN.uv2_DetailTex);

                // Mix normals by just averaging the data and then doing unpack.
                // Much cheaper than unpacking both and then averaging -
                // not 100% the same result but fairly close. And neither of
                // these is "proper" normal blending anyway.
                fixed4 nmMain = tex2D(_MainBump, IN.uv_MainTex);
                fixed4 nmDetail = tex2D(_DetailBump, IN.uv2_DetailTex);
                fixed3 normal = UnpackNormal((nmMain + nmDetail) * 0.5);

                col.rgb = lerp(col.rgb, col.rgb * detail.rgb, detail.a * _Strength);
                col *= IN.color;

                o.Albedo = col.rgb;
                o.Normal = normal;
                o.Specular = _Specular.a;
                o.Gloss = _Shininess;
                o.Alpha = col.a;

                #ifdef UNITY_UI_CLIP_RECT
                o.Alpha *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (o.Alpha - 0.001);
                #endif
            }

            half4 LightingPPL (SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
            {
                half shininess = s.Gloss * 250.0 + 4.0;

            #ifndef USING_DIRECTIONAL_LIGHT
                lightDir = normalize(lightDir);
            #endif

                // Phong shading model
                half reflectiveFactor = max(0.0, dot(-viewDir, reflect(lightDir, s.Normal)));

                // Blinn-Phong shading model
                //half reflectiveFactor = max(0.0, dot(s.Normal, normalize(lightDir + viewDir)));

                half diffuseFactor = max(0.0, dot(s.Normal, lightDir));
                half specularFactor = pow(reflectiveFactor, shininess) * s.Specular;

                half4 c;
                c.rgb = (s.Albedo * diffuseFactor + _Specular.rgb * specularFactor) * _LightColor0.rgb;
                c.rgb *= atten;
                c.a = s.Alpha;
                return c;
            }
        ENDCG
    }
    Fallback "UI/Lit/Transparent"
}