#version 330 core

in vec3 _PositionWS;
in vec4 _TexCoords;
in mat3 _TBN;
in vec4 _PositionLS;

uniform vec4 _BaseColor;
uniform sampler2D _BaseMap;
uniform vec4 _TilingOffset;
uniform float _NormalScale;
uniform sampler2D _NormalMap;
uniform float _Smoothness;
uniform float _Metallic;
uniform sampler2D _MetallicMap;
uniform vec4 _SpecularColor;
uniform sampler2D _SpecularMap;
uniform vec3 _EmissiveColor;
uniform sampler2D _EmissiveMap;
uniform float _OcclusionStrength;
uniform sampler2D _OcclusionMap;
uniform vec3 _MatCapColor;
uniform sampler2D _MatCapMap;
uniform vec3 _ViewPosWS;
uniform vec3 _MainLightDir;
uniform mat4 MatrixView;
uniform sampler2DShadow _ShadowMap;
uniform float _ShadowRange;

out vec4 FragColor;

void main()
{
    vec2 uv = _TexCoords.xy * _TilingOffset.xy + _TilingOffset.zw;
    vec4 baseMapColor = texture(_BaseMap, uv);
    vec3 normapMapVec = vec3(texture(_NormalMap, uv).xy, 0.0);
    normapMapVec = (normapMapVec * 2.0 - 1.0) * _NormalScale;
    normapMapVec.z = sqrt(1.0 - normapMapVec.x * normapMapVec.x - normapMapVec.y * normapMapVec.y);
    
    vec3 normalWS = normalize(_TBN * normapMapVec);

    vec3 normalVS = (MatrixView * vec4(normalWS, 0.0)).xyz * 0.5 + 0.5;
    vec3 matcap = texture(_MatCapMap, normalVS.xy).xyz;

    vec3 mainLightDir = normalize(_MainLightDir);
    float NdotL = max(dot(normalWS, mainLightDir), 0.0);

    vec3 viewDiffPosWS = _ViewPosWS - _PositionWS;
    vec3 viewDir = normalize(viewDiffPosWS);
    vec3 reflectDir = reflect(-mainLightDir, normalWS);

    float specular = pow(max(dot(viewDir, reflectDir), 0.0), 1.0 + 100.0 * _SpecularColor.w);

    vec3 shadowMapProjCoords = _PositionLS.xyz / _PositionLS.w;
    shadowMapProjCoords = shadowMapProjCoords * 0.5 + 0.5;
    vec2 shadowMapSize = textureSize(_ShadowMap, 0);
    vec2 shadowMapTexelSize = 1.0 / shadowMapSize;
    
    float bias = max(0.002 * (1.0 - NdotL), 0.0002);
    float mainLightDepth = shadowMapProjCoords.z - bias;

    float shadow = 0;
    if(shadowMapProjCoords.x < 0.0 || shadowMapProjCoords.x > 1.0 ||
       shadowMapProjCoords.y < 0.0 || shadowMapProjCoords.y > 1.0 ||
       shadowMapProjCoords.z < 0.0 || shadowMapProjCoords.z > 1.0)
    {
        shadow = 1.0;
    }
    else
    {
        for (int i = 0; i < 9.0; i++)
        {
            shadow += texture(_ShadowMap, 
                vec3(shadowMapProjCoords.xy + 
                shadowMapTexelSize * vec2(mod(i, 3.0) - 1, floor(i / 3.0) - 1),
                mainLightDepth));
        }
        shadow /= 9.0;
        float shadowFadeOut = smoothstep(_ShadowRange - 1.0, _ShadowRange, length(viewDiffPosWS));
        shadow = mix(shadow, 1.0, shadowFadeOut);
    }
    
    vec3 finalColor = (matcap * _MatCapColor + NdotL * shadow) *
                    baseMapColor.xyz * _BaseColor.xyz +
                    specular * _SpecularColor.xyz * shadow;

    FragColor = vec4(finalColor, baseMapColor.w * _BaseColor.w);
}