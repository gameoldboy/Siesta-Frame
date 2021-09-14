using System;
using Unity.Mathematics;

namespace SiestaFrame
{
    public static class MathHelper
    {
        public const float PiOver2 = MathF.PI / 2;

        public const float Deg2Rad = MathF.PI / 180.0f;

        public const float Rad2Deg = 180.0f / MathF.PI;

        public static float4x4 ToFloat4x4(System.Numerics.Matrix4x4 m)
        {
            return new float4x4(
                m.M11, m.M21, m.M31, m.M41,
                m.M12, m.M22, m.M32, m.M42,
                m.M13, m.M23, m.M33, m.M43,
                m.M14, m.M24, m.M34, m.M44
            );
        }

        public static float3 ToFloat3(System.Numerics.Vector3 vec)
        {
            return new float3(vec.X, vec.Y, vec.Z);
        }

        public static float4 ToFloat4(System.Numerics.Vector4 vec)
        {
            return new float4(vec.X, vec.Y, vec.Z, vec.W);
        }

        public static quaternion FromEulerAngles(float x, float y, float z)
        {
            return quaternion.Euler(x, y, z);
        }

        public static quaternion FromEulerAngles(float3 xyz)
        {
            return quaternion.Euler(xyz);
        }

        public static float3 ToEulerAngles(quaternion q)
        {
            const float SINGULARITY_THRESHOLD = 0.4999995f;

            var sqw = q.value.w * q.value.w;
            var sqx = q.value.x * q.value.x;
            var sqy = q.value.y * q.value.y;
            var sqz = q.value.z * q.value.z;
            var unit = sqx + sqy + sqz + sqw;
            var singularityTest = (q.value.x * q.value.z) + (q.value.w * q.value.y);

            float3 eulerAngles = float3.zero;
            if (singularityTest > SINGULARITY_THRESHOLD * unit)
            {
                eulerAngles.z = (float)(2 * Math.Atan2(q.value.x, q.value.w));
                eulerAngles.y = PiOver2;
                eulerAngles.x = 0;
            }
            else if (singularityTest < -SINGULARITY_THRESHOLD * unit)
            {
                eulerAngles.z = (float)(-2 * Math.Atan2(q.value.x, q.value.w));
                eulerAngles.y = -PiOver2;
                eulerAngles.x = 0;
            }
            else
            {
                eulerAngles.z = MathF.Atan2(2 * ((q.value.w * q.value.z) - (q.value.x * q.value.y)), sqw + sqx - sqy - sqz);
                eulerAngles.y = MathF.Asin(2 * singularityTest / unit);
                eulerAngles.x = MathF.Atan2(2 * ((q.value.w * q.value.x) - (q.value.y * q.value.z)), sqw - sqx - sqy + sqz);
            }
            return eulerAngles;
        }

        public static quaternion LookRotation(float x, float y, float z)
        {
            return LookRotation(new float3(x, y, z));
        }

        public static quaternion LookRotation(float3 forward)
        {
            return LookRotation(forward, math.up());
        }

        public static quaternion LookRotation(float3 forward, float3 up)
        {
            return quaternion.LookRotationSafe(forward, up);
        }

        public static quaternion Rotate(float3 eulers, quaternion rotation)
        {
            quaternion eulerRot = FromEulerAngles(eulers);
            //return math.mul(rotation, eulerRot);
            return math.mul(rotation, math.mul(math.mul(math.inverse(rotation), eulerRot), rotation));
        }

        public static quaternion FromToRotation(float3 from, float3 to)
        {
            float3 cross = math.cross(from, to);
            CalculatePerpendicularNormalized(from, out float3 safeAxis, out float3 unused); // for when angle ~= 180
            float dot = math.dot(from, to);
            float3 squares = new float3(0.5f - new float2(dot, -dot) * 0.5f, math.lengthsq(cross));
            float3 inverses = math.select(math.rsqrt(squares), 0.0f, squares < 1e-10f);
            float2 sinCosHalfAngle = squares.xy * inverses.xy;
            float3 axis = math.select(cross * inverses.z, safeAxis, squares.z < 1e-10f);
            return new quaternion(new float4(axis * sinCosHalfAngle.x, sinCosHalfAngle.y));
        }

        public static void CalculatePerpendicularNormalized(float3 v, out float3 p, out float3 q)
        {
            float3 vSquared = v * v;
            float3 lengthsSquared = vSquared + vSquared.xxx; // y = ||j x v||^2, z = ||k x v||^2
            float3 invLengths = math.rsqrt(lengthsSquared);

            // select first direction, j x v or k x v, whichever has greater magnitude
            float3 dir0 = new float3(-v.y, v.x, 0.0f);
            float3 dir1 = new float3(-v.z, 0.0f, v.x);
            bool cmp = (lengthsSquared.y > lengthsSquared.z);
            float3 dir = math.select(dir1, dir0, cmp);

            // normalize and get the other direction
            float invLength = math.select(invLengths.z, invLengths.y, cmp);
            p = dir * invLength;
            float3 cross = math.cross(v, dir);
            q = cross * invLength;
        }

        public static float4x4 LookAt(float3 pos, float3 target, float3 up)
        {
            var f = math.normalize(target - pos);
            var s = math.normalize(math.cross(up, f));
            var u = math.cross(f, s);

            var Result = float4x4.identity;
            Result[0][0] = s.x;
            Result[1][0] = s.y;
            Result[2][0] = s.z;
            Result[0][1] = u.x;
            Result[1][1] = u.y;
            Result[2][1] = u.z;
            Result[0][2] = f.x;
            Result[1][2] = f.y;
            Result[2][2] = f.z;
            Result[3][0] = -math.dot(s, pos);
            Result[3][1] = -math.dot(u, pos);
            Result[3][2] = -math.dot(f, pos);
            return Result;
        }

        public static float4x4 PerspectiveFov(float fov, float aspect, float near, float far)
        {
            float tanHalfFovy = math.tan(fov / 2f);

            var Result = float4x4.zero;
            Result[0][0] = 1f / (aspect * tanHalfFovy);
            Result[1][1] = 1f / (tanHalfFovy);
            Result[2][2] = (far + near) / (far - near);
            Result[2][3] = 1f;
            Result[3][2] = -(2f * far * near) / (far - near);
            return Result;
        }
    }
}
