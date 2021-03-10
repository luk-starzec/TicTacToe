namespace TicTacToe.Models
{
    public class WinType
    {
        public EnumWinDirection Direction { get; init; }
        public int Index { get; init; }

        public WinType(EnumWinDirection direction, int index)
            => (Direction, Index) = (direction, index);
    }
}
