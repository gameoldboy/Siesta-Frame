using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace ModelTools.Animation
{
    public class TimeLine
    {
        class keyframeindex
        {
            public int pos = 0;
            public int rot = 0;
            public int sc = 0;
        }

        double time;
        public double CurrentFrames => time;
        public double Position => time / Animation.FramesPerSecond;
        public float Speed { get; set; }

        public event Action OnFinished;

        public Animation Animation { get; }

        keyframeindex[] indices;

        public Dictionary<Bone, float4x4> Results { get; }

        public TimeLine(Animation animation)
        {
            time = 0;
            Speed = 1f;
            Animation = animation;
            indices = new keyframeindex[animation.Tracks.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = new keyframeindex();
            }
            Results = new Dictionary<Bone, float4x4>();
        }

        public void Update(double deltaTime)
        {
            time += deltaTime * Speed * Animation.FramesPerSecond;
            update();
        }

        public void Update(float deltaTime)
        {
            time += deltaTime * Speed * Animation.FramesPerSecond;
            update();
        }

        public void Seek(double pos)
        {
            time = pos * Animation.FramesPerSecond;
            update();
        }

        public void Seek(float pos)
        {
            time = pos * Animation.FramesPerSecond;
            update();
        }

        public void SeekFrames(double frames)
        {
            time = frames;
            update();
        }

        public void SeekFrames(float frames)
        {
            time = frames;
            update();
        }

        public void SeekFrames(int frames)
        {
            time = frames;
            update();
        }

        void update()
        {
            // 倒放
            while (time < 0)
            {
                time += Animation.FramesCount;
                OnFinished?.Invoke();
            }
            // 正放
            while (time >= Animation.FramesCount)
            {
                time -= Animation.FramesCount;
                OnFinished?.Invoke();
            }

            for (int i = 0; i < Animation.Tracks.Length; i++)
            {
                var track = Animation.Tracks[i];

                while (indices[i].pos >= 0 && indices[i].pos < track.positionKeys.Length)
                {
                    var cur = track.positionKeys[indices[i].pos].time;
                    double next;
                    if (indices[i].pos + 1 < track.positionKeys.Length)
                    {
                        next = track.positionKeys[indices[i].pos + 1].time;
                    }
                    else
                    {
                        next = cur;
                    }
                    if (time < cur)
                    {
                        indices[i].pos--;
                        if (indices[i].pos < 0)
                        {
                            indices[i].pos = 0;
                            break;
                        }
                    }
                    else if (time >= next)
                    {
                        indices[i].pos++;
                        if (indices[i].pos > track.positionKeys.Length - 1)
                        {
                            indices[i].pos = track.positionKeys.Length - 1;
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                while (indices[i].rot >= 0 && indices[i].rot < track.rotationKeys.Length)
                {
                    var cur = track.rotationKeys[indices[i].rot].time;
                    double next;
                    if (indices[i].rot + 1 < track.rotationKeys.Length)
                    {
                        next = track.rotationKeys[indices[i].rot + 1].time;
                    }
                    else
                    {
                        next = cur;
                    }
                    if (time < cur)
                    {
                        indices[i].rot--;
                        if (indices[i].rot < 0)
                        {
                            indices[i].rot = 0;
                            break;
                        }
                    }
                    else if (time >= next)
                    {
                        indices[i].rot++;
                        if (indices[i].rot > track.rotationKeys.Length - 1)
                        {
                            indices[i].rot = track.rotationKeys.Length - 1;
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                while (indices[i].sc >= 0 && indices[i].sc < track.scalingKeys.Length)
                {
                    var cur = track.scalingKeys[indices[i].sc].time;
                    double next;
                    if (indices[i].sc + 1 < track.scalingKeys.Length)
                    {
                        next = track.scalingKeys[indices[i].sc + 1].time;
                    }
                    else
                    {
                        next = cur;
                    }
                    if (time < cur)
                    {
                        indices[i].sc--;
                        if (indices[i].sc < 0)
                        {
                            indices[i].sc = 0;
                            break;
                        }
                    }
                    else if (time >= next)
                    {
                        indices[i].sc++;
                        if (indices[i].sc > track.scalingKeys.Length - 1)
                        {
                            indices[i].sc = track.scalingKeys.Length - 1;
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                var bone = Animation.Tracks[i].bone;
                float3 pos = bone.matrix.c3.xyz;
                float3 sc = new float3(math.length(bone.matrix.c0.xyz), math.length(bone.matrix.c1.xyz), math.length(bone.matrix.c2.xyz));
                quaternion rot = new quaternion(bone.matrix);

                if (track.positionKeys.Length > 0)
                {
                    PositionKey curPos, nextPos;
                    curPos = track.positionKeys[indices[i].pos];
                    if (indices[i].pos + 1 < track.positionKeys.Length)
                    {
                        nextPos = track.positionKeys[indices[i].pos + 1];
                        var t = (float)((time - curPos.time) / (nextPos.time - curPos.time));
                        pos = math.lerp(curPos.position, nextPos.position, t);
                    }
                    else
                    {
                        pos = curPos.position;
                    }
                }

                if (track.rotationKeys.Length > 0)
                {
                    RotationKey curRot, nextRot;
                    curRot = track.rotationKeys[indices[i].rot];
                    if (indices[i].rot + 1 < track.rotationKeys.Length)
                    {
                        nextRot = track.rotationKeys[indices[i].rot + 1];
                        var t = (float)((time - curRot.time) / (nextRot.time - curRot.time));
                        rot = math.slerp(curRot.rotation, nextRot.rotation, t);
                    }
                    else
                    {
                        rot = curRot.rotation;
                    }
                }

                if (track.scalingKeys.Length > 0)
                {
                    ScalingKey curSc, nextSc;
                    curSc = track.scalingKeys[indices[i].sc];
                    if (indices[i].sc + 1 < track.scalingKeys.Length)
                    {
                        nextSc = track.scalingKeys[indices[i].sc + 1];
                        var t = (float)((time - curSc.time) / (nextSc.time - curSc.time));
                        sc = math.lerp(curSc.scale, nextSc.scale, t);
                    }
                    else
                    {
                        sc = curSc.scale;
                    }
                }

                Results[bone] = float4x4.TRS(pos, rot, sc);
            }
        }
    }
}
