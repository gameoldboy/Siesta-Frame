#version 330 core

flat in vec3 _OriginPositionWS;
in vec3 _PositionWS;

uniform mat4 MatrixView;
uniform vec4 _BaseColor;
uniform vec3 _ViewPosWS;

out vec4 FragColor;

void main()
{
    vec3 dir = normalize(_PositionWS - _OriginPositionWS);
    vec3 viewDir = normalize(_ViewPosWS - _PositionWS);

    float theta = abs(dot(dir, viewDir));
    theta = sqrt(1.0 - theta * theta);
    vec3 posVS = (MatrixView * vec4(_PositionWS, 1.0)).xyz;

    FragColor = vec4(_BaseColor.xyz, theta * (1.0 - smoothstep(50.0, 100.0, posVS.z)) * _BaseColor.w);
}