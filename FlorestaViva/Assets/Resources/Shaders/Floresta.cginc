// Funções compartilhadas pelos shaders da Floresta Viva.
// Neblina "Omno": densidade cai com a altura (morros e copas aparecem acima da névoa),
// a cor vai da névoa próxima para o tom do horizonte ao longe, e esquenta na direção do sol.
#ifndef FLORESTA_INCLUDED
#define FLORESTA_INCLUDED

float4 _FogNear, _FogFar, _FogSun;
float4 _FogParams;          // x densidade, y queda com a altura, z altura base, w força do brilho do sol
float4 _FlorestaSunDir;     // direção PARA o sol (ou a lua)

float OmnoFogAmount(float3 wpos, out float3 fogCol)
{
    float3 v = wpos - _WorldSpaceCameraPos;
    float d = length(v);
    float h = max(0, wpos.y - _FogParams.z);
    float dens = _FogParams.x * exp(-h * _FogParams.y);
    float f = saturate(1 - exp(-d * dens));
    float sun = pow(saturate(dot(v / max(d, 1e-3), normalize(_FlorestaSunDir.xyz))), 5) * _FogParams.w;
    fogCol = lerp(_FogNear.rgb, _FogFar.rgb, saturate(d / 220));
    fogCol = lerp(fogCol, _FogSun.rgb, saturate(sun));
    return f;
}

float3 OmnoFog(float3 col, float3 wpos)
{
    float3 fc;
    float f = OmnoFogAmount(wpos, fc);
    return lerp(col, fc, f);
}
#endif
