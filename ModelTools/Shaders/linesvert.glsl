#version 330 core

layout (location = 0) in vec3 PositionOS;

uniform mat4 MatrixModel;
uniform mat4 MatrixView;
uniform mat4 MatrixProjection;

flat out vec3 _OriginPositionVS;
out vec3 _PositionVS;

void main()
{
    _OriginPositionVS = (MatrixView * MatrixModel * vec4(PositionOS, 1.0)).xyz;
    _PositionVS = _OriginPositionVS;

    gl_Position = MatrixProjection * vec4(_PositionVS, 1.0);
}