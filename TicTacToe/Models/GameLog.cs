using System;

namespace TicTacToe.Models
{
    public class GameLog
    {
        public DateTime Time { get; set; }
        public string Message { get; set; }

        public GameLog(string message)
        {
            Time = DateTime.Now;
            Message = message;
        }

        public override string ToString()
        {
            return $"[{Time.ToShortTimeString()}]: {Message}";
        }
    }
}
