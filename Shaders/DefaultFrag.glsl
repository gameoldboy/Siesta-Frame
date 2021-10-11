#version 330 core

in vec3 _PositionWS;
in vec4 _TexCoords;
in mat3 _TBN;
in vec4 _PositionLS;
in vec4 _PositionCS;
in vec4 _PrevPosCS;

uniform vec4 _BaseColor;
uniform sampler2D _BaseMap;
uniform vec4 _TilingOffset;
uniform float _NormalScale;
uniform sampler2D _NormalMap;
uniform float _Smoothness;
uniform float _Metallic;
uniform sampler2D _MetallicMap;
uniform vec4 _SpecularColor;
uniform sampler2D _SpecularMap;
uniform vec3 _EmissiveColor;
uniform sampler2D _EmissiveMap;
uniform vec3 _SelectedColor;
uniform float _OcclusionStrength;
uniform sampler2D _OcclusionMap;
uniform vec3 _MatCapColor;
uniform sampler2D _MatCapMap;
uniform vec3 _ViewPosWS;
uniform vec3 _MainLightDir;
uniform mat4 MatrixView;
uniform sampler2DShadow _ShadowMap;
uniform float _ShadowRange;
uniform vec2 _Jitter;
uniform bool _AlphaHashed;
uniform bool _AlphaDither;
uniform vec2 _ScreenSize;

vec2 ShadowMapSize = textureSize(_ShadowMap, 0);
vec2 ShadowMapTexelSize = 1.0 / ShadowMapSize;

layout (location = 0) out vec4 FragColor;
layout (location = 1) out vec4 NormalMap;
layout (location = 2) out vec4 MotionVectors;

vec2 poissonDisk[16] = vec2[]( 
    vec2( -0.94201624, -0.39906216 ), 
    vec2( 0.94558609, -0.76890725 ), 
    vec2( -0.094184101, -0.92938870 ), 
    vec2( 0.34495938, 0.29387760 ), 
    vec2( -0.91588581, 0.45771432 ), 
    vec2( -0.81544232, -0.87912464 ), 
    vec2( -0.38277543, 0.27676845 ), 
    vec2( 0.97484398, 0.75648379 ), 
    vec2( 0.44323325, -0.97511554 ), 
    vec2( 0.53742981, -0.47373420 ), 
    vec2( -0.26496911, -0.41893023 ), 
    vec2( 0.79197514, 0.19090188 ), 
    vec2( -0.24188840, 0.99706507 ), 
    vec2( -0.81409955, 0.91437590 ), 
    vec2( 0.19984126, 0.78641367 ), 
    vec2( 0.14383161, -0.14100790 )
);

float hash(vec2 input)
{
    return fract(1.0e4 * sin(17.0 * input.x + 0.1 * input.y) * (0.1 + abs(sin(13.0 * input.y + input.x))));
}

float hash3D(vec3 input)
{
    return hash(vec2(hash(input.xy), input.z));
}

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
    vec2 uv = _TexCoords.xy * _TilingOffset.xy + _TilingOffset.zw;
    vec4 baseMapColor = texture(_BaseMap, uv);
    vec3 normapMapVec = vec3(texture(_NormalMap, uv).xy, 0.0);
    normapMapVec = (normapMapVec * 2.0 - 1.0) * _NormalScale;
    normapMapVec.z = sqrt(1.0 - normapMapVec.x * normapMapVec.x - normapMapVec.y * normapMapVec.y);
    
    vec3 normalWS = normalize(_TBN * normapMapVec);

    vec3 normalVS = (MatrixView * vec4(normalWS, 0.0)).xyz * 0.5 + 0.5;
    vec3 matcap = texture(_MatCapMap, normalVS.xy).xyz;

    vec3 mainLightDir = normalize(_MainLightDir);
    float NdotL = dot(normalWS, mainLightDir);

    vec3 viewDiffPosWS = _ViewPosWS - _PositionWS;
    vec3 viewDir = normalize(viewDiffPosWS);
    vec3 reflectDir = reflect(-mainLightDir, normalWS);

    float diffuse = OrenNayar(mainLightDir, viewDir, normalWS, 0.5);

    float specular = pow(max(dot(viewDir, reflectDir), 0.0), 1.0 + 100.0 * _SpecularColor.w);

    vec3 shadowMapProjCoords = _PositionLS.xyz / _PositionLS.w;
    shadowMapProjCoords = shadowMapProjCoords * 0.5 + 0.5;

    float bias = max(0.005 * (1.0 - NdotL), 0.0005);
    float mainLightDepth = shadowMapProjCoords.z - bias;

    float hashed = hash3D(_PositionWS);

    float shadow = 0;
    if(shadowMapProjCoords.x < 0.0 || shadowMapProjCoords.x > 1.0 ||
       shadowMapProjCoords.y < 0.0 || shadowMapProjCoords.y > 1.0 ||
       shadowMapProjCoords.z < 0.0 || shadowMapProjCoords.z > 1.0)
    {
        shadow = 1.0;
    }
    else
    {
        for (int i = 0; i < 16; i++)
        {
            float angle = 2.0 * PI * hashed;
            float s = sin(angle);
            float c = cos(angle);
            vec2 rotatedOffset = vec2(
                poissonDisk[i].x * c + poissonDisk[i].y * s,
                poissonDisk[i].x * -s + poissonDisk[i].y * c);
            shadow += texture(_ShadowMap,
                vec3(shadowMapProjCoords.xy +
                ShadowMapTexelSize * rotatedOffset * 4.0, mainLightDepth));
        }
        shadow *= 0.0625;
        float shadowFadeOut = smoothstep(_ShadowRange - 1.0, _ShadowRange, length(viewDiffPosWS));
        shadow = mix(shadow, 1.0, shadowFadeOut);
    }
    
    float alpha = min(baseMapColor.w * 1.004, 1.0) * _BaseColor.w;
    if(_AlphaHashed)
    {
        if(alpha < hashed)
        {
            discard;
        }
    }
    else if(_AlphaDither)
    {
        ivec2 screenPos = ivec2(((_PositionCS.xy / _PositionCS.w) * 0.5 + 0.5) * _ScreenSize + ivec2(_Jitter * 16));
        int index = screenPos.x % 8 * 8 + screenPos.y % 8;
        if(alpha < DitherThresholds[index])
        {
            discard;
        }
    }

    vec3 emissiveMapColor = texture(_EmissiveMap, uv).xyz;
    
    vec3 finalColor = (matcap * _MatCapColor + diffuse * shadow) *
                    baseMapColor.xyz * _BaseColor.xyz +
                    specular * _SpecularColor.xyz * shadow +
                    emissiveMapColor * _EmissiveColor +
                    _SelectedColor;

    FragColor = vec4(finalColor, alpha);

    NormalMap = vec4(normalWS, alpha);

    vec2 screenPos = _PositionCS.xy / _PositionCS.w;
    vec2 prevScreenPos = _PrevPosCS.xy / _PrevPosCS.w;

    MotionVectors = vec4((screenPos - prevScreenPos) * 0.5, 0.0, alpha);
}