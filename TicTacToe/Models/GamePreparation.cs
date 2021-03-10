using System;

namespace TicTacToe.Models
{
    public class GamePreparation
    {
        public Guid GameId { get; set; }
        public string OpponentName { get; set; }
        public Guid OpponentId { get; set; }
    }
}
