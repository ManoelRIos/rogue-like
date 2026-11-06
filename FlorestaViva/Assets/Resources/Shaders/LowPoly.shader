// Low-poly com cores por vértice, iluminação Lambert, neblina e vento.
// O canal alfa da cor do vértice diz quanto aquele vértice balança com o vento
// (0 = raiz/tronco parado, 1 = ponta das folhas).
Shader "Floresta/LowPoly"
{
    Properties
    {
        _Color ("Tinta", Color) = (1,1,1,1)
        _Emission ("Emissão", Color) = (0,0,0,0)
        _WindScale ("Força do vento", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        CGPROGRAM
        #pragma surface surf Lambert vertex:vert addshadow
        #pragma target 3.0

        fixed4 _Color;
        fixed4 _Emission;
        float _WindScale;
        float _FlorestaWind;      // intensidade global do vento (DayNight)
        float _FlorestaTime;

        struct Input { float4 color : COLOR; };

        void vert (inout appdata_full v)
        {
            float3 w = mul(unity_ObjectToWorld, v.vertex).xyz;
            float sway = v.color.a * _WindScale * _FlorestaWind;
            float t = _FlorestaTime;
            float wave = sin(t * 1.7 + w.x * 0.35 + w.z * 0.27) + 0.4 * sin(t * 3.1 + w.x * 0.9);
            float3 off = float3(wave, 0, wave * 0.6) * 0.12 * sway;
            v.vertex.xyz += mul((float3x3)unity_WorldToObject, off);
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            o.Albedo = IN.color.rgb * _Color.rgb;
            o.Emission = _Emission.rgb;
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
