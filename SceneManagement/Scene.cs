﻿using SiestaFrame.Object;
using SiestaFrame.Rendering;
using System;
using System.Collections.Generic;

namespace SiestaFrame.SceneManagement
{
    public class Scene : IDisposable
    {
        public string Name { get; set; }

        public List<Entity> Entites { get; }
        public Camera MainCamera { get; set; }
        public Light MainLight { get; set; }

        public Scene(string name)
        {
            Name = name;
            Entites = new List<Entity>();
            MainCamera = new Camera();
            MainLight = new Light();
        }

        public void Render(ShadowMap shadowMap, TemporalAntiAliasing temporalAntiAliasing)
        {
            for (int i = 0; i < Entites.Count; i++)
            {
                var entity = Entites[i];
                entity.Draw(MainCamera, MainLight, shadowMap, temporalAntiAliasing);
            }
        }

        public void SyncCollision()
        {
            for (int i = 0; i < Entites.Count; i++)
            {
                var entity = Entites[i];
                entity.SyncCollision();
            }
        }

        public void Dispose()
        {
            foreach (var entity in Entites)
            {
                entity.Dispose();
            }
        }
    }
}
