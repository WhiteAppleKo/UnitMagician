Shader "Hidden/TimeSlowFilter/GrayscaleExcludeTarget"
{
    Properties
    {
        _DesaturateAmount ("Desaturate Amount", Range(0, 1)) = 0.0
        _Contrast ("Contrast", Range(0.5, 2.0)) = 1.15
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        ZWrite Off
        ZTest Always
        Cull Off

        Stencil
        {
            Ref 1
            Comp NotEqual
            Pass Keep
        }

        Pass
        {
            Name "GrayscaleExcludeTargetPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _DesaturateAmount;
                float _Contrast;
            CBUFFER_END

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);

                // 휘도(Luma) 계산 (Rec. 709)
                half luma = dot(col.rgb, half3(0.2126h, 0.7152h, 0.0722h));

                // 대비(Contrast) 적용
                half contrastedLuma = saturate((luma - 0.5h) * _Contrast + 0.5h);
                half3 gray = half3(contrastedLuma, contrastedLuma, contrastedLuma);

                // 채도 감소 보간
                half3 finalRgb = lerp(col.rgb, gray, _DesaturateAmount);
                return half4(finalRgb, col.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
