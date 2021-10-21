using Unity.Mathematics;
using System.Collections.Generic;

namespace ModelTools.Animation
{
    public class Bone
    {
        public string name;
        public float4x4 offset;
        public float4x4 matrix;
        public Bone parent;
        public Bone[] children;

        public float4x4 CalculateObjectSpaceMatrix()
        {
            var bone = this;
            var m = matrix;
            while (bone.parent != null)
            {
                m = math.mul(bone.parent.matrix, m);
                bone = bone.parent;
            }
            return math.mul(float4x4.Scale(((BoneRoot)bone).UnitScaleFactor), m);
        }

        public float4x4 CalculateObjectSpaceMatrix(Dictionary<Bone, float4x4> matrices)
        {
            var bone = this;
            float4x4 m;
            if (matrices.TryGetValue(bone, out var matrix))
            {
                m = matrix;
            }
            else
            {
                m = this.matrix;
            }
            while (bone.parent != null)
            {
                if (matrices.TryGetValue(bone.parent, out var parentMatrix))
                {
                    m = math.mul(parentMatrix, m);
                }
                else
                {
                    m = math.mul(bone.parent.matrix, m);
                }
                bone = bone.parent;
            }
            return math.mul(float4x4.Scale(((BoneRoot)bone).UnitScaleFactor), m);
        }
    }
}
