// Pós-processamento "Omno": bloom suave (brilho em volta do sol, do fogo, dos vagalumes),
// correção de cor (saturação, contraste, tom quente/frio, sombras levemente azuladas) e vinheta.
Shader "Hidden/Floresta/Post"
{
    Properties { _MainTex ("", 2D) = "white" {} }
    CGINCLUDE
    #include "UnityCG.cginc"
    sampler2D _MainTex, _Bloom;
    float4 _MainTex_TexelSize;
    float4 _Threshold;      // x limiar, y joelho, z intensidade
    float4 _Grade;          // x saturação, y contraste, z exposição, w vinheta
    float4 _Tint, _Lift;
    float _Delta;

    struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
    v2f vert (appdata_img v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord; return o; }

    half3 Box (float2 uv, float d)
    {
        float4 o = _MainTex_TexelSize.xyxy * float2(-d, d).xxyy;
        half3 s = tex2D(_MainTex, uv + o.xy).rgb + tex2D(_MainTex, uv + o.zy).rgb + tex2D(_MainTex, uv + o.xw).rgb + tex2D(_MainTex, uv + o.zw).rgb;
        return s * 0.25;
    }
    ENDCG

    SubShader
    {
        Cull Off ZTest Always ZWrite Off
        // 0: só o que é claro passa (joelho suave, sem corte duro)
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag (v2f i) : SV_Target
            {
                half3 c = Box(i.uv, 1);
                half b = max(c.r, max(c.g, c.b));
                half soft = clamp(b - _Threshold.x + _Threshold.y, 0, 2 * _Threshold.y);
                soft = soft * soft / (4 * _Threshold.y + 1e-4);
                half contrib = max(soft, b - _Threshold.x) / max(b, 1e-4);
                return half4(c * contrib, 1);
            }
            ENDCG
        }
        // 1: reduz
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag (v2f i) : SV_Target { return half4(Box(i.uv, 1), 1); }
            ENDCG
        }
        // 2: amplia somando
        Pass
        {
            Blend One One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag (v2f i) : SV_Target { return half4(Box(i.uv, 0.5), 1); }
            ENDCG
        }
        // 3: final — imagem + bloom + cor + vinheta
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag (v2f i) : SV_Target
            {
                half3 c = tex2D(_MainTex, i.uv).rgb;
                c += tex2D(_Bloom, i.uv).rgb * _Threshold.z;
                c *= _Grade.z;
                c = c * _Tint.rgb;
                c += _Lift.rgb * (1 - saturate(dot(c, half3(0.3, 0.59, 0.11)) * 2));   // sombras puxadas para o azul
                half l = dot(c, half3(0.3, 0.59, 0.11));
                c = lerp(l.xxx, c, _Grade.x);
                c = (c - 0.5) * _Grade.y + 0.5;
                float2 d = i.uv - 0.5;
                c *= 1 - dot(d, d) * _Grade.w * 2.2;
                // pontilhado leve: some com as faixas no degradê do céu
                float n = frac(sin(dot(i.uv * _MainTex_TexelSize.zw, float2(12.9898, 78.233))) * 43758.5453);
                c += (n - 0.5) / 255.0;
                return half4(saturate(c), 1);
            }
            ENDCG
        }
    }
}
