#version 430 core

in vec4 _TexCoords;
in vec4 _PositionCS;
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
uniform vec2 _ScreenSize;

float DitherThresholds[64] = float[](
    0.0, 0.5, 0.125, 0.625, 0.03125, 0.53125, 0.15625, 0.65625,
    0.75, 0.25, 0.875, 0.375, 0.78125, 0.28125, 0.90625, 0.40625,
    0.1875, 0.6875, 0.0625, 0.5625, 0.21875, 0.71875, 0.09375, 0.59375,
    0.9375, 0.4375, 0.8125, 0.3125, 0.96875, 0.46875, 0.84375, 0.34375,
    0.046875, 0.546875, 0.171875, 0.671875, 0.015625, 0.515625, 0.140625, 0.640625,
    0.796875, 0.296875, 0.921875, 0.421875, 0.765625, 0.265625, 0.890625, 0.390625,
    0.234375, 0.734375, 0.109375, 0.609375, 0.203125, 0.703125, 0.078125, 0.578125,
    0.984375, 0.484375, 0.859375, 0.359375, 0.953125, 0.453125, 0.828125, 0.328125
);

void main()
{
    if(_AlphaTest)
    {
        vec2 uv = _TexCoords.xy * _TilingOffset.xy + _TilingOffset.zw;
        float alpha = min(texture(_BaseMap, uv).w * 1.004, 1.0) * _BaseColor.w;

        ivec2 screenPos = ivec2(((_PositionCS.xy / _PositionCS.w) * 0.5 + 0.5) * _ScreenSize);
        int index = screenPos.x % 8 * 8 + screenPos.y % 8;
        if(alpha < DitherThresholds[index])
        {
            discard;
        }
    }
}