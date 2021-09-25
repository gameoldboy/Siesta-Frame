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
    if(sRGB <= 0.04045f)
    {
        return sRGB / 12.92f;
    }
    else if(sRGB <= 1f)
    {
        return pow(((sRGB + 0.055f) / 1.055f), 2.4f);
    }
    else
    {
        return pow(sRGB, 2.2f);
    }
}

vec3 sRGB2Linear(vec3 sRGB)
{
    return vec3(sRGB2Linear(sRGB.x), sRGB2Linear(sRGB.y), sRGB2Linear(sRGB.z));
}

float Linear2sRGB(float linear)
{
    if(linear <= 0.0031308f)
    {
        return linear * 12.92f;
    }
    else if(linear <= 1f)
    {
        return 1.055f * pow(linear, 1f / 2.4f) - 0.055f;
    }
    else
    {
        return pow(linear, 1 / 2.2f);
    }
}

vec3 Linear2sRGB(vec3 linear)
{
    return vec3(Linear2sRGB(linear.x), Linear2sRGB(linear.y), Linear2sRGB(linear.z));
}

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
    vec2 uv = _FlipY ? vec2(_TexCoords.x, 1f - _TexCoords.y) : _TexCoords;
    vec4 baseMapColor = texture(_BaseMap, uv);

    if(_Switch.x > 0f && _Switch.y == 0f && _Switch.z == 0f)
    {
        baseMapColor = vec4(baseMapColor.xxx,
            _Switch.w > 0f ? baseMapColor.w : 1f);
    }
    else if(_Switch.x == 0f && _Switch.y > 0f && _Switch.z == 0f)
    {
        baseMapColor = vec4(baseMapColor.yyy,
            _Switch.w > 0f ? baseMapColor.w : 1f);
    }
    else if(_Switch.x == 0f && _Switch.y == 0f && _Switch.z > 0f)
    {
        baseMapColor = vec4(baseMapColor.zzz,
            _Switch.w > 0f ? baseMapColor.w : 1f);
    }
    else if(_Switch.x > 0f || _Switch.y > 0f || _Switch.z > 0f)
    {
        baseMapColor = vec4(
        _Switch.x * baseMapColor.x,
        _Switch.y * baseMapColor.y,
        _Switch.z * baseMapColor.z,
        _Switch.w > 0f ? baseMapColor.w : 1f);
    }
    else if(_Switch.w > 0f)
    {
        baseMapColor = vec4(baseMapColor.www, 1f);
    }
    else
    {
        baseMapColor = vec4(0f);
    }

    baseMapColor = _Tonemap ? vec4(ACESFilm(baseMapColor.xyz), baseMapColor.w) : baseMapColor;
    baseMapColor = _ToLinear ? vec4(sRGB2Linear(baseMapColor.xyz), baseMapColor.w) : baseMapColor;
    baseMapColor = _sRGBOutput ? vec4(Linear2sRGB(baseMapColor.xyz), baseMapColor.w) : baseMapColor;

    FragColor = baseMapColor;
}