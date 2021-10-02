#version 330 core

in vec2 uv;

uniform sampler2D _BaseMap;
uniform float _Threshold;

out vec4 FragColor;

void main()
{
    vec3 color = texture(_BaseMap, uv).xyz;
    float brightness = max(max(color.r, color.g), color.b);
    float softness = clamp(brightness - _Threshold + 0.5, 0.0, 2.0 * 0.5);
    softness = (softness * softness) / (4.0 * 0.5 + 1e-4);
    float multiplier = max(brightness - _Threshold, softness) / max(brightness, 1e-4);
    color *= multiplier;

    FragColor = vec4(color, 1.0);
}