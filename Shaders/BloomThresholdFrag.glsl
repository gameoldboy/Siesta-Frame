#version 330 core

in vec2 uv;

uniform sampler2D _BaseMap;
uniform float _Threshold;

out vec4 FragColor;

float Luminance(vec3 linearRgb)
{
    return dot(linearRgb, vec3(0.2126729, 0.7151522, 0.0721750));
}

void main()
{
    vec3 color = texture(_BaseMap, uv).xyz;
    color = smoothstep(_Threshold, _Threshold + 0.5, Luminance(color)) * color;

    FragColor = vec4(color, 1.0);
}