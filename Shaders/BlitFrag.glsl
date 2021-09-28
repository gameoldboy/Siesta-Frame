#version 330 core

in vec2 uv;

uniform sampler2D _BaseMap;

out vec4 FragColor;

void main()
{
    vec4 baseMapColor = texture(_BaseMap, uv);

    FragColor = baseMapColor;
}