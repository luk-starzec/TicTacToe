using System;
using System.Collections.Generic;
using System.Linq;

namespace TicTacToe.Models
{
    public class GameState
    {
        public Guid PlayerX { get; init; }
        public Guid PlayerO { get; init; }
        public Guid ActivePlayer { get; set; }
        public decimal TimeLeftX { get; set; }
        public decimal TimeLeftO { get; set; }
        public IEnumerable<string> Fields { get; init; }
        public IEnumerable<GameLog> Logs { get; init; }
        public EnumPlayerType FirstPlayer { get; set; }
        public EnumGameStage GameStage { get; set; }
        public GameResult GameResult { get; set; }

        public GameState()
        {
            Fields = new string[9];
            Logs = new List<GameLog>();
            GameStage = EnumGameStage.Preparing;
            GameResult = new GameResult();
        }


        public void AddLog(string message) => ((List<GameLog>)Logs).Add(new(message));

        public void SetField(int field, string value) => ((IList<string>)Fields)[field] = value;

        public EnumPlayerType? GetPlayerType(Guid playerId)
        {
            if (playerId == PlayerX)
                return EnumPlayerType.X;
            if (playerId == PlayerO)
                return EnumPlayerType.O;
            return null;
        }
    }
}
