Shader "StarFormer/Smile Face Awakening"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _EyeOpen ("Eye Opening", Range(0,1)) = 0
        _EyeReveal ("Eyelid Transition", Range(0,1)) = 0
        _LeftLook ("Left Pupil Offset", Vector) = (0,0,0,0)
        _RightLook ("Right Pupil Offset", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            struct Attributes { COMMON_2D_INPUTS half4 color : COLOR; };
            struct Varyings { COMMON_2D_OUTPUTS half4 color : COLOR; };
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _EyeOpen, _EyeReveal;
                float4 _LeftLook, _RightLook;
            CBUFFER_END
            Varyings vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }
            float ellipse(float2 uv, float2 center, float2 radius)
            {
                return 1 - smoothstep(.96, 1.02, length((uv - center) / radius));
            }
            half3 eye(half3 baseColor, float2 uv, float2 center, float2 look)
            {
                // Feather a clean forehead sample across the whole painted eyelid,
                // including its yellow-tinted anti-aliased edge.
                float2 area = abs(uv - center);
                float patch = (1 - smoothstep(.086, .098, area.x)) *
                    (1 - smoothstep(.060, .076, area.y));
                float2 skinUV = float2(center.x, .75) + (uv - center) * .1;
                half3 skin = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, skinUV).rgb;
                baseColor = lerp(baseColor, skin, patch * _EyeReveal);
                center.y -= .015;
                // Account for the sprite's texture and scene scale so the small eyes
                // are circular on screen rather than circular only in texture UVs.
                float2 radius = float2(.0391, max(.0012, .0621 * _EyeOpen));
                float rim = ellipse(uv, center, radius) * _EyeReveal;
                float white = ellipse(uv, center, radius * .90) * _EyeReveal;
                baseColor = lerp(baseColor, half3(.065,.085,.08), rim);
                baseColor = lerp(baseColor, half3(.94,.97,.97), white);
                float2 pupilCenter = center + look * _EyeOpen;
                // Fixed round pupils are revealed by the opening eyelids, rather
                // than fading or stretching the whole eye into existence.
                float iris = ellipse(uv, pupilCenter, float2(.0253, .04025)) * white;
                float pupil = ellipse(uv, pupilCenter, float2(.023, .0368)) * white;
                float glint = ellipse(uv, pupilCenter + float2(-.007,.012 * _EyeOpen),
                    float2(.0069,.0109)) * white;
                float smallGlint = ellipse(uv, pupilCenter + float2(.009,-.011 * _EyeOpen),
                    float2(.0032,.0051)) * white;
                baseColor = lerp(baseColor, half3(.15,.20,.21), iris);
                baseColor = lerp(baseColor, half3(.018,.023,.025), pupil);
                return lerp(baseColor, half3(1,1,1), max(glint, smallGlint));
            }
            half4 frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                color.rgb = eye(color.rgb, input.uv, float2(.372,.625), _LeftLook.xy);
                color.rgb = eye(color.rgb, input.uv, float2(.614,.625), _RightLook.xy);
                return color * input.color;
            }
            ENDHLSL
        }
    }
}
