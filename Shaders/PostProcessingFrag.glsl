#version 330 core

in vec2 uv;

uniform sampler2D _BaseMap;
uniform sampler2D _DepthTexture;
uniform sampler2D _ShadowMap;
uniform sampler2D _NormalTexture;
uniform sampler2D _MotionVectors;
uniform sampler2D _BloomMap;
uniform float _BloomIntensity;
uniform float _Exposure;
uniform bool _Tonemap;
uniform vec4 _ColorGrading;
uniform vec2 _Jitter;

#define _Contrast _ColorGrading.x
#define _Saturation _ColorGrading.y
#define _Temperature _ColorGrading.z
#define _Tint _ColorGrading.w

out vec4 FragColor;

float sRGB2Linear (float sRGB)
{
    if (sRGB <= 0.04045)
    {
        return sRGB / 12.92;
    }
    else if (sRGB < 1.0)
    {
        return pow((sRGB + 0.055)/1.055, 2.4);
    }
    else
    {
        return pow(sRGB, 2.2);
    }
}

float Linear2sRGB (float linear)
{
    if (linear <= 0.0)
    {
        return 0.0;
    }
    else if (linear <= 0.0031308)
    {
        return 12.92 * linear;
    }
    else if (linear < 1.0)
    {
        return 1.055 * pow(linear, 0.4166667) - 0.055;
    }
    else
    {
        return pow(linear, 0.45454545);
    }
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

vec3 log10(vec3 x)
{
    return log(x) / log(10.0);
}

vec3 LinearToLogC(vec3 x)
{
    return 0.244161 * log10(max(5.555556 * x + 0.047996, 0.0)) + 0.386036;
}

vec3 LogCToLinear(vec3 x)
{
    return (pow(vec3(10.0), (x - 0.386036) / 0.244161) - 0.047996) / 5.555556;
}

#define MidGray 0.4135884

float Luminance(vec3 linearRgb)
{
    return dot(linearRgb, vec3(0.2126729, 0.7151522, 0.0721750));
}

float StandardIlluminantY(float x)
{
    return 2.87 * x - 3.0 * x * x - 0.27509507;
}

vec3 CIExyToLMS(float x, float y)
{
    float Y = 1.0;
    float X = Y * x / y;
    float Z = Y * (1.0 - x - y) / y;

    float L =  0.7328 * X + 0.4296 * Y - 0.1624 * Z;
    float M = -0.7036 * X + 1.6975 * Y + 0.0061 * Z;
    float S =  0.0030 * X + 0.0136 * Y + 0.9834 * Z;

    return vec3(L, M, S);
}

vec3 ColorBalanceToLMSCoeffs(float temperature, float tint)
{
    float t1 = temperature * 1.5384615;
    float t2 = tint * 1.5384615;

    float x = 0.31271 - t1 * (t1 < 0.0 ? 0.1 : 0.05);
    float y = StandardIlluminantY(x) + t2 * 0.05;

    vec3 w1 = vec3(0.949237, 1.03542, 1.08728);
    vec3 w2 = CIExyToLMS(x, y);
    return vec3(w1.x / w2.x, w1.y / w2.y, w1.z / w2.z);
}

vec3 LinearToLMS(vec3 x)
{
    const mat3 LIN_2_LMS_MAT = mat3(
        3.90405e-1, 5.49941e-1, 8.92632e-3,
        7.08416e-2, 9.63172e-1, 1.35775e-3,
        2.31082e-2, 1.28021e-1, 9.36245e-1);

    return LIN_2_LMS_MAT * x;
}

vec3 LMSToLinear(vec3 x)
{
    const mat3 LMS_2_LIN_MAT = mat3(
        2.85847e+0, -1.62879e+0, -2.48910e-2,
        -2.10182e-1,  1.15820e+0,  3.24281e-4,
        -4.18120e-2, -1.18169e-1,  1.06867e+0);

    return LMS_2_LIN_MAT * x;
}

float hash(vec2 input)
{
    return fract(1.0e4 * sin(17.0 * input.x + 0.1 * input.y) * (0.1 + abs(sin(13.0 * input.y + input.x))));
}

void main()
{
    vec4 baseMapColor = texture(_BaseMap, uv);
    // apply bloom
    baseMapColor.xyz += texture(_BloomMap, uv).xyz * _BloomIntensity;
    // exposure
    float exposure = pow(2.0, _Exposure);
    baseMapColor.xyz = baseMapColor.xyz * exposure;
    // tonemapping
    if(_Tonemap)
    {
        vec3 acesColor = ACESFilm(baseMapColor.xyz * 0.6);
        baseMapColor.xyz = pow(acesColor, vec3(0.91));
    }
    // white balance
    vec3 colorLMS = LinearToLMS(baseMapColor.xyz);
    colorLMS *= ColorBalanceToLMSCoeffs(_Temperature, _Tint);
    baseMapColor.xyz = LMSToLinear(colorLMS);
    // contrast
    baseMapColor.xyz = LinearToLogC(baseMapColor.xyz);
    baseMapColor.xyz = (baseMapColor.xyz - MidGray) * _Contrast + MidGray;
    baseMapColor.xyz = LogCToLinear(baseMapColor.xyz);
    // saturation
    float luma = Luminance(baseMapColor.xyz);
    baseMapColor.xyz = vec3(luma) + (baseMapColor.xyz - vec3(luma)) * _Saturation;
    // dithering
    baseMapColor.xyz = baseMapColor.xyz + 0.00390625 * (hash(uv + _Jitter) - 0.5);

    FragColor = baseMapColor;
}