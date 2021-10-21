#version 330 core

layout (location = 0) in vec3 PositionOS;
layout (location = 1) in vec2 TexCoords;

out vec2 uv;

void main()
{
    uv = TexCoords;
    
    gl_Position = vec4(PositionOS, 1.0);
}