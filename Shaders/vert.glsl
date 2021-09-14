#version 330 core

layout (location = 0) in vec3 vPos;
layout (location = 1) in vec3 vNormal;
layout (location = 2) in vec4 vUv;
layout (location = 3) in vec4 vColor;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec2 uv;
out vec3 normalWS;
out vec3 normalVS;
out vec3 worldPos;

void main()
{
    gl_Position = uProjection * uView * uModel * vec4(vPos, 1.0);
    worldPos = (uModel * vec4(vPos, 1.0)).xyz;
    uv = vUv.xy;
    normalVS = transpose(inverse(mat3(uView * uModel))) * vNormal;
    normalWS = transpose(inverse(mat3(uModel))) * vNormal;
}