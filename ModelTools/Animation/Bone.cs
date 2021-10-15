using Unity.Mathematics;

namespace ModelTools.Animation
{
    public class Bone
    {
        public string name;
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
    }
}
