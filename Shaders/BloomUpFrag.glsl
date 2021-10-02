#version 330 core

in vec2 uv;

uniform sampler2D _BaseMap;
uniform sampler2D _PrevMap;
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
    color *= 0.083333333;
    vec3 prevColor = texture(_PrevMap, uv).xyz;

    FragColor = vec4(mix(prevColor, color, 0.68), 1.0);
}