namespace MBaske
{
    public static class Layers
    {
        public const int AGENT = 8;
        public const int OBSTACLE = 9;
        public const int ROAD = 10;
        public const int TERRAIN = 11;
        public const int RAY_TARGET = 12;
    }

    public static class LayerMasks
    {
        public const int AGENT = 1 << 8;
        public const int OBSTACLE = 1 << 9;
        public const int ROAD = 1 << 10;
        public const int TERRAIN = 1 << 11;
        public const int RAY_TARGET = 1 << 12;

        public const int GROUND = ROAD | TERRAIN;
        public const int DETECTABLE = OBSTACLE | RAY_TARGET;
    }
}