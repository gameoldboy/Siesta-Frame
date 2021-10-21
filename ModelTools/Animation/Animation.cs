using Unity.Mathematics;

namespace ModelTools.Animation
{
    public struct Track
    {
        public Bone bone;
        public PositionKey[] positionKeys;
        public RotationKey[] rotationKeys;
        public ScalingKey[] scalingKeys;
    }

    public struct PositionKey
    {
        public double time;
        public float3 position;
    }

    public struct RotationKey
    {
        public double time;
        public quaternion rotation;
    }

    public struct ScalingKey
    {
        public double time;
        public float3 scale;
    }

    public class Animation
    {
        public string Name { get; private set; }
        public double FramesPerSecond { get; private set; }
        public double FramesCount { get; private set; }
        public double Duration => FramesCount / FramesPerSecond;
        public Track[] Tracks { get; private set; }

        public Animation(string name, double fps, double frameCount, int keysCount)
        {
            Name = name;
            FramesPerSecond = fps;
            FramesCount = frameCount;
            Tracks = new Track[keysCount];
        }

        public void AdjustFramesPerSecond(double fps)
        {
            var adjustScale = fps / FramesPerSecond;
            for (int i = 0; i < Tracks.Length; i++)
            {
                for (int j = 0; j < Tracks[i].positionKeys.Length; j++)
                {
                    Tracks[i].positionKeys[j].time *= adjustScale;
                }
                for (int j = 0; j < Tracks[i].rotationKeys.Length; j++)
                {
                    Tracks[i].rotationKeys[j].time *= adjustScale;
                }
                for (int j = 0; j < Tracks[i].scalingKeys.Length; j++)
                {
                    Tracks[i].scalingKeys[j].time *= adjustScale;
                }
            }
            FramesCount *= adjustScale;
            FramesPerSecond = fps;
        }

        public void AlignAllKeysByInteger()
        {
            for (int i = 0; i < Tracks.Length; i++)
            {
                for (int j = 0; j < Tracks[i].positionKeys.Length; j++)
                {
                    Tracks[i].positionKeys[j].time = math.round(Tracks[i].positionKeys[j].time);
                }
                for (int j = 0; j < Tracks[i].rotationKeys.Length; j++)
                {
                    Tracks[i].rotationKeys[j].time = math.round(Tracks[i].rotationKeys[j].time);
                }
                for (int j = 0; j < Tracks[i].scalingKeys.Length; j++)
                {
                    Tracks[i].scalingKeys[j].time = math.round(Tracks[i].scalingKeys[j].time);
                }
            }
            FramesCount = math.round(FramesCount);
            FramesPerSecond = math.round(FramesPerSecond);
        }
    }
}
