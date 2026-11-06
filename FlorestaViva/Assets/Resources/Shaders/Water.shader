// Água estilizada: cor que muda com o dia/noite, ondulação no vértice e brilho na borda.
Shader "Floresta/Water"
{
    Properties
    {
        _Shallow ("Raso", Color) = (0.25, 0.6, 0.6, 0.75)
        _Deep ("Fundo", Color) = (0.05, 0.2, 0.3, 0.9)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        CGPROGRAM
        #pragma surface surf Lambert vertex:vert alpha:fade
        #pragma target 3.0
        fixed4 _Shallow, _Deep;
        float _FlorestaTime;
        struct Input { float3 worldPos; };

        void vert (inout appdata_full v)
        {
            float3 w = mul(unity_ObjectToWorld, v.vertex).xyz;
            v.vertex.y += sin(_FlorestaTime * 1.3 + w.x * 0.4) * 0.05 + cos(_FlorestaTime * 1.1 + w.z * 0.5) * 0.05;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            float n = sin(IN.worldPos.x * 0.8 + _FlorestaTime) * cos(IN.worldPos.z * 0.7 - _FlorestaTime * 0.8);
            fixed4 c = lerp(_Deep, _Shallow, saturate(n * 0.5 + 0.5));
            o.Albedo = c.rgb;
            o.Emission = c.rgb * 0.15 + step(0.92, n) * 0.25;
            o.Alpha = c.a;
        }
        ENDCG
    }
}
