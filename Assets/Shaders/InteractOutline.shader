Shader "Custom/InteractOutline"
{
    Properties
    {
        _OutlineColor ("Color", Color) = (1, 0.9, 0.4, 1)
        _OutlineWidth ("Width (pixeles)", Float) = 3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+10" }
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="UniversalForward" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float3 smoothNormalOS : TEXCOORD3; //normal suavizada que guarda el postprocessor (0 si la malla no la tiene)
            };

            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 n = dot(IN.smoothNormalOS, IN.smoothNormalOS) > 0.0001 ? IN.smoothNormalOS : IN.normalOS;

                float4 posCS = TransformObjectToHClip(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(n);
                float2 dirCS = mul((float3x3)UNITY_MATRIX_VP, normalWS).xy;
                dirCS = dirCS / max(length(dirCS), 1e-5);

                //desplazamiento en pantalla: pixeles -> clip space
                posCS.xy += dirCS * _OutlineWidth * 2.0 / _ScreenParams.xy * posCS.w;

                OUT.positionCS = posCS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target { return _OutlineColor; }
            ENDHLSL
        }
    }
}