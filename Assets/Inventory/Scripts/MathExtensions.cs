using System.Text;

namespace Inventories
{
    public static class MathExtensions
    {
        public static bool IsInRange(this int number, int min, int max)
        {
            return number >= min && number <= max;
        }

        public static string GridToString(this Item[,] grid)
        {
            var sb = new StringBuilder();
            var lastY = 0;

            for (int y = 0; y < grid.GetLength(1); y++)
            {
                for (int x = 0; x < grid.GetLength(0); x++)
                {
                    if (lastY != y)
                    {
                        sb.AppendLine();
                        lastY = y;
                    }

                    sb.Append($"[{grid[x, y]}]");
                }
            }

            return sb.ToString();
        }
    }
}