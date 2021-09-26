#version 330 core

layout (location = 0) in vec3 PositionOS;
layout (location = 1) in vec3 NormalOS;
layout (location = 2) in vec3 TangentOS;
layout (location = 3) in vec3 BitangentOS;
layout (location = 4) in vec4 TexCoords;
layout (location = 5) in vec4 Color;
layout (location = 6) in uvec4 BoneIds;
layout (location = 7) in vec4 Weights;

uniform mat4 MatrixModel;
uniform mat4 MatrixView;
uniform mat4 MatrixProjection;
uniform mat4 MatrixMainLightView;
uniform mat4 MatrixMainLightProjection;

out vec3 _PositionWS;
out vec4 _TexCoords;
out mat3 _TBN;
out vec4 _PositionLS;

void main()
{
    _PositionWS = (MatrixModel * vec4(PositionOS, 1.0)).xyz;
    _TexCoords = TexCoords;
    vec3 normalWS = normalize((MatrixModel * vec4(NormalOS, 0.0)).xyz);
    vec3 tangentWS = normalize((MatrixModel * vec4(TangentOS, 0.0)).xyz);
    vec3 bitangentWS = normalize((MatrixModel * vec4(BitangentOS, 0.0)).xyz);
    _TBN = mat3(tangentWS, bitangentWS, normalWS);
    _PositionLS = MatrixMainLightProjection * MatrixMainLightView * vec4(_PositionWS, 1.0);

    gl_Position = MatrixProjection * MatrixView * vec4(_PositionWS, 1.0);
} 