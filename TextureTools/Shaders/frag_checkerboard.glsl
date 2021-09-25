#version 330 core

in vec2 _TexCoords;

uniform sampler2D _BaseMap;
uniform vec2 _TexCoordScale;

out vec4 FragColor;

void main()
{
    FragColor = texture(_BaseMap, _TexCoords * _TexCoordScale);
}