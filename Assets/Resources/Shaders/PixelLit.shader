// Shader principal do jogo (Built-in Render Pipeline).
// - Recorte de alfa (pixel art nítida) com escrita de profundidade: sprites em pé
//   e geometria 3D se ordenam corretamente pela profundidade.
// - Iluminação estilizada: ambiente + até 16 luzes pontuais globais, quantizada
//   em degraus para combinar com a pixel art.
// - _Flash deixa o sprite branco (dano), usado via MaterialPropertyBlock.
Shader "Ecos/PixelLit"
{
    Properties
    {
        _MainTex ("Textura", 2D) = "white" {}
        _Color ("Cor", Color) = (1,1,1,1)
        _Flash ("Flash", Range(0,1)) = 0
        _Lit ("Recebe luz", Range(0,1)) = 1
        _Emission ("Emissão", Range(0,1)) = 0
        _Cutoff ("Recorte", Range(0,1)) = 0.5
        _Fade ("Esmaecer (0 = escuro)", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True" }
        Cull Off
        ZWrite On
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Flash;
            float _Lit;
            float _Emission;
            float _Cutoff;
            float _Fade;

            // definidos por Ecos.Rendering.LightingSystem
            float4 _EcosAmbient;
            float4 _EcosLightPos[16];   // xyz = posição, w = raio
            float4 _EcosLightCol[16];   // rgb = cor, a = intensidade
            float _EcosLightCount;
            float _EcosSteps;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; float3 wpos : TEXCOORD1; float4 spos : TEXCOORD2; };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.spos = ComputeScreenPos(o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                clip(c.a - _Cutoff);

                float3 light = _EcosAmbient.rgb;
                int count = (int)_EcosLightCount;
                for (int k = 0; k < 16; k++)
                {
                    if (k >= count) break;
                    float3 d = _EcosLightPos[k].xyz - i.wpos;
                    d.y *= 0.6;
                    float dist = length(d);
                    float a = saturate(1.0 - dist / _EcosLightPos[k].w);
                    light += _EcosLightCol[k].rgb * _EcosLightCol[k].a * a * a;
                }
                // degraus de luz com pontilhado ordenado 2x2 (transição pixelada entre os tons)
                float steps = max(_EcosSteps, 1.0);
                float2 pix = floor(i.spos.xy / max(i.spos.w, 1e-5) * _ScreenParams.xy);
                float bayer = fmod(pix.x, 2.0) * 0.5 + fmod(pix.y, 2.0) * 0.25 + fmod(pix.x + pix.y, 2.0) * 0.125;
                light = floor(light * steps + bayer) / steps;
                light = lerp(float3(1,1,1), light, _Lit);
                light = max(light, _Emission.xxx);

                float3 col = c.rgb * light;
                col = lerp(col, float3(1,1,1), _Flash);
                col *= _Fade;   // transição suave entre salas (some no escuro)
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
