#version 430 core

in vec4 _TexCoords;
in vec4 _PositionCS;
in vec4 _PrevPosCS;
in flat int InstanceID;

struct InstancedData
{
    mat4 ModelMatrix;
    vec4 BaseColor;
    vec4 TilingOffset;
    float NormalScale;
    float Smoothness;
    float Metallic;
    float OcclusionStrength;
    vec4 SpecularColor;
    vec3 SelectedColor;
    vec3 MatCapColor;
    vec3 EmissiveColor;
    mat4 PrevModelMatrix;
};

layout (std430, binding = 0) buffer InstancedBuffer
{
    InstancedData SSBOData[];
};

#define _BaseColor SSBOData[InstanceID].BaseColor
uniform sampler2D _BaseMap;
#define _TilingOffset SSBOData[InstanceID].TilingOffset
uniform bool _AlphaTest;

out vec2 FragColor;

void main()
{
    if(_AlphaTest)
    {
        vec2 uv = _TexCoords.xy * _TilingOffset.xy + _TilingOffset.zw;
        float alpha = min(texture(_BaseMap, uv).w * 1.004, 1.0) * _BaseColor.w;

        if(alpha < 0.1)
        {
            discard;
        }
    }

    vec2 screenPos = _PositionCS.xy / _PositionCS.w * 0.5;
    vec2 prevScreenPos = _PrevPosCS.xy / _PrevPosCS.w * 0.5;

    FragColor = screenPos - prevScreenPos;
}