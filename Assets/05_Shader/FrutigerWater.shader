Shader "Custom/FrutigerWater"
{
    Properties
    {
        [Header(Colores)]
        _ShallowColor ("Color superficie (claro)", Color) = (0.55, 0.95, 1.0, 0.75)
        _DeepColor ("Color fondo (profundo)", Color) = (0.0, 0.45, 0.85, 0.95)
        _DepthPower ("Curva del degradado", Range(0.2, 4)) = 1.2

        [Header(Olas)]
        _WaveSpeed ("Velocidad olas", Float) = 0.6
        _WaveScale ("Escala olas", Float) = 4.0
        _WaveStrength ("Fuerza olas", Range(0, 1)) = 0.35

        [Header(Caustics)]
        _CausticColor ("Color caustics", Color) = (0.9, 1.0, 1.0, 1)
        _CausticScale ("Escala caustics", Float) = 1.0
        _CausticSpeed ("Velocidad caustics", Float) = 0.5
        _CausticIntensity ("Intensidad caustics", Range(0, 3)) = 1.0

        [Header(Brillo tipo vidrio)]
        _SpecColor2 ("Color brillo", Color) = (1, 1, 1, 1)
        _Shininess ("Dureza del brillo", Range(8, 256)) = 90
        _SpecIntensity ("Intensidad brillo", Range(0, 3)) = 1.2
        _LightDir ("Direccion luz falsa", Vector) = (-0.4, 0.8, -0.3, 0)
        _FresnelPower ("Fresnel (borde brillante)", Range(0.5, 8)) = 3.0
        _FresnelColor ("Color fresnel", Color) = (0.85, 1.0, 1.0, 1)

        [Header(Burbujas)]
        _BubbleDensity ("Densidad burbujas", Float) = 8.0
        _BubbleSpeed ("Velocidad burbujas", Float) = 0.25
        _BubbleIntensity ("Intensidad burbujas", Range(0, 1)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            fixed4 _ShallowColor, _DeepColor, _CausticColor, _SpecColor2, _FresnelColor;
            float _DepthPower;
            float _WaveSpeed, _WaveScale, _WaveStrength;
            float _CausticScale, _CausticSpeed, _CausticIntensity;
            float _Shininess, _SpecIntensity, _FresnelPower;
            float4 _LightDir;
            float _BubbleDensity, _BubbleSpeed, _BubbleIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            // Altura de la ola (suma de senos cruzados)
            float waveHeight(float2 uv, float t)
            {
                float2 p = uv * _WaveScale;
                float h = sin(p.x * 1.0 + t) * 0.5;
                h += sin(p.y * 1.3 - t * 1.2) * 0.4;
                h += sin((p.x + p.y) * 1.7 + t * 0.8) * 0.3;
                h += sin((p.x - p.y) * 2.3 - t * 1.5) * 0.2;
                return h;
            }

            // Caustics clasicas (patron de luz bajo el agua)
            float caustics(float2 uv, float t)
            {
                float2 p = fmod(uv * 6.28318, 6.28318) - 250.0;
                float2 i = p;
                float c = 1.0;
                float inten = 0.005;

                for (int n = 0; n < 4; n++)
                {
                    float tt = t * (1.0 - (3.5 / (n + 1.0)));
                    i = p + float2(cos(tt - i.x) + sin(tt + i.y),
                                   sin(tt - i.y) + cos(tt + i.x));
                    c += 1.0 / length(float2(p.x / (sin(i.x + tt) / inten),
                                             p.y / (cos(i.y + tt) / inten)));
                }
                c /= 4.0;
                c = 1.17 - pow(c, 1.4);
                return pow(abs(c), 8.0);
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Burbujas que suben en una cuadricula
            float bubbles(float2 uv, float t)
            {
                float total = 0;
                for (int layer = 0; layer < 2; layer++)
                {
                    float dens = _BubbleDensity * (1.0 + layer * 0.7);
                    float2 g = uv * dens;
                    g.y -= t * _BubbleSpeed * dens * (1.0 + layer * 0.4);

                    float2 id = floor(g);
                    float2 f = frac(g) - 0.5;
                    float r = hash21(id + layer * 17.0);

                    if (r > 0.35)
                    {
                        float2 off = float2(hash21(id + 3.1) - 0.5, hash21(id + 7.7) - 0.5) * 0.4;
                        float size = 0.06 + hash21(id + 9.3) * 0.12;
                        float d = length(f - off);

                        float body = smoothstep(size, size * 0.85, d) * 0.15;   // relleno suave
                        float ring = smoothstep(size, size * 0.9, d) - smoothstep(size * 0.9, size * 0.7, d); // borde
                        float glint = smoothstep(size * 0.3, 0.0, length(f - off - float2(-size * 0.4, size * 0.4))); // reflejo

                        total += body + ring * 0.7 + glint;
                    }
                }
                return saturate(total);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y;

                // ---- Normal de las olas (derivadas numericas) ----
                float wt = t * _WaveSpeed;
                float e = 0.01;
                float h = waveHeight(i.uv, wt);
                float hx = waveHeight(i.uv + float2(e, 0), wt);
                float hy = waveHeight(i.uv + float2(0, e), wt);
                float3 n = normalize(float3(-(hx - h) / e * _WaveStrength * 0.1, 1.0,
                                            -(hy - h) / e * _WaveStrength * 0.1));

                // ---- Degradado de profundidad (claro arriba, azul abajo) ----
                float depth = pow(saturate(1.0 - i.uv.y), _DepthPower);
                depth += h * 0.04;                          // las olas mueven el degradado
                fixed4 col = lerp(_ShallowColor, _DeepColor, saturate(depth));

                // ---- Caustics ----
                float2 cuv = i.uv * _CausticScale + n.xz * 0.05;
                float c = caustics(cuv, t * _CausticSpeed);
                col.rgb += _CausticColor.rgb * c * _CausticIntensity * (1.0 - depth * 0.6);

                // ---- Brillo especular tipo vidrio ----
                float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 L = normalize(_LightDir.xyz);
                float3 H = normalize(L + V);
                float spec = pow(saturate(dot(n, H)), _Shininess) * _SpecIntensity;
                col.rgb += _SpecColor2.rgb * spec;

                // ---- Fresnel: borde mas claro y opaco ----
                float fres = pow(1.0 - saturate(dot(n, V)), _FresnelPower);
                col.rgb = lerp(col.rgb, _FresnelColor.rgb, fres * 0.6);
                col.a = saturate(col.a + fres * 0.25);

                // ---- Burbujas ----
                float b = bubbles(i.uv, t);
                col.rgb += b * _BubbleIntensity;
                col.a = saturate(col.a + b * 0.3);

                // ---- Banda de luz suave arriba (efecto "gloss" Aero) ----
                float gloss = smoothstep(0.55, 1.0, i.uv.y) * 0.18;
                col.rgb += gloss;

                return col;
            }
            ENDCG
        }
    }
}