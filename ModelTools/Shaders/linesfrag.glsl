#version 330 core

flat in vec3 _OriginPositionVS;
in vec3 _PositionVS;

uniform mat4 MatrixView;
uniform vec4 _BaseColor;

out vec4 FragColor;

void main()
{
    vec3 dir = normalize(_PositionVS - _OriginPositionVS);

    float theta = abs(dot(dir, vec3(0.0, 0.0, 1.0)));
    theta = max(0.0, sqrt(1.0 - theta * theta));

    float fadeout = 1.0 - smoothstep(50.0, 100.0, _PositionVS.z);

    FragColor = vec4(_BaseColor.xyz, theta * fadeout * _BaseColor.w);
}