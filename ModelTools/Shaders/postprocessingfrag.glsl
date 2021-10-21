#version 330 core

in vec2 uv;

uniform sampler2DMS _BaseMap;
ivec2 _BaseMapSize = textureSize(_BaseMap);

out vec4 FragColor;

void main()
{
    vec3 baseMapColor = vec3(0.0);
    for(int i = 0; i < 4; i++)
    {
       baseMapColor += texelFetch(_BaseMap, ivec2(uv * _BaseMapSize), i).xyz;
    }
    baseMapColor *= 0.25;

    FragColor = vec4(baseMapColor, 1.0);
}