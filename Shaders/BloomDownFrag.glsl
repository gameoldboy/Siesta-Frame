#version 330 core

in vec2 uv;

uniform sampler2D _BaseMap;
uniform vec2 _Size;
vec2 HalfTexelSize = 0.5 / _Size;

out vec4 FragColor;

void main()
{
    vec3 color = texture(_BaseMap, uv).xyz * 4.0;
    color += texture(_BaseMap, uv - HalfTexelSize.xy).xyz;
    color += texture(_BaseMap, uv + HalfTexelSize.xy).xyz;
    color += texture(_BaseMap, uv + vec2(HalfTexelSize.x, -HalfTexelSize.y)).xyz;
    color += texture(_BaseMap, uv - vec2(HalfTexelSize.x, -HalfTexelSize.y)).xyz;
    color *= 0.125;

    FragColor = vec4(color, 1.0);
}