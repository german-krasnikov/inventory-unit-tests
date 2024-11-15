namespace Inventories
{
    public static class MathExtensions
    {
        public static bool IsInRange(this int number, int min, int max)
        {
            return number >= min && number <= max;
        }
    }
}