using SiestaFrame.Rendering;
using System.Collections.Generic;

namespace SiestaFrame.SceneManagement
{
    public class SceneManager
    {
        #region Static
        public static SceneManager Instance { get; private set; }

        static Dictionary<string, Texture> commonTexturePool;
        static Dictionary<string, Shader> commonShaderPool;

        static SceneManager()
        {
            commonTexturePool = new Dictionary<string, Texture>();
            commonShaderPool = new Dictionary<string, Shader>();
        }

        public static Texture AddCommonTexture(string name)
        {
            if (!commonTexturePool.ContainsKey(name))
            {
                var texture = new Texture(name);
                commonTexturePool.Add(name, texture);
                return texture;
            }
            else
            {
                return commonTexturePool[name];
            }
        }

        public static Texture GetCommonTexture(string name)
        {
            if (commonTexturePool.ContainsKey(name))
            {
                return commonTexturePool[name];
            }
            return null;
        }

        public static void AddOrUpdateCommonTexture(Texture texture, string name)
        {
            if (commonTexturePool.ContainsKey(name))
            {
                commonTexturePool[name].Dispose();
            }
            commonTexturePool[name] = texture;
        }

        public static Shader AddCommonShader(string name1, string name2)
        {
            var name = $"{name1}&{name2}";
            if (!commonShaderPool.ContainsKey(name))
            {
                var shader = new Shader(name1, name2);
                commonShaderPool.Add(name, shader);
                return shader;
            }
            else
            {
                return commonShaderPool[name];
            }
        }

        public static Shader GetCommonShader(string name1, string name2)
        {
            var name = $"{name1}&{name2}";
            if (commonShaderPool.ContainsKey(name))
            {
                return commonShaderPool[name];
            }
            return null;
        }

        public static void AddOrUpdateCommonShader(Shader shader, string name1, string name2)
        {
            var name = $"{name1}&{name2}";
            if (commonShaderPool.ContainsKey(name))
            {
                commonShaderPool[name].Dispose();
            }
            commonShaderPool[name] = shader;
        }

        public static bool UnloadCommonTexture(string name)
        {
            if (commonTexturePool.ContainsKey(name))
            {
                commonTexturePool[name].Dispose();
                commonTexturePool.Remove(name);
                return true;
            }
            return false;
        }

        public static bool UnloadCommonShader(string name1, string name2)
        {
            var name = $"{name1}&{name2}";
            if (commonShaderPool.ContainsKey(name))
            {
                commonShaderPool[name].Dispose();
                commonShaderPool.Remove(name);
                return true;
            }
            return false;
        }

        public static void UnloadCommonPool()
        {
            foreach (var name in commonTexturePool.Keys)
            {
                var texture = commonTexturePool[name];
                texture.Dispose();
            }
            commonTexturePool.Clear();
            foreach (var name in commonShaderPool.Keys)
            {
                var shader = commonShaderPool[name];
                shader.Dispose();
            }
            commonShaderPool.Clear();
        }
        #endregion

        #region Instance
        Dictionary<string, Texture> texturePool;
        Dictionary<string, Shader> shaderPool;

        public Scene CurrentScene { get; private set; }

        public SceneManager()
        {
            texturePool = new Dictionary<string, Texture>();
            shaderPool = new Dictionary<string, Shader>();

            Instance = this;
        }

        public static void LoadScene(Scene scene)
        {
            if (Instance.CurrentScene != null)
            {
                Instance.UnloadScene();
            }

            Instance.CurrentScene = scene;
        }

        void UnloadScene()
        {
            foreach (var name in texturePool.Keys)
            {
                var texture = texturePool[name];
                texture.Dispose();
            }
            texturePool.Clear();
            foreach (var name in shaderPool.Keys)
            {
                var shader = shaderPool[name];
                shader.Dispose();
            }
            shaderPool.Clear();
        }

        public Texture AddTexture(string name)
        {
            if (!texturePool.ContainsKey(name))
            {
                var texture = new Texture(name);
                texturePool.Add(name, texture);
                return texture;
            }
            else
            {
                return texturePool[name];
            }
        }

        public Texture GetTexture(string name)
        {
            if (texturePool.ContainsKey(name))
            {
                return texturePool[name];
            }
            return null;
        }

        public void AddOrUpdateTexture(Texture texture, string name)
        {
            if (texturePool.ContainsKey(name))
            {
                texturePool[name].Dispose();
            }
            texturePool[name] = texture;
        }

        public Shader AddShader(string name1, string name2)
        {
            var name = $"{name1}&{name2}";
            if (!shaderPool.ContainsKey(name))
            {
                var shader = new Shader(name1, name2);
                shaderPool.Add(name, shader);
                return shader;
            }
            else
            {
                return shaderPool[name];
            }
        }

        public Shader GetShader(string name1, string name2)
        {
            var name = $"{name1}&{name2}";
            if (shaderPool.ContainsKey(name))
            {
                return shaderPool[name];
            }
            return null;
        }

        public void AddOrUpdateShader(Shader shader, string name1, string name2)
        {
            var name = $"{name1}&{name2}";
            if (shaderPool.ContainsKey(name))
            {
                shaderPool[name].Dispose();
            }
            shaderPool[name] = shader;
        }

        public bool UnloadTexture(string name)
        {
            if (texturePool.ContainsKey(name))
            {
                texturePool[name].Dispose();
                texturePool.Remove(name);
                return true;
            }
            return false;
        }

        public bool UnloadShader(string name1, string name2)
        {
            var name = $"{name1}&{name2}";
            if (shaderPool.ContainsKey(name))
            {
                shaderPool[name].Dispose();
                shaderPool.Remove(name);
                return true;
            }
            return false;
        }
        #endregion
    }
}
