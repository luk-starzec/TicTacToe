namespace TicTacToe.Models
{
    public class GameResult
    {
        public EnumGameResult Result { get; set; }
        public WinType WinType { get; set; }

        public GameResult()
        {
            Result = EnumGameResult.None;
        }
    }
}
