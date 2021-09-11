using SiestaFrame.Entity;
using System.Numerics;

namespace SiestaFrame.Rendering
{
    public class Camera : Transform
    {
        public Vector3 Target { get; set; }
        public Vector3 Direction { get; set; }
        public Vector3 Right { get; set; }
        public Vector3 Up { get; set; }
    }
}
