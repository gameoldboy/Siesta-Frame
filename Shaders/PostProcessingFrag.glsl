#version 330 core

in vec2 uv;

uniform sampler2D _BaseMap;

out vec4 FragColor;

vec3 ACESFilm(vec3 x)
{
    float a = 2.51f;
    float b = 0.03f;
    float c = 2.43f;
    float d = 0.59f;
    float e = 0.14f;
    return clamp((x * (a * x + b)) / (x * (c * x + d) + e), 0f, 1f);
}

void main()
{
    vec4 baseMapColor = texture(_BaseMap, uv);
    
    FragColor = vec4(ACESFilm(baseMapColor.xyz), baseMapColor.w);
    // FragColor = baseMapColor;
}