Shader "StarFormer/Star Passage Darkness"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        _LightCenter ("Star Position", Vector) = (0,0,0,0)
        _LightRadius ("Light Radius", Float) = 1.1
        _LightStrength ("Light Strength", Range(0,1)) = 0.45
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex PassageVertex
            #pragma fragment PassageFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
            };
            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
                float2 worldPosition : TEXCOORD1;
            };
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _LightCenter;
                float _LightRadius;
                float _LightStrength;
            CBUFFER_END

            Varyings PassageVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                output.worldPosition = TransformObjectToWorld(input.positionOS).xy;
                return output;
            }

            half4 PassageFragment(Varyings input) : SV_Target
            {
                half4 color = CommonUnlitFragment(input, input.color);
                float radius = distance(input.worldPosition, _LightCenter.xy) / max(_LightRadius, 0.001);
                float reveal = (1.0 - smoothstep(0.15, 1.0, radius)) * saturate(_LightStrength);
                color.a *= 1.0 - reveal;
                return color;
            }
            ENDHLSL
        }
    }
}
