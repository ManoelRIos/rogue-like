// Brilho aditivo sem iluminação: vagalumes, olhos das criaturas, faíscas da fogueira.
Shader "Floresta/Glow"
{
    Properties { _Color ("Cor", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; };
            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = fixed4(v.color.rgb * _Color.rgb * _Color.a, 1);
                return o;
            }
            fixed4 frag (v2f i) : SV_Target { return fixed4(i.color.rgb, 1); }
            ENDCG
        }
    }
}
