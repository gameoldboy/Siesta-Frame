#version 330 core

in vec2 uv;

uniform sampler2D _BaseMap;
uniform vec2 _Size;
vec2 TexelSize = 1.0 / _Size;

out vec4 FragColor;

void main()
{
    vec3 color = texture(_BaseMap, uv + vec2(-TexelSize.x * 2.0, 0.0)).xyz;
    color += texture(_BaseMap, uv + vec2(-TexelSize.x, TexelSize.y)).xyz * 2.0;
    color += texture(_BaseMap, uv + vec2(0.0, TexelSize.y * 2.0)).xyz;
    color += texture(_BaseMap, uv + vec2(TexelSize.x, TexelSize.y)).xyz * 2.0;
    color += texture(_BaseMap, uv + vec2(TexelSize.x * 2.0, 0.0)).xyz;
    color += texture(_BaseMap, uv + vec2(TexelSize.x, -TexelSize.y)).xyz * 2.0;
    color += texture(_BaseMap, uv + vec2(0.0, -TexelSize.y * 2.0)).xyz;
    color += texture(_BaseMap, uv + vec2(-TexelSize.x, -TexelSize.y)).xyz * 2.0;
    color /= 12.0;

    FragColor = vec4(color, 1.0);
}