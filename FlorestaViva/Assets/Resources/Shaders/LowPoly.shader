// Low-poly com cores por vértice, neblina e sombras.
// O canal alfa da cor do vértice diz quanto aquele vértice balança com o vento
// (0 = raiz/tronco parado, 1 = ponta da folha). Ele também liga a luz "translúcida" das folhas.
// Vento: rajadas em faixas que andam a favor do vento (mesma fórmula do Wind.cs) + tremidinha rápida.
// A grama é empurrada por quem passa (_FlorestaPushers) e encolhe longe da câmera (_FadeDist).
Shader "Floresta/LowPoly"
{
    Properties
    {
        _Color ("Tinta", Color) = (1,1,1,1)
        _Emission ("Emissão", Color) = (0,0,0,0)
        _WindScale ("Força do vento", Float) = 1
        _FadeDist ("Some ao longe (0 = não)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        CGPROGRAM
        #pragma surface surf Omno vertex:vert addshadow fullforwardshadows finalcolor:OmnoFinal nofog
        #pragma target 3.0
        #pragma multi_compile_instancing
        #include "Floresta.cginc"

        fixed4 _Color;
        fixed4 _Emission;
        float _WindScale;
        float _FadeDist;
        float4 _FlorestaWindDir;     // xy direção, z força
        float _FlorestaTime;
        float4 _FlorestaPushers[4];  // xyz posição, w raio
        float4 _FlorestaRim;         // luz de contorno (cor do céu)

        struct Input { float4 color : COLOR; float3 worldPos; };

        float Gust(float2 p, float2 dir, float t)
        {
            float along = dot(p, dir), across = -p.x * dir.y + p.y * dir.x;
            float w = sin(along * 0.16 - t * 1.9 + 0.9 * sin(across * 0.07 + t * 0.13));
            float m = 0.6 + 0.4 * sin(across * 0.05 - t * 0.07 + along * 0.01);
            float n = saturate(((0.5 + 0.5 * w) * m - 0.35) / 0.6);
            return n * n * (3 - 2 * n);
        }

        void vert (inout appdata_full v)
        {
            float3 w = mul(unity_ObjectToWorld, v.vertex).xyz;
            float sway = v.color.a * _WindScale;
            float t = _FlorestaTime;
            float2 dir = _FlorestaWindDir.xy;
            float str = _FlorestaWindDir.z;
            float g = Gust(w.xz, dir, t);

            // inclina a favor do vento (mais quando a rajada passa) + oscilação + tremidinha das pontas
            float bend = sway * str * (0.22 + g * 0.85);
            float osc = sin(t * 1.7 + w.x * 0.35 + w.z * 0.27) + 0.4 * sin(t * 3.1 + w.x * 0.9);
            float2 perp = float2(-dir.y, dir.x);
            float flutter = sin(t * 7.0 + w.x * 2.1 + w.z * 1.7) * (0.25 + g);
            float3 off = float3(dir.x, 0, dir.y) * (bend * 0.32 + osc * 0.05 * sway)
                       + float3(perp.x, 0, perp.y) * flutter * 0.035 * sway;
            off.y -= bend * bend * 0.12;

            // quem passa empurra a grama para os lados
            for (int i = 0; i < 4; i++)
            {
                float4 p = _FlorestaPushers[i];
                float2 d = w.xz - p.xz;
                float dist = length(d) + 1e-4;
                float k = saturate(1 - dist / p.w) * step(abs(w.y - p.y), 1.6);
                off.xz += (d / dist) * k * 0.42 * v.color.a;
                off.y -= k * 0.22 * v.color.a;
            }

            v.vertex.xyz += mul((float3x3)unity_WorldToObject, off);

            // grama some aos poucos ao longe (encolhe para a base) em vez de "piscar"
            if (_FadeDist > 0)
            {
                float d = distance(w, _WorldSpaceCameraPos);
                v.vertex.xyz *= saturate((_FadeDist - d) / 14.0);
            }
        }

        // Luz estilo "pintado" (Omno): passagem suave da sombra para a luz numa faixa curta,
        // folhas finas deixam a luz atravessar (brilham contra o sol), contorno claro com a cor do céu.
        half4 LightingOmno (SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
        {
            half ndl = dot(s.Normal, lightDir);
            half w = ndl * 0.5 + 0.5;
            half ramp = smoothstep(0.3, 0.8, w);
            half leaf = saturate(abs(ndl) * 0.55 + 0.45);
            half diff = lerp(ramp, leaf, s.Gloss * 0.7);
            half trans = pow(saturate(dot(viewDir, -lightDir)), 5) * s.Gloss * 0.9;
            half3 c = s.Albedo * _LightColor0.rgb * (diff * atten + trans * (0.35 + 0.65 * atten));
        #ifdef UNITY_PASS_FORWARDBASE
            half rim = pow(1 - saturate(dot(s.Normal, viewDir)), 4);
            c += _FlorestaRim.rgb * rim * (0.22 + 0.4 * s.Gloss);
        #endif
            return half4(c, s.Alpha);
        }

        void OmnoFinal (Input IN, SurfaceOutput o, inout fixed4 color)
        {
            float3 fc;
            float f = OmnoFogAmount(IN.worldPos, fc);
        #ifdef UNITY_PASS_FORWARDADD
            color.rgb *= 1 - f;          // luzes extras (fogueira) somem na névoa
        #else
            color.rgb = lerp(color.rgb, fc, f);
        #endif
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            float foliage = saturate(IN.color.a * 2.2);
            o.Albedo = IN.color.rgb * _Color.rgb * (1 + IN.color.a * 0.12);
            o.Emission = _Emission.rgb;
            o.Gloss = foliage;
            o.Specular = 0;
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
