#version 330 core

layout (location = 0) in vec3 PositionOS;
layout (location = 4) in vec4 TexCoords;

uniform mat4 MatrixModel;
uniform mat4 MatrixView;
uniform mat4 MatrixProjection;
uniform mat4 PrevMatrixModel;
uniform mat4 PrevMatrixView;
uniform mat4 PrevMatrixProjection;

out vec4 _TexCoords;
out vec4 _PositionCS;
out vec4 _PrevPosCS;

void main()
{
    _TexCoords = TexCoords;
    _PositionCS = MatrixProjection * MatrixView * MatrixModel * vec4(PositionOS, 1.0);
    _PrevPosCS = PrevMatrixProjection * PrevMatrixView * PrevMatrixModel * vec4(PositionOS, 1.0);

    gl_Position = _PositionCS;
}