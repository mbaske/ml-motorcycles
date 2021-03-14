namespace MBaske.Motorcycles
{
    public static class Layers
    {
        public const int Terrain = 6;
        public const int Road = 7;
        public const int Agent = 8;
        public const int Obstacle = 9;
    }

    public static class LayerMasks
    {
        public const int Terrain = 1 << 6;
        public const int Road = 1 << 7;
        public const int Agent = 1 << 8;
        public const int Obstacle = 1 << 9;

        public const int Ground = Road | Terrain;
    }
}