Shader "Custom/DynamicShadow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ShadowColor ("Shadow Color", Color) = (0,0,0,0.5)
        _FadeDistance ("Fade Distance", Range(0,10)) = 5
        _ShadowIntensity ("Shadow Intensity", Range(0,1)) = 0.7
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }
        
        Pass
        {
            Name "ShadowPass"
            Tags { "LightMode"="UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float fogCoord : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _ShadowColor;
            float _FadeDistance;
            half _ShadowIntensity;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = vertexInput.positionCS;
                output.worldPos = vertexInput.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Sample texture
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // Calculate distance fade
                float distanceFromCenter = length(input.uv - 0.5) * 2.0;
                float fadeFactor = saturate(1.0 - (distanceFromCenter / _FadeDistance));
                
                // Shadow color with fade
                half4 shadowColor = _ShadowColor;
                shadowColor.a *= _ShadowIntensity * fadeFactor * texColor.a;
                
                // Apply fog
                shadowColor.rgb = MixFog(shadowColor.rgb, input.fogCoord);
                
                return shadowColor;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}


// Alternative: Simple Shadow Material Settings
/*
برای ساده‌تر کردن، می‌توانید از Unity's Built-in materials استفاده کنید:

1. Standard Material با این تنظیمات:
   - Rendering Mode: Transparent
   - Albedo: Dark gray (RGB: 50,50,50)
   - Alpha: 0.6
   - Metallic: 0
   - Smoothness: 0

2. یا URP/Lit material:
   - Surface Type: Transparent
   - Base Map: Dark gray
   - Alpha: 0.6
*/