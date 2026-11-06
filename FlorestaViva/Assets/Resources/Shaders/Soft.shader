// Partículas e névoa: cor do vértice × cor por instância (fumaça, poeira, névoa, nuvens, faíscas).
// _Lit = 1: a cor acompanha a luz do dia (fumaça cinza não brilha à noite). Blend configurável (normal ou aditivo).
Shader "Floresta/Soft"
{
    Properties
    {
        _Color ("Cor", Color) = (1,1,1,1)
        _Lit ("Acompanha a luz", Float) = 1
        _UseFog ("Neblina", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _Src ("Src", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _Dst ("Dst", Float) = 10
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend [_Src] [_Dst]
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "Floresta.cginc"
            float _Lit, _UseFog;
            float4 _FlorestaAmbient;
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
            UNITY_INSTANCING_BUFFER_END(Props)
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; fixed4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float shade : TEXCOORD1; float3 wpos : TEXCOORD0; };
            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.pos = UnityObjectToClipPos(v.vertex);
                fixed4 c = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                o.color = fixed4(v.color.rgb * c.rgb, c.a);
                // facetas viradas para cima um pouco mais claras (volume nas bolhas low-poly)
                float3 n = UnityObjectToWorldNormal(v.normal);
                o.shade = 0.82 + 0.18 * saturate(n.y * 0.5 + 0.5);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = i.color;
                c.rgb *= lerp(1, _FlorestaAmbient.rgb * i.shade, _Lit);
                if (_UseFog > 0.5)
                {
                    float3 fc; float f = OmnoFogAmount(i.wpos, fc);
                    // aditivo (faíscas, brilhos) some na névoa; normal mistura com a cor da névoa
                    c.rgb = _Lit > 0.5 ? lerp(c.rgb, fc, f) : c.rgb * (1 - f);
                }
                return c;
            }
            ENDCG
        }
    }
}
