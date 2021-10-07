using FMOD;
using System;

namespace SiestaFrame.Audio
{
    public class Engine : IDisposable
    {
        public static Engine Instance { get; private set; }

        FMOD.System system;

        ChannelGroup main;
        public ref ChannelGroup Main { get => ref main; }
        ChannelGroup se;
        public ref ChannelGroup SE { get => ref se; }
        ChannelGroup voice;
        public ref ChannelGroup Voice { get => ref voice; }
        ChannelGroup env;
        public ref ChannelGroup Env { get => ref env; }
        ChannelGroup bgm;
        public ref ChannelGroup BGM { get => ref bgm; }

        public Engine()
        {
            Factory.System_Create(out system);
            system.getVersion(out var ver);
            system.getDSPBufferSize(out var bufferlength, out var numbuffers);
            Console.WriteLine($"FMOD Version:{ver}");
            Console.WriteLine($"bufferlength:{bufferlength}, numbuffers:{numbuffers}");
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
            system.playSound(sound, SE, false, out var channel);
        }

        public void Dispose()
        {
            Main.release();
            SE.release();
            Voice.release();
            Env.release();
            BGM.release();
            system.release();
        }
    }
}
