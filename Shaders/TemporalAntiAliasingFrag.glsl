#version 330 core

in vec2 uv;

uniform sampler2D _BaseMap;
uniform sampler2D _HistoryMap;
uniform sampler2D _DepthTexture;
uniform sampler2D _MotionVectorMap;
uniform vec4 _Jitter;

vec2 _BaseMapTexelSize = 1.0 / textureSize(_BaseMap, 0);
vec2 _DepthTextureTexelSize = 1.0 / textureSize(_DepthTexture, 0);
vec2 _HistoryMapSize = textureSize(_HistoryMap, 0);
vec2 _HistoryMapTexelSize = 1.0 / _HistoryMapSize;

out vec4 FragColor;

#define FLT_EPS  5.960464478e-8
#define YCOCG_CHROMA_BIAS (128.0 / 255.0)

vec3 RGBToYCoCg(vec3 rgb)
{
    vec3 YCoCg;
    YCoCg.x = dot(rgb, vec3(0.25, 0.5, 0.25));
    YCoCg.y = dot(rgb, vec3(0.5, 0.0, -0.5)) + YCOCG_CHROMA_BIAS;
    YCoCg.z = dot(rgb, vec3(-0.25, 0.5, -0.25)) + YCOCG_CHROMA_BIAS;

    return YCoCg;
}

vec3 YCoCgToRGB(vec3 YCoCg)
{
    float Y = YCoCg.x;
    float Co = YCoCg.y - YCOCG_CHROMA_BIAS;
    float Cg = YCoCg.z - YCOCG_CHROMA_BIAS;

    vec3 rgb;
    rgb.x = Y + Co - Cg;
    rgb.y = Y + Cg;
    rgb.z = Y - Co - Cg;

    return rgb;
}

vec3 ClipColor(vec3 minColor, vec3 maxColor, vec3 color)
{
    vec3 p_clip = 0.5 * (maxColor + minColor);
    vec3 e_clip = 0.5 * (maxColor - minColor) + FLT_EPS;

    vec3 v_clip = color - p_clip;
    vec3 v_unit = v_clip / e_clip;
    vec3 a_unit = abs(v_unit);
    float ma_unit = max(a_unit.x, max(a_unit.y, a_unit.z));

    if (ma_unit > 1.0)
        return p_clip + v_clip / ma_unit;
    else
        return color;
}

void MinMax(vec3 samples[9], out vec3 minColor, out vec3 maxColor)
{
    vec3 color[9];

    color[0] = RGBToYCoCg(samples[0]);
    color[1] = RGBToYCoCg(samples[1]);
    color[2] = RGBToYCoCg(samples[2]);
    color[3] = RGBToYCoCg(samples[3]);
    color[4] = RGBToYCoCg(samples[4]);
    color[5] = RGBToYCoCg(samples[5]);
    color[6] = RGBToYCoCg(samples[6]);
    color[7] = RGBToYCoCg(samples[7]);
    color[8] = RGBToYCoCg(samples[8]);

    vec3 m1 = color[0] + color[1] + color[2]
                + color[3] + color[4] + color[5]
                + color[6] + color[7] + color[8];
    vec3 m2 = color[0] * color[0] + color[1] * color[1] + color[2] * color[2]
                + color[3] * color[3] + color[4] * color[4] + color[5] * color[5]
                + color[6] * color[6] + color[7] * color[7] + color[8] * color[8];
    vec3 mu = m1 / 9.0;
    vec3 sigma = sqrt(abs(m2 / 9.0 - mu * mu));
    minColor = mu - 2.0 * sigma;
    maxColor = mu + 2.0 * sigma;
}

void GetSamples(vec2 uv, out vec3 samples[9])
{
    samples[0] = texture(_BaseMap, uv + _BaseMapTexelSize * vec2(-1.0, -1.0)).xyz;
    samples[1] = texture(_BaseMap, uv + _BaseMapTexelSize * vec2( 0.0, -1.0)).xyz;
    samples[2] = texture(_BaseMap, uv + _BaseMapTexelSize * vec2( 1.0, -1.0)).xyz;
    samples[3] = texture(_BaseMap, uv + _BaseMapTexelSize * vec2(-1.0,  0.0)).xyz;
    samples[4] = texture(_BaseMap, uv).xyz;
    samples[5] = texture(_BaseMap, uv + _BaseMapTexelSize * vec2( 1.0,  0.0)).xyz;
    samples[6] = texture(_BaseMap, uv + _BaseMapTexelSize * vec2(-1.0,  1.0)).xyz;
    samples[7] = texture(_BaseMap, uv + _BaseMapTexelSize * vec2( 0.0,  1.0)).xyz;
    samples[8] = texture(_BaseMap, uv + _BaseMapTexelSize * vec2( 1.0,  1.0)).xyz;
}

vec3 SampleHistory(vec2 uv)
{
    vec2 samplePos = uv * _HistoryMapSize;
    vec2 tc1 = floor(samplePos - 0.5) + 0.5;
    vec2 f = samplePos - tc1;
    vec2 f2 = f * f;
    vec2 f3 = f * f2;

    const float c = 0.5;

    vec2 w0 = -c         * f3 +  2.0 * c         * f2 - c * f;
    vec2 w1 =  (2.0 - c) * f3 - (3.0 - c)        * f2          + 1.0;
    vec2 w2 = -(2.0 - c) * f3 + (3.0 - 2.0 * c)  * f2 + c * f;
    vec2 w3 = c          * f3 - c                * f2;

    vec2 w12 = w1 + w2;
    vec2 tc0 = _HistoryMapTexelSize * (tc1 - 1.0);
    vec2 tc3 = _HistoryMapTexelSize * (tc1 + 2.0);
    vec2 tc12 = _HistoryMapTexelSize  * (tc1 + w2 / w12);

    vec3 s0 = texture(_HistoryMap, vec2(tc12.x, tc0.y)).xyz;
    vec3 s1 = texture(_HistoryMap, vec2(tc0.x, tc12.y)).xyz;
    vec3 s2 = texture(_HistoryMap, vec2(tc12.x, tc12.y)).xyz;
    vec3 s3 = texture(_HistoryMap, vec2(tc3.x, tc0.y)).xyz;
    vec3 s4 = texture(_HistoryMap, vec2(tc12.x, tc3.y)).xyz;

    float cw0 = (w12.x * w0.y);
    float cw1 = (w0.x * w12.y);
    float cw2 = (w12.x * w12.y);
    float cw3 = (w3.x * w12.y);
    float cw4 = (w12.x *  w3.y);

    vec3 min_color = min(s0, min(s1, s2));
    min_color = min(min_color, min(s3, s4));

    vec3 max_color = max(s0, max(s1, s2));
    max_color = max(max_color, max(s3, s4));

    s0 *= cw0;
    s1 *= cw1;
    s2 *= cw2;
    s3 *= cw3;
    s4 *= cw4;

    vec3 historyFiltered = s0 + s1 + s2 + s3 + s4;
    float weightSum = cw0 + cw1 + cw2 + cw3 + cw4;

    vec3 filteredVal = historyFiltered * (1.0 / weightSum);

    return clamp(filteredVal, min_color, max_color);
}

float Luminance(vec3 linearRgb)
{
    return dot(linearRgb, vec3(0.2126729, 0.7151522, 0.0721750));
}

vec3 ReinhardToneMap(vec3 c)
{
    return c * (1.0 / (Luminance(c) + 1.0));
}

vec3 InverseReinhardToneMap(vec3 c)
{
    return c * (1.0 / (1.0 - Luminance(c)));
}

#define ZCMP_GT(a, b) (a > b)

vec2 FindClosestUV(float depth, vec2 uv)
{
    vec2 dd = _DepthTextureTexelSize;
    vec2 du = vec2(dd.x, 0.0);
    vec2 dv = vec2(0.0, dd.y);

    vec3 dtl = vec3(-1, -1, texture(_DepthTexture, uv - dv - du).x);
    vec3 dtc = vec3( 0, -1, texture(_DepthTexture, uv - dv).x);
    vec3 dtr = vec3( 1, -1, texture(_DepthTexture, uv - dv + du).x);

    vec3 dml = vec3(-1, 0, texture(_DepthTexture, uv - du).x);
    vec3 dmc = vec3( 0, 0, depth);
    vec3 dmr = vec3( 1, 0, texture(_DepthTexture, uv + du).x);

    vec3 dbl = vec3(-1, 1, texture(_DepthTexture, uv + dv - du).x);
    vec3 dbc = vec3( 0, 1, texture(_DepthTexture, uv + dv).x);
    vec3 dbr = vec3( 1, 1, texture(_DepthTexture, uv + dv + du).x);

    vec3 dmin = dtl;
    if (ZCMP_GT(dmin.z, dtc.z)) dmin = dtc;
    if (ZCMP_GT(dmin.z, dtr.z)) dmin = dtr;

    if (ZCMP_GT(dmin.z, dml.z)) dmin = dml;
    if (ZCMP_GT(dmin.z, dmc.z)) dmin = dmc;
    if (ZCMP_GT(dmin.z, dmr.z)) dmin = dmr;

    if (ZCMP_GT(dmin.z, dbl.z)) dmin = dbl;
    if (ZCMP_GT(dmin.z, dbc.z)) dmin = dbc;
    if (ZCMP_GT(dmin.z, dbr.z)) dmin = dbr;

    return uv + dd.xy * dmin.xy;
}

void main()
{
    float depth = texture(_DepthTexture, uv).x;
    vec2 motionVector = texture(_MotionVectorMap, FindClosestUV(depth, uv)).xy;

    vec2 prevUV = uv - motionVector + _Jitter.xy - _Jitter.zw;
    motionVector = uv - prevUV;

    vec3 samples[9];
    GetSamples(uv, samples);
    if (prevUV.x > 1 || prevUV.y > 1 || prevUV.x < 0 || prevUV.y < 0)
    {
        FragColor = vec4(samples[4], 1.0);
        return;
    }
    vec3 historyMapColor = SampleHistory(prevUV);
    vec3 minColor, maxColor;
    MinMax(samples, minColor, maxColor);
    historyMapColor = YCoCgToRGB(ClipColor(minColor, maxColor, RGBToYCoCg(historyMapColor)));
    float finalBlend = mix(0.9375, 0.2, clamp(length(motionVector) * 20.0, 0.0, 1.0));
    vec3 color = ReinhardToneMap(samples[4]);
    historyMapColor = ReinhardToneMap(historyMapColor);
    vec3 finalColor = InverseReinhardToneMap(mix(color, historyMapColor, finalBlend));

    FragColor = vec4(finalColor, 1.0);
}