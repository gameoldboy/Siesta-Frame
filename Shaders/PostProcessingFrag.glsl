#version 330 core

in vec2 uv;

uniform sampler2D _BaseMap;
uniform sampler2D _DepthTexture;
uniform sampler2D _MotionVectorMap;

out vec4 FragColor;

vec3 ACESFilm(vec3 x)
{
    float a = 2.51;
    float b = 0.03;
    float c = 2.43;
    float d = 0.59;
    float e = 0.14;
    return clamp((x * (a * x + b)) / (x * (c * x + d) + e), 0.0, 1.0);
}

void main()
{
    vec4 baseMapColor = texture(_BaseMap, uv);

    FragColor = vec4(ACESFilm(baseMapColor.xyz), baseMapColor.w);
    // FragColor = baseMapColor;
}