#version 330 core

in vec2 uv;

uniform sampler2D _BaseMap;

layout (location = 0) out vec4 FragColor0;
layout (location = 1) out vec4 FragColor1;
layout (location = 2) out vec4 FragColor2;
layout (location = 3) out vec4 FragColor3;

void main()
{
    vec4 baseMapColor = texture(_BaseMap, uv);

    FragColor0 = baseMapColor;
    FragColor1 = baseMapColor;
    FragColor2 = baseMapColor;
    FragColor3 = baseMapColor;
}