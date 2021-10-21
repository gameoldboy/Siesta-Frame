#version 330 core

layout (location = 0) in vec3 PositionOS;

uniform mat4 MatrixModel;
uniform mat4 MatrixView;
uniform mat4 MatrixProjection;

flat out vec3 _OriginPositionWS;
out vec3 _PositionWS;

void main()
{
    _OriginPositionWS = (MatrixModel * vec4(PositionOS, 1.0)).xyz;
    _PositionWS = _OriginPositionWS;
    
    gl_Position = MatrixProjection * MatrixView * vec4(_PositionWS, 1.0);
}