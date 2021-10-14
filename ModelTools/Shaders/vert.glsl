#version 330 core

layout (location = 0) in vec3 PositionOS;
layout (location = 1) in vec3 NormalOS;
layout (location = 2) in vec4 TangentOS;
layout (location = 3) in vec4 TexCoords;
layout (location = 4) in vec4 Color;
layout (location = 5) in uvec4 BoneIds;
layout (location = 6) in vec4 Weights;

uniform mat4 MatrixModel;
uniform mat4 MatrixView;
uniform mat4 MatrixProjection;

out vec3 _PositionWS;
out vec4 _TexCoords;
out vec3 _NormalWS;
out vec4 _PositionCS;

void main()
{
    _PositionWS = (MatrixModel * vec4(PositionOS, 1.0)).xyz;
    _TexCoords = TexCoords;
    _NormalWS = normalize((MatrixModel * vec4(NormalOS, 0.0)).xyz);
    _PositionCS = MatrixProjection * MatrixView * vec4(_PositionWS, 1.0);

    gl_Position = _PositionCS;
}