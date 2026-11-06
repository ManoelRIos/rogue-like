// Água low-poly: ondas em facetas (normal por triângulo), cor pela profundidade de verdade
// (_CameraDepthTexture), espuma na margem e em listras que descem com a correnteza,
// reflexo do céu nos ângulos rasos (Fresnel) e brilhos do sol/lua nas facetas.
Shader "Floresta/Water"
{
    Properties
    {
        _Shallow ("Raso", Color) = (0.56, 0.94, 0.86, 1)
        _Deep ("Fundo", Color) = (0.08, 0.48, 0.62, 1)
        _Foam ("Espuma", Color) = (1, 1, 1, 1)
        _DepthMax ("Profundidade de cor", Float) = 2.6
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "Floresta.cginc"
            fixed4 _Shallow, _Deep, _Foam;
            float _DepthMax;
            float _FlorestaTime;
            float4 _FlorestaSunColor, _FlorestaSkyColor, _FlorestaAmbient;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 wpos : TEXCOORD0;
                float4 screen : TEXCOORD1;
                float2 flow : TEXCOORD2;
            };

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), f.x), lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), f.x), f.y);
            }

            v2f vert (appdata v)
            {
                v2f o;
                float3 w = mul(unity_ObjectToWorld, v.vertex).xyz;
                float2 fl = v.color.rg * 2 - 1;
                float t = _FlorestaTime;
                w.y += sin(dot(w.xz, float2(0.35, 0.21)) + t * 1.6) * 0.055
                     + sin(dot(w.xz, float2(-0.19, 0.43)) + t * 1.15) * 0.045
                     + sin(dot(w.xz, fl * 1.1) - t * 3.2) * 0.04 * length(fl);
                o.wpos = w;
                o.pos = mul(UNITY_MATRIX_VP, float4(w, 1));
                o.screen = ComputeScreenPos(o.pos);
                COMPUTE_EYEDEPTH(o.screen.z);
                o.flow = fl;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _FlorestaTime;
                // normal da faceta (visual low-poly)
                float3 n = normalize(cross(ddy(i.wpos), ddx(i.wpos)));
                if (n.y < 0) n = -n;

                float scene = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screen)));
                float depth = max(0, scene - i.screen.z);
                float d01 = saturate(depth / _DepthMax);

                float3 L = normalize(_FlorestaSunDir.xyz);
                float3 V = normalize(_WorldSpaceCameraPos - i.wpos);
                float ndl = saturate(dot(n, L));
                float3 col = lerp(_Shallow.rgb, _Deep.rgb, d01);
                col *= _FlorestaAmbient.rgb + _FlorestaSunColor.rgb * (0.35 + ndl * 0.45);

                // céu refletido nos ângulos rasos
                float fres = pow(1 - saturate(dot(n, V)), 4);
                col = lerp(col, _FlorestaSkyColor.rgb, saturate(fres * 0.7));

                // brilho do sol (ou da lua) em algumas facetas
                float3 H = normalize(L + V);
                float spec = pow(saturate(dot(n, H)), 220) * 3.0;
                col += _FlorestaSunColor.rgb * spec;

                // espuma: borda irregular na margem + listras que descem o rio
                float edge = 1 - saturate(depth / 0.5);
                float nz = vnoise(i.wpos.xz * 1.6 - i.flow * t * 1.3) * 0.6 + vnoise(i.wpos.xz * 4.0 + t * 0.4) * 0.4;
                float shore = step(1 - edge, nz * 0.9 + edge * 0.35) * step(0.02, edge);
                float2 fd = normalize(i.flow + float2(1e-4, 0));
                float along = dot(i.wpos.xz, fd), across = dot(i.wpos.xz, float2(-fd.y, fd.x));
                float streak = step(0.8, vnoise(float2(along * 0.3 - t * 1.4, across * 2.4))) * saturate(length(i.flow) * 2) * (1 - d01 * 0.5);
                float foam = saturate(shore + streak * 0.55);
                float3 foamCol = _Foam.rgb * (_FlorestaAmbient.rgb + _FlorestaSunColor.rgb * 0.7);
                col = lerp(col, foamCol, foam);

                float alpha = lerp(0.5, 0.93, d01);
                alpha = max(alpha, foam * 0.95) * saturate(depth * 5);
                col = OmnoFog(col, i.wpos);
                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
}
