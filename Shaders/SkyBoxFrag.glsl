#version 330 core

in vec3 _TexCoords;
in vec3 _NormalWS;
in vec4 _PositionCS;
in vec4 _PrevPosCS;

uniform samplerCube _BaseMap;
uniform vec3 _SkyboxColor;

layout (location = 0) out vec4 FragColor;
layout (location = 1) out vec3 NormalMap;
layout (location = 2) out vec2 MotionVectors;

void main()
{
    vec3 uv = _TexCoords;
    FragColor = vec4(texture(_BaseMap, uv).xyz * _SkyboxColor, 1.0);

    NormalMap = normalize(_NormalWS);
    
    vec2 screenPos = _PositionCS.xy / _PositionCS.w;
    vec2 prevScreenPos = _PrevPosCS.xy / _PrevPosCS.w;

    MotionVectors = (screenPos - prevScreenPos) * 0.5;
}