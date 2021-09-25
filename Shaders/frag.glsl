#version 330 core

in vec3 _PositionWS;
in vec4 _TexCoords;
in mat3 _TBN;

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

    vec3 viewDir = normalize(_ViewPosWS - _PositionWS);
    vec3 reflectDir = reflect(-mainLightDir, normalWS);

    float specular = pow(max(dot(viewDir, reflectDir), 0.0), 1.0 + 100.0 * _SpecularColor.w);

    vec3 finalColor = (matcap * _MatCapColor + NdotL) * baseMapColor.xyz * _BaseColor.xyz +
                      specular * _SpecularColor.xyz;

    FragColor = vec4(finalColor, baseMapColor.w * _BaseColor.w);
}