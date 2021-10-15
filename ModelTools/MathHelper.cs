using System;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace ModelTools
{
    public static class MathHelper
    {
        public const float Deg2Rad = MathF.PI / 180.0f;
        public const float Rad2Deg = 180.0f / MathF.PI;

        static Random random = new Random(233);
        public static ref Random Random => ref random;

        public enum MatrixOrder
        {
            Column,
            Row
        }

        public static float4x4 ToFloat4x4(System.Numerics.Matrix4x4 m, MatrixOrder order = MatrixOrder.Column)
        {
            switch (order)
            {
                default:
                case MatrixOrder.Column:
                    return new float4x4(
                        m.M11, m.M21, m.M31, m.M41,
                        m.M12, m.M22, m.M32, m.M42,
                        m.M13, m.M23, m.M33, m.M43,
                        m.M14, m.M24, m.M34, m.M44
                    );
                case MatrixOrder.Row:
                    return new float4x4(
                        m.M11, m.M12, m.M13, m.M14,
                        m.M21, m.M22, m.M23, m.M24,
                        m.M31, m.M32, m.M33, m.M34,
                        m.M41, m.M42, m.M43, m.M44
                    );
            }
        }

        public static float4x4 ToFloat4x4(Assimp.Matrix4x4 m, MatrixOrder order = MatrixOrder.Column)
        {
            switch (order)
            {
                default:
                case MatrixOrder.Column:
                    return new float4x4(
                        m.A1, m.B1, m.C1, m.D1,
                        m.A2, m.B2, m.C2, m.D2,
                        m.A3, m.B3, m.C3, m.D3,
                        m.A4, m.B4, m.C4, m.D4
                    );
                case MatrixOrder.Row:
                    return new float4x4(
                        m.A1, m.A2, m.A3, m.A4,
                        m.B1, m.B2, m.B3, m.B4,
                        m.C1, m.C2, m.C3, m.C4,
                        m.D1, m.D2, m.D3, m.D4
                    );
            }
        }

        public static System.Numerics.Matrix4x4 ToMatrix4x4(float4x4 m, MatrixOrder order = MatrixOrder.Column)
        {
            switch (order)
            {
                default:
                case MatrixOrder.Column:
                    return new System.Numerics.Matrix4x4(
                        m.c0.x, m.c0.y, m.c0.z, m.c0.w,
                        m.c1.x, m.c1.y, m.c1.z, m.c1.w,
                        m.c2.x, m.c2.y, m.c2.z, m.c2.w,
                        m.c3.x, m.c3.y, m.c3.z, m.c3.w
                    );
                case MatrixOrder.Row:
                    return new System.Numerics.Matrix4x4(
                        m.c0.x, m.c1.x, m.c2.x, m.c3.x,
                        m.c0.y, m.c1.y, m.c2.y, m.c3.y,
                        m.c0.z, m.c1.z, m.c2.z, m.c3.z,
                        m.c0.w, m.c1.w, m.c2.w, m.c3.w
                    );
            }
        }

        public static float2 ToFloat2(System.Numerics.Vector2 vec)
        {
            return new float2(vec.X, vec.Y);
        }

        public static float3 ToFloat3(System.Numerics.Vector3 vec)
        {
            return new float3(vec.X, vec.Y, vec.Z);
        }

        public static float3 ToFloat3(Assimp.Vector3D vec)
        {
            return new float3(vec.X, vec.Y, vec.Z);
        }

        public static float4 ToFloat4(System.Numerics.Vector4 vec)
        {
            return new float4(vec.X, vec.Y, vec.Z, vec.W);
        }

        public static float4 ToFloat4(Assimp.Color4D color)
        {
            return new float4(color.R, color.G, color.B, color.A);
        }

        public static quaternion FromEuler(float x, float y, float z, math.RotationOrder order = math.RotationOrder.Default)
        {
            return quaternion.Euler(x, y, z, order);
        }

        public static quaternion FromEuler(float3 xyz, math.RotationOrder order = math.RotationOrder.Default)
        {
            return quaternion.Euler(xyz, order);
        }

        public static float3 ToEuler(quaternion q, math.RotationOrder order = math.RotationOrder.Default)
        {
            return toEuler(q, order);
        }

        #region toEuler
        static float3 toEuler(quaternion q, math.RotationOrder order)
        {
            const float epsilon = 1e-6f;

            //prepare the data
            var qv = q.value;
            var d1 = qv * qv.wwww * new float4(2.0f); //xw, yw, zw, ww
            var d2 = qv * qv.yzxw * new float4(2.0f); //xy, yz, zx, ww
            var d3 = qv * qv;
            var euler = new float3(0.0f);

            const float CUTOFF = (1.0f - 2.0f * epsilon) * (1.0f - 2.0f * epsilon);

            switch (order)
            {
                case math.RotationOrder.ZYX:
                    {
                        var y1 = d2.z + d1.y;
                        if (y1 * y1 < CUTOFF)
                        {
                            var x1 = -d2.x + d1.z;
                            var x2 = d3.x + d3.w - d3.y - d3.z;
                            var z1 = -d2.y + d1.x;
                            var z2 = d3.z + d3.w - d3.y - d3.x;
                            euler = new float3(math.atan2(x1, x2), math.asin(y1), math.atan2(z1, z2));
                        }
                        else //zxz
                        {
                            y1 = math.clamp(y1, -1.0f, 1.0f);
                            var abcd = new float4(d2.z, d1.y, d2.y, d1.x);
                            var x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                            var x2 = math.csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
                            euler = new float3(math.atan2(x1, x2), math.asin(y1), 0.0f);
                        }

                        break;
                    }

                case math.RotationOrder.ZXY:
                    {
                        var y1 = d2.y - d1.x;
                        if (y1 * y1 < CUTOFF)
                        {
                            var x1 = d2.x + d1.z;
                            var x2 = d3.y + d3.w - d3.x - d3.z;
                            var z1 = d2.z + d1.y;
                            var z2 = d3.z + d3.w - d3.x - d3.y;
                            euler = new float3(math.atan2(x1, x2), -math.asin(y1), math.atan2(z1, z2));
                        }
                        else //zxz
                        {
                            y1 = math.clamp(y1, -1.0f, 1.0f);
                            var abcd = new float4(d2.z, d1.y, d2.y, d1.x);
                            var x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                            var x2 = math.csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
                            euler = new float3(math.atan2(x1, x2), -math.asin(y1), 0.0f);
                        }

                        break;
                    }

                case math.RotationOrder.YXZ:
                    {
                        var y1 = d2.y + d1.x;
                        if (y1 * y1 < CUTOFF)
                        {
                            var x1 = -d2.z + d1.y;
                            var x2 = d3.z + d3.w - d3.x - d3.y;
                            var z1 = -d2.x + d1.z;
                            var z2 = d3.y + d3.w - d3.z - d3.x;
                            euler = new float3(math.atan2(x1, x2), math.asin(y1), math.atan2(z1, z2));
                        }
                        else //yzy
                        {
                            y1 = math.clamp(y1, -1.0f, 1.0f);
                            var abcd = new float4(d2.x, d1.z, d2.y, d1.x);
                            var x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                            var x2 = math.csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
                            euler = new float3(math.atan2(x1, x2), math.asin(y1), 0.0f);
                        }

                        break;
                    }

                case math.RotationOrder.YZX:
                    {
                        var y1 = d2.x - d1.z;
                        if (y1 * y1 < CUTOFF)
                        {
                            var x1 = d2.z + d1.y;
                            var x2 = d3.x + d3.w - d3.z - d3.y;
                            var z1 = d2.y + d1.x;
                            var z2 = d3.y + d3.w - d3.x - d3.z;
                            euler = new float3(math.atan2(x1, x2), -math.asin(y1), math.atan2(z1, z2));
                        }
                        else //yxy
                        {
                            y1 = math.clamp(y1, -1.0f, 1.0f);
                            var abcd = new float4(d2.x, d1.z, d2.y, d1.x);
                            var x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                            var x2 = math.csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
                            euler = new float3(math.atan2(x1, x2), -math.asin(y1), 0.0f);
                        }

                        break;
                    }

                case math.RotationOrder.XZY:
                    {
                        var y1 = d2.x + d1.z;
                        if (y1 * y1 < CUTOFF)
                        {
                            var x1 = -d2.y + d1.x;
                            var x2 = d3.y + d3.w - d3.z - d3.x;
                            var z1 = -d2.z + d1.y;
                            var z2 = d3.x + d3.w - d3.y - d3.z;
                            euler = new float3(math.atan2(x1, x2), math.asin(y1), math.atan2(z1, z2));
                        }
                        else //xyx
                        {
                            y1 = math.clamp(y1, -1.0f, 1.0f);
                            var abcd = new float4(d2.x, d1.z, d2.z, d1.y);
                            var x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                            var x2 = math.csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
                            euler = new float3(math.atan2(x1, x2), math.asin(y1), 0.0f);
                        }

                        break;
                    }

                case math.RotationOrder.XYZ:
                    {
                        var y1 = d2.z - d1.y;
                        if (y1 * y1 < CUTOFF)
                        {
                            var x1 = d2.y + d1.x;
                            var x2 = d3.z + d3.w - d3.y - d3.x;
                            var z1 = d2.x + d1.z;
                            var z2 = d3.x + d3.w - d3.y - d3.z;
                            euler = new float3(math.atan2(x1, x2), -math.asin(y1), math.atan2(z1, z2));
                        }
                        else //xzx
                        {
                            y1 = math.clamp(y1, -1.0f, 1.0f);
                            var abcd = new float4(d2.z, d1.y, d2.x, d1.z);
                            var x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                            var x2 = math.csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
                            euler = new float3(math.atan2(x1, x2), -math.asin(y1), 0.0f);
                        }

                        break;
                    }
            }

            return eulerReorderBack(euler, order);
        }

        static float3 eulerReorderBack(float3 euler, math.RotationOrder order)
        {
            switch (order)
            {
                case math.RotationOrder.XZY:
                    return euler.xzy;
                case math.RotationOrder.YZX:
                    return euler.zxy;
                case math.RotationOrder.YXZ:
                    return euler.yxz;
                case math.RotationOrder.ZXY:
                    return euler.yzx;
                case math.RotationOrder.ZYX:
                    return euler.zyx;
                case math.RotationOrder.XYZ:
                default:
                    return euler;
            }
        }
        #endregion

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
            quaternion eulerRot = FromEuler(eulers);
            //return math.mul(rotation, eulerRot);
            return math.mul(rotation, math.mul(math.mul(math.inverse(rotation), eulerRot), rotation));
        }

        #region FromToRotation
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
        #endregion

        public static float4x4 TRS(float3 translation, quaternion rotation, float3 scale)
        {
            float3x3 r = new float3x3(rotation);
            return new float4x4(new float4(r.c0 * -scale.x, 0.0f),
                                new float4(r.c1 * scale.y, 0.0f),
                                new float4(r.c2 * scale.z, 0.0f),
                                new float4(translation, 1.0f));
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

        public static float4x4 PerspectiveFov(float fov, uint width, uint height, float near, float far)
        {
            float rad = fov;
            float h = math.cos(0.5f * rad) / math.sin(0.5f * rad);
            float w = h * height / width;

            var Result = float4x4.zero;
            Result[0][0] = w;
            Result[1][1] = h;
            Result[2][2] = (far + near) / (far - near);
            Result[2][3] = 1f;
            Result[3][2] = -(2f * far * near) / (far - near);
            return Result;
        }

        public static float4x4 Ortho(float left, float right, float bottom, float top, float zNear, float zFar)
        {
            var Result = float4x4.identity;
            Result[0][0] = 2f / (right - left);
            Result[1][1] = 2f / (top - bottom);
            Result[2][2] = 2f / (zFar - zNear);
            Result[3][0] = -(right + left) / (right - left);
            Result[3][1] = -(top + bottom) / (top - bottom);
            Result[3][2] = -(zFar + zNear) / (zFar - zNear);
            return Result;
        }

        public static float4x4 RemoveTranslation(float4x4 m)
        {
            return new float4x4(
                new float4(-m.c0.xyz, 0.0f),
                new float4(m.c1.xyz, 0.0f),
                new float4(m.c2.xyz, 0.0f),
                new float4(new float3(0), 1.0f));
        }
    }
}
