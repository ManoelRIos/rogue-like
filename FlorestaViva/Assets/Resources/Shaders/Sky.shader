// Céu em degradê (topo/horizonte) com estrelas que aparecem à noite.
// Desenhado numa esfera grande que segue a câmera.
Shader "Floresta/Sky"
{
    Properties
    {
        _Top ("Topo", Color) = (0.3, 0.55, 0.85, 1)
        _Horizon ("Horizonte", Color) = (0.85, 0.8, 0.65, 1)
        _Stars ("Estrelas", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" }
        Cull Front
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Top, _Horizon;
            float _Stars;
            float4 _FlorestaSunDir, _FlorestaSunColor, _FogSun;
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };
            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = normalize(v.vertex.xyz);
                return o;
            }
            float hash(float3 p) { return frac(sin(dot(p, float3(12.9898, 78.233, 37.719))) * 43758.5453); }
            fixed4 frag (v2f i) : SV_Target
            {
                float h = saturate(i.dir.y * 1.25 + 0.02);
                float3 c = lerp(_Horizon.rgb, _Top.rgb, smoothstep(0.0, 1.0, pow(h, 0.55)));
                // brilho grande e suave em volta do sol perto do horizonte (céu "Omno")
                float sdir = saturate(dot(normalize(i.dir), normalize(_FlorestaSunDir.xyz)));
                c = lerp(c, _FogSun.rgb, pow(sdir, 6) * 0.55 * (1 - h * 0.6));
                float3 cell = floor(i.dir * 180);
                float s = step(0.9965, hash(cell)) * saturate(i.dir.y * 3);
                c += s * _Stars * (0.6 + 0.4 * hash(cell + 7));
                // disco do sol (ou da lua) + halo
                float sd = dot(normalize(i.dir), normalize(_FlorestaSunDir.xyz));
                float disc = smoothstep(0.9991, 0.9995, sd);
                float halo = pow(saturate(sd), 48) * 0.35 + pow(saturate(sd), 8) * 0.08;
                c += _FlorestaSunColor.rgb * (disc * 1.6 + halo) * saturate(i.dir.y * 8 + 0.3);
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
}
