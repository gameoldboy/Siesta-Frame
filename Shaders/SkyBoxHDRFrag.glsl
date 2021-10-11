#version 330 core

in vec3 _TexCoords;
in vec3 _NormalWS;
in vec4 _PositionCS;
in vec4 _PrevPosCS;

uniform sampler2D _BaseMap;
uniform vec3 _SkyboxColor;

layout (location = 0) out vec4 FragColor;
layout (location = 1) out vec3 NormalMap;
layout (location = 2) out vec2 MotionVectors;

const vec2 invAtan = vec2(0.1591, 0.3183);
vec2 SampleSphericalMap(vec3 v)
{
    vec2 uv = vec2(atan(v.z, v.x), asin(v.y));
    uv *= invAtan;
    uv += 0.5;
    return uv;
}

void main()
{
    vec2 uv = SampleSphericalMap(normalize(_TexCoords));
    FragColor = vec4(texture(_BaseMap, uv).xyz * _SkyboxColor, 1.0);

    NormalMap = normalize(_NormalWS);
    
    vec2 screenPos = _PositionCS.xy / _PositionCS.w;
    vec2 prevScreenPos = _PrevPosCS.xy / _PrevPosCS.w;

    MotionVectors = (screenPos - prevScreenPos) * 0.5;
}