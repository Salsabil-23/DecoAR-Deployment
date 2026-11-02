// Surface AR detectee (F4.1, F4.10) : le logo DecoAR repete en badges a la place des points du template.
// Compatible avec le prefab de plan du template : bord adouci (UV2 + _ShortestUVMapping,
// ARFeatheredPlaneMeshVisualizer) et fondu d'apparition (_PlaneAlpha, ARPlaneMeshVisualizerFader).
Shader "DecoAR/M4 Logo Plane"
{
    Properties
    {
        _LogoTex ("Logo", 2D) = "white" {}
        _TileSize ("Espacement des logos (m)", Float) = 0.5
        _LogoScale ("Taille du logo (part de l'espacement)", Range(0.1, 1)) = 0.45
        _CornerRadius ("Arrondi des coins", Range(0, 0.5)) = 0.22
        _Opacity ("Opacite du logo", Range(0, 1)) = 0.85
        _FillColor ("Voile de la surface", Color) = (1, 1, 1, 0.08)
        _PlaneAlpha ("Fondu", Range(0, 1)) = 1
        _ShortestUVMapping ("Bord adouci", Float) = 1.5
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "LogoPlane"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_LogoTex);
            SAMPLER(sampler_LogoTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _LogoTex_ST;
                float _TileSize;
                float _LogoScale;
                float _CornerRadius;
                float _Opacity;
                float4 _FillColor;
                float _PlaneAlpha;
                float _ShortestUVMapping;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 feather : TEXCOORD1;   // x : distance relative au bord (ARFeatheredPlaneMeshVisualizer)
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 planePos : TEXCOORD0;  // position dans le plan (m), repere du plan AR
                float feather : TEXCOORD1;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.planePos = v.positionOS.xz;
                o.feather = v.feather.x;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // Grille de logos, une rangee sur deux decalee d'une demi-case
                float2 grid = i.planePos / _TileSize;
                grid.x += 0.5 * step(0.5, frac(grid.y * 0.5));
                float2 cell = frac(grid) - 0.5;
                float2 uv = cell / _LogoScale + 0.5;

                // Badge aux coins arrondis (le logo est une image carree)
                float2 q = abs(uv - 0.5) - (0.5 - _CornerRadius);
                float d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - _CornerRadius;
                float aa = max(fwidth(d), 1e-4);
                float mask = saturate(0.5 - d / aa);

                // Derivees continues : pas de couture de mipmap au bord des cases
                float2 duv = 1.0 / (_TileSize * _LogoScale);
                half4 logo = SAMPLE_TEXTURE2D_GRAD(_LogoTex, sampler_LogoTex, saturate(uv),
                    ddx(i.planePos) * duv, ddy(i.planePos) * duv);

                float logoAlpha = mask * logo.a * _Opacity;
                half3 rgb = lerp(_FillColor.rgb, logo.rgb, logoAlpha);
                float alpha = logoAlpha + _FillColor.a * (1.0 - logoAlpha);

                // Bord adouci et fondu d'apparition
                // feather vaut 1 au debut de la bande adoucie et _ShortestUVMapping au bord du plan
                float edge = 1.0 - smoothstep(1.0, _ShortestUVMapping, i.feather);
                alpha *= edge * _PlaneAlpha;
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
