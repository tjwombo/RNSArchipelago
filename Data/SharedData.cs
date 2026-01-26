namespace RnSArchipelago.Data
{
    internal class SharedData
    {
        public readonly DataContext connection = new();
        public readonly DataContext options = new();
        public readonly DataContext idToItem = new();
    }
}
