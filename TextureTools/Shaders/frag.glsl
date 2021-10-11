#version 330 core

in vec2 _TexCoords;

uniform sampler2D _BaseMap;
uniform vec4 _Switch;
uniform bool _FlipY;
uniform bool _Tonemap;
uniform bool _ToLinear;
uniform bool _sRGBOutput;

out vec4 FragColor;

float sRGB2Linear(float sRGB)
{
    if(sRGB <= 0.04045)
    {
        return sRGB / 12.92;
    }
    else if(sRGB <= 1.0)
    {
        return pow(((sRGB + 0.055) / 1.055), 2.4);
    }
    else
    {
        return pow(sRGB, 2.2);
    }
}

vec3 sRGB2Linear(vec3 sRGB)
{
    return vec3(sRGB2Linear(sRGB.x), sRGB2Linear(sRGB.y), sRGB2Linear(sRGB.z));
}

float Linear2sRGB(float linear)
{
    if(linear <= 0.0031308)
    {
        return linear * 12.92;
    }
    else if(linear <= 1.0)
    {
        return 1.055 * pow(linear, 1.0 / 2.4) - 0.055;
    }
    else
    {
        return pow(linear, 1 / 2.2);
    }
}

vec3 Linear2sRGB(vec3 linear)
{
    return vec3(Linear2sRGB(linear.x), Linear2sRGB(linear.y), Linear2sRGB(linear.z));
}

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
    vec2 uv = _FlipY ? vec2(_TexCoords.x, 1.0 - _TexCoords.y) : _TexCoords;
    vec4 baseMapColor = texture(_BaseMap, uv);

    if(_Switch.x > 0.0 && _Switch.y == 0.0 && _Switch.z == 0.0)
    {
        baseMapColor.xyz = baseMapColor.xxx;
        baseMapColor.w = _Switch.w > 0.0 ? baseMapColor.w : 1.0;
    }
    else if(_Switch.x == 0.0 && _Switch.y > 0.0 && _Switch.z == 0.0)
    {
        baseMapColor.xyz = baseMapColor.yyy;
        baseMapColor.w = _Switch.w > 0.0 ? baseMapColor.w : 1.0;
    }
    else if(_Switch.x == 0.0 && _Switch.y == 0.0 && _Switch.z > 0.0)
    {
        baseMapColor.xyz = baseMapColor.zzz;
        baseMapColor.w = _Switch.w > 0.0 ? baseMapColor.w : 1.0;
    }
    else if(_Switch.x > 0.0 || _Switch.y > 0.0 || _Switch.z > 0.0)
    {
        baseMapColor.x = _Switch.x * baseMapColor.x;
        baseMapColor.y = _Switch.y * baseMapColor.y;
        baseMapColor.z = _Switch.z * baseMapColor.z;
        baseMapColor.w = _Switch.w > 0.0 ? baseMapColor.w : 1.0;
    }
    else if(_Switch.w > 0.0)
    {
        baseMapColor.xyz = baseMapColor.www;
        baseMapColor.w = 1.0;
    }
    else
    {
        baseMapColor = vec4(0.0);
    }

    if(_Tonemap)
    {
        baseMapColor.xyz = ACESFilm(baseMapColor.xyz * 0.6);
        baseMapColor.xyz = pow(baseMapColor.xyz, vec3(0.91));
    }
    if(_ToLinear)
    {
        baseMapColor.xyz = sRGB2Linear(baseMapColor.xyz);
    }
    if(_sRGBOutput)
    {
        baseMapColor.xyz = Linear2sRGB(baseMapColor.xyz);
    }

    FragColor = baseMapColor;
}