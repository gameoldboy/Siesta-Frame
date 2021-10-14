#version 330 core

in vec3 _PositionWS;
in vec4 _TexCoords;
in vec3 _NormalWS;
in vec4 _PositionCS;

uniform vec4 _BaseColor;
uniform sampler2D _BaseMap;
uniform vec3 _ViewPosWS;
uniform vec3 _MainLightDir;
uniform bool _AlphaDither;
uniform vec2 _ScreenSize;

out vec4 FragColor;

#define PI 3.14159265358979323846

float OrenNayar(vec3 lightDir, vec3 viewDir, vec3 normal, float roughness) {
    float LdotV = dot(lightDir, viewDir);
    float NdotL = dot(lightDir, normal);
    float NdotV = dot(normal, viewDir);

    float s = LdotV - NdotL * NdotV;
    float t = mix(1.0, max(NdotL, NdotV), step(0.0, s));

    float sigma2 = roughness * roughness;
    float A = 1.0 + sigma2 * (1.0 / (sigma2 + 0.13) + 0.5 / (sigma2 + 0.33));
    float B = 0.45 * sigma2 / (sigma2 + 0.09);

    return max(0.0, NdotL) * (A + B * s / t) / PI;
}

float DitherThresholds[64] = float[](
    0.0, 0.5, 0.125, 0.625, 0.03125, 0.53125, 0.15625, 0.65625,
    0.75, 0.25, 0.875, 0.375, 0.78125, 0.28125, 0.90625, 0.40625,
    0.1875, 0.6875, 0.0625, 0.5625, 0.21875, 0.71875, 0.09375, 0.59375,
    0.9375, 0.4375, 0.8125, 0.3125, 0.96875, 0.46875, 0.84375, 0.34375,
    0.046875, 0.546875, 0.171875, 0.671875, 0.015625, 0.515625, 0.140625, 0.640625,
    0.796875, 0.296875, 0.921875, 0.421875, 0.765625, 0.265625, 0.890625, 0.390625,
    0.234375, 0.734375, 0.109375, 0.609375, 0.203125, 0.703125, 0.078125, 0.578125,
    0.984375, 0.484375, 0.859375, 0.359375, 0.953125, 0.453125, 0.828125, 0.328125
);

void main()
{
    vec2 uv = _TexCoords.xy;
    vec4 baseMapColor = texture(_BaseMap, uv);

    vec3 mainLightDir = normalize(_MainLightDir);

    vec3 viewDiffPosWS = _ViewPosWS - _PositionWS;
    vec3 viewDir = normalize(viewDiffPosWS);

    float diffuse = OrenNayar(mainLightDir, viewDir, _NormalWS, 0.5);
    
    float alpha = min(baseMapColor.w * 1.004, 1.0) * _BaseColor.w;
    if(_AlphaDither)
    {
        ivec2 screenPos = ivec2(((_PositionCS.xy / _PositionCS.w) * 0.5 + 0.5) * _ScreenSize);
        int index = screenPos.x % 8 * 8 + screenPos.y % 8;
        if(alpha < DitherThresholds[index])
        {
            discard;
        }
    }
    
    vec3 finalColor = diffuse * baseMapColor.xyz * _BaseColor.xyz;

    FragColor = vec4(finalColor, alpha);
}