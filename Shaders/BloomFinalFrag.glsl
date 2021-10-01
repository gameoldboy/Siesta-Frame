#version 330 core

in vec2 uv;

uniform sampler2D _BaseMap;
uniform sampler2D _FinalMap;
uniform float _Intensity;

out vec4 FragColor;

void main()
{
    vec3 baseMapColor = texture(_BaseMap, uv).xyz;
    vec3 finalMapColor = texture(_FinalMap, uv).xyz;
    vec3 finalColor = baseMapColor + finalMapColor * _Intensity;

    FragColor = vec4(finalColor, 1.0);
}