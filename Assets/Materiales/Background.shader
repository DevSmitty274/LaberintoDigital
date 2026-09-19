Shader "Custom/Background"
{
    Properties
    {
        _ColorA ("Color A", Color) = (0.9, 0.2, 0.5, 1)
        _ColorB ("Color B", Color) = (0.2, 0.4, 0.9, 1)
        _ColorC ("Color C", Color) = (0.9, 0.7, 0.1, 1)
        _Speed ("Speed", Float) = 0.15
        _Scale ("Scale", Float) = 1.5
        _Distortion ("Distortion", Float) = 1.0

        _MaskTex ("Mask Texture", 2D) = "gray" {}
        _MaskTiling ("Mask Tiling", Float) = 4.0
        _MaskScrollSpeed ("Mask Scroll Speed", Vector) = (0.01, 0.005, 0, 0)
        _MaskStrength ("Mask Strength", Range(0,1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            fixed4 _ColorA, _ColorB, _ColorC;
            float _Speed, _Scale, _Distortion;

            sampler2D _MaskTex;
            float _MaskTiling, _MaskStrength;
            float2 _MaskScrollSpeed;

            float2x2 rot(float a)
            {
                float s = sin(a), c = cos(a);
                return float2x2(c, -s, s, c);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = (i.uv - 0.5) * _Scale;
                float t = _Time.y * _Speed;

                float2 p1 = mul(rot(t * 0.3), uv);
                float2 p2 = mul(rot(-t * 0.2), uv * 1.3);

                float wave1 = sin(p1.x * 3.0 + t) * cos(p1.y * 3.0 - t * 0.7);
                float wave2 = sin(p2.x * 2.0 - t * 1.2) * cos(p2.y * 2.5 + t * 0.5);

                float mixVal = (wave1 + wave2) * 0.5 * _Distortion;
                mixVal = smoothstep(0.0, 1.0, mixVal * 0.5 + 0.5);

                fixed4 col = lerp(_ColorA, _ColorB, mixVal);
                float mixVal2 = sin(mixVal * 3.14 + t * 0.4) * 0.5 + 0.5;
                col = lerp(col, _ColorC, mixVal2 * 0.4);

                // --- Máscara: la textura modula brillo del degradado ---
                float2 maskUV = i.uv * _MaskTiling + _Time.y * _MaskScrollSpeed;
                float mask = tex2D(_MaskTex, maskUV).r; // usa canal rojo como luminancia
                float maskInfluence = lerp(1.0, mask, _MaskStrength);
                col.rgb *= lerp(0.75, 1.25, maskInfluence); // oscurece/aclara según el patrón

                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}