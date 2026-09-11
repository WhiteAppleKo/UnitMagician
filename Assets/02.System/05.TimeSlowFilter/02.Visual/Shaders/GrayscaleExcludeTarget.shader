Shader "Hidden/TimeSlowFilter/GrayscaleExcludeTarget"
{
    Properties
    {
        _DesaturateAmount ("Desaturate Amount", Range(0, 1)) = 0.0
        _Contrast ("Contrast", Range(0.5, 2.5)) = 1.35
        _FilmGrainAmount ("Film Grain Amount", Range(0, 0.1)) = 0.035
        _VignetteAmount ("Vignette Amount", Range(0, 1)) = 0.35
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

            float _DesaturateAmount;
            float _Contrast;
            float _FilmGrainAmount;
            float _VignetteAmount;

            inline half3 LinearToPerceptual(half3 c)
            {
                return sqrt(max(c, 0.0h));
            }

            inline half3 PerceptualToLinear(half3 c)
            {
                return c * c;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);

                // 1. 선형 HDR 색상을 인지 감마 공간으로 변환
                half3 srgbCol = LinearToPerceptual(saturate(col.rgb));

                // 2. 쿠로사와 흑백 영화 필름 감도 (오렌지/레드 광학 필터 효과: 파란 하늘을 어둡게 눌러 바닥과 완벽 분리)
                half luma = dot(srgbCol, half3(0.55h, 0.40h, 0.05h));

                // 3. 고대비 S자 명암 곡선 (깊은 먹색 그림자 + 강렬한 하이라이트)
                half sCurve = luma < 0.5h ? (2.0h * luma * luma) : (1.0h - 2.0h * (1.0h - luma) * (1.0h - luma));
                half contrasted = saturate((sCurve - 0.5h) * _Contrast + 0.5h);

                // 4. 셀룰로이드 필름 그레인 (1950년대 영화 필름 질감)
                float2 grainCoord = input.texcoord * _ScreenParams.xy;
                float grainNoise = frac(sin(dot(grainCoord + float2(_Time.y * 24.0, _Time.x * 37.0), float2(12.9898, 78.233))) * 43758.5453);
                half grain = (grainNoise - 0.5h) * _FilmGrainAmount;
                half filmLuma = saturate(contrasted + grain);

                // 5. 빈티지 렌즈 비네팅
                float2 uvCenter = (input.texcoord - 0.5h) * 2.0h;
                half vignette = saturate(1.0h - dot(uvCenter, uvCenter) * _VignetteAmount);
                filmLuma *= vignette;

                // 6. 선형 공간으로 복원 및 보간
                half3 kurosawaLinear = PerceptualToLinear(half3(filmLuma, filmLuma, filmLuma));
                half3 finalRgb = lerp(col.rgb, kurosawaLinear, _DesaturateAmount);

                return half4(finalRgb, col.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
