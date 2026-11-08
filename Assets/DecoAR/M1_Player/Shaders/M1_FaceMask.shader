// Visage 3D scanne pose sur la tete du personnage (URP).
// Le visage du personnage est souvent un peu plus en avant que le masque (nez, pommettes) :
// la profondeur est rapprochee de la camera de _DepthBias metres pour que le masque passe devant,
// tandis que les cheveux et l'arriere de la tete (plus loin que cette marge) le cachent toujours.
Shader "DecoAR/FaceMask"
{
    Properties
    {
        _BaseMap ("Visage", 2D) = "white" {}
        _DepthBias ("Marge de profondeur (m)", Range(0, 0.05)) = 0.02
        _Lighting ("Part d'eclairage de la scene", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _DepthBias;
                float _Lighting;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 positionVS = TransformWorldToView(positionWS);
                o.positionCS = TransformWViewToHClip(positionVS);

                // Profondeur d'un point _DepthBias plus pres de la camera, position a l'ecran inchangee
                float3 closerVS = positionVS - normalize(positionVS) * _DepthBias;
                float4 closerCS = TransformWViewToHClip(closerVS);
                o.positionCS.z = closerCS.z / closerCS.w * o.positionCS.w;

                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 face = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);

                // La photo contient deja la lumiere de la piece : on ne garde qu'une part de l'eclairage de la scene
                Light light = GetMainLight();
                float3 n = normalize(i.normalWS);
                float3 lit = light.color * saturate(dot(n, light.direction)) + SampleSH(n);
                face.rgb *= lerp(1.0, lit, _Lighting);
                return face;
            }
            ENDHLSL
        }
    }
}
