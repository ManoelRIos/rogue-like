// Fitas de luz: rastros do vento e feixes de sol na mata. Aditivo, cor e transparência pelo vértice,
// some na neblina ao longe.
Shader "Floresta/Ribbon"
{
    Properties { _Color ("Intensidade", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Transparent+2" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Floresta.cginc"
            fixed4 _Color;
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float3 wpos : TEXCOORD0; };
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.color = v.color * _Color;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float3 fc; float f = OmnoFogAmount(i.wpos, fc);
                return fixed4(i.color.rgb, i.color.a * (1 - f * 0.85));
            }
            ENDCG
        }
    }
}
