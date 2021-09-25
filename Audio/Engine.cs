using FMOD;
using System;
using Debug = System.Diagnostics.Debug;

namespace SiestaFrame.Audio
{
    public class Engine
    {
        public static Engine Instance { get; private set; }

        FMOD.System system;

        ChannelGroup main;
        public ChannelGroup Main { get => main; }
        ChannelGroup se;
        public ChannelGroup SE { get => se; }
        ChannelGroup voice;
        public ChannelGroup Voice { get => voice; }
        ChannelGroup env;
        public ChannelGroup Env { get => env; }
        ChannelGroup bgm;
        public ChannelGroup BGM { get => bgm; }

        public Engine()
        {
            Factory.System_Create(out system);
            system.getVersion(out var ver);
            system.getDSPBufferSize(out var bufferlength, out var numbuffers);
            Debug.WriteLine($"FMOD Version:{ver}");
            Debug.WriteLine($"bufferlength:{bufferlength}, numbuffers:{numbuffers}");
            system.init(512, INITFLAGS.NORMAL, (IntPtr)0);
            system.createChannelGroup("Default", out main);
            system.createChannelGroup("SE", out se);
            system.createChannelGroup("Voice", out voice);
            system.createChannelGroup("Env", out env);
            system.createChannelGroup("BGM", out bgm);

            Instance = this;
        }

        public void PlaySound(string path)
        {
            system.createSound(path, MODE._2D | MODE.CREATESTREAM, out var sound);
            system.playSound(sound, se, false, out var channel);
        }
    }
}
