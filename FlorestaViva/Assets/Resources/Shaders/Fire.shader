// Chamas: cones low-poly aditivos que tremem e se esticam. O alfa da cor do vértice é a altura (0 base, 1 ponta):
// a cor vai de branco-amarelo na base a laranja e vermelho na ponta, e a ponta some piscando.
Shader "Floresta/Fire"
{
    Properties
    {
        _Color ("Intensidade", Color) = (1,1,1,1)
        _Seed ("Semente", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent+1" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            float _Seed;
            float _FlorestaTime;
            float4 _FlorestaWindDir;
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float h : TEXCOORD0; };
            v2f vert (appdata v)
            {
                v2f o;
                float h = v.color.a;
                float t = _FlorestaTime * 1.0 + _Seed * 7.13;
                float3 p = v.vertex.xyz;
                // tremor que cresce com a altura (a ponta da chama dança, a base fica firme)
                p.x += (sin(t * 9.1 + p.y * 7.0) * 0.05 + sin(t * 14.3 + p.z * 9.0) * 0.025) * h;
                p.z += (cos(t * 8.3 + p.y * 6.0) * 0.05 + sin(t * 12.1 + p.x * 8.0) * 0.025) * h;
                p.y *= 0.85 + 0.3 * (0.5 + 0.5 * sin(t * 5.7)) * (0.6 + 0.4 * sin(t * 2.3 + 1.0));
                // o vento deita a chama
                float3 wl = mul((float3x3)unity_WorldToObject, float3(_FlorestaWindDir.x, 0, _FlorestaWindDir.y));
                p += wl * h * h * 0.18 * _FlorestaWindDir.z;
                o.pos = UnityObjectToClipPos(float4(p, 1));
                o.color = v.color;
                o.h = h;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float h = i.h;
                float3 hot = float3(1.0, 0.95, 0.7), mid = float3(1.0, 0.55, 0.12), top = float3(0.85, 0.16, 0.04);
                float3 c = h < 0.45 ? lerp(hot, mid, h / 0.45) : lerp(mid, top, (h - 0.45) / 0.55);
                c *= i.color.rgb;
                float a = saturate(1.15 - h * 1.05) * _Color.a;
                return fixed4(c * _Color.rgb, a);
            }
            ENDCG
        }
    }
}
