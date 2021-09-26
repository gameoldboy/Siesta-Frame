#version 330 core

layout (location = 0) in vec3 PositionOS;
layout (location = 1) in vec2 TexCoords;

uniform mat4 MatrixModel;
uniform mat4 MatrixView;
uniform mat4 MatrixProjection;

out vec2 uv;

void main()
{
    uv = TexCoords;

    gl_Position = MatrixProjection * MatrixView * MatrixModel * vec4(PositionOS, 1.0);
}
