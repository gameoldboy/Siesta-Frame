#version 330 core

in vec2 uv;

uniform sampler2D _BaseMap;

layout (location = 0) out vec4 FragColor0;
layout (location = 1) out vec4 FragColor1;

void main()
{
    vec4 baseMapColor = texture(_BaseMap, uv);

    FragColor0 = baseMapColor;
    FragColor1 = baseMapColor;
}