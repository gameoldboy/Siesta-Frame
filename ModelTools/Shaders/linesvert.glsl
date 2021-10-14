#version 330 core

layout (location = 0) in vec3 PositionOS;

uniform mat4 MatrixModel;
uniform mat4 MatrixView;
uniform mat4 MatrixProjection;

void main()
{
    gl_Position = MatrixProjection * MatrixView * MatrixModel * vec4(PositionOS, 1.0);
}