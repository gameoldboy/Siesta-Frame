#version 430 core

layout (location = 0) in vec3 PositionOS;
layout (location = 4) in vec4 TexCoords;

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

#define MatrixModel SSBOData[gl_InstanceID].ModelMatrix
uniform mat4 MatrixView;
uniform mat4 MatrixProjection;
#define PrevMatrixModel SSBOData[gl_InstanceID].PrevModelMatrix
uniform mat4 PrevMatrixView;
uniform mat4 PrevMatrixProjection;

out vec4 _TexCoords;
out vec4 _PositionCS;
out vec4 _PrevPosCS;
out flat int InstanceID;

void main()
{
    _TexCoords = TexCoords;
    _PositionCS = MatrixProjection * MatrixView * MatrixModel * vec4(PositionOS, 1.0);
    _PrevPosCS = PrevMatrixProjection * PrevMatrixView * PrevMatrixModel * vec4(PositionOS, 1.0);
    InstanceID = gl_InstanceID;

    gl_Position = _PositionCS;
}