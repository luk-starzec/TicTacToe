using System;
using System.Collections.Generic;
using System.Linq;
using TicTacToe.Helpers;
using TicTacToe.Models;

namespace TicTacToe.Logic
{
    public static class GameLogic
    {
        /// <summary>
        /// (in seconds)
        /// </summary>
        public static decimal PlayerTime => 20;
        /// <summary>
        /// (in seconds)
        /// </summary>
        public static decimal TimeInterval => 1;

        public static EnumPlayerType OponentType(EnumPlayerType playerType) => (EnumPlayerType)((int)playerType * (-1));

        public static string GetGameUrl(Guid gameId) => $"/game/{GuidEncoder.Encode(gameId)}";
        public static string GetGameUrl(Guid gameId, Guid playerId) => $"/game/{GuidEncoder.Encode(gameId)}/{GuidEncoder.Encode(playerId)}";
        public static string GetGameUrl(Guid gameId, Guid playerId, EnumPlayerType playerType)
            => $"/game/{GuidEncoder.Encode(gameId)}/{GuidEncoder.Encode(playerId)}?init={playerType}";

        public static GameState CreateGameState(Guid playerId, EnumPlayerType playerType, Guid oponentId)
        {
            var playerX = playerType == EnumPlayerType.X ? playerId : oponentId;
            var playerO = playerType == EnumPlayerType.O ? playerId : oponentId;

            var state = new GameState
            {
                PlayerX = playerX,
                PlayerO = playerO,
                TimeLeftX = PlayerTime,
                TimeLeftO = PlayerTime,
                ActivePlayer = playerId,
                FirstPlayer = playerType,
            };

            state.AddLog($"Game created");

            return state;
        }

        public static GameState StartGame(GameState state)
        {
            if (state.GameStage != EnumGameStage.Preparing)
                return state;

            state.GameStage = EnumGameStage.Starting;

            var player = state.ActivePlayer == state.PlayerX ? EnumPlayerType.X : EnumPlayerType.O;
            state.AddLog($"Game started (active {player})");

            return state;
        }

        public static GameState PlayerMoved(Guid playerId, int field, GameState state)
        {
            if (state.ActivePlayer != playerId)
                throw new Exception();

            var playerType = state.GetPlayerType(playerId);
            if (!playerType.HasValue)
                return null;

            state.SetField(field, playerType.ToString());
            state.AddLog($"Player {playerType} moved [{field}]");

            state.GameResult = GetGameResult(state.Fields);

            if (state.GameResult.Result == EnumGameResult.None)
            {
                state.ActivePlayer = playerType == EnumPlayerType.X ? state.PlayerO : state.PlayerX;
                state.AddLog($"Next round");
            }
            else
            {
                state.ActivePlayer = new Guid();
                state.AddLog($"Game Over ({state.GameResult.Result.GetDescription()})");
            }

            return state;
        }
        public static GameState PlayerTimeChange(Guid playerId, decimal timeLeft, GameState state)
        {
            if (state.PlayerX == playerId)
                state.TimeLeftX = timeLeft;
            if (state.PlayerO == playerId)
                state.TimeLeftO = timeLeft;

            if (state.TimeLeftX <= 0)
            {
                state.GameResult.Result = EnumGameResult.TimeOutX;
                state.AddLog($"Game Over (timeout X)");
            }
            if (state.TimeLeftO <= 0)
            {
                state.GameResult.Result = EnumGameResult.TimeOutO;
                state.AddLog($"Game Over (timeout O)");
            }
            return state;
        }

        public static int[] GetWinFields(WinType winType)
        {
            var index = winType.Index;
            switch (winType.Direction)
            {
                case EnumWinDirection.Row:
                    return new int[] { index * 3, index * 3 + 1, index * 3 + 2 };
                case EnumWinDirection.Column:
                    return new int[] { index, index + 3, index + 6 };
                case EnumWinDirection.Diagonal:
                    return index == 0 ? new int[] { 0, 4, 8 } : new int[] { 2, 4, 6 };
                default:
                    return null;
            }
        }

        private static GameResult GetGameResult(IEnumerable<string> fields)
        {
            foreach (var p in GetWinPossibilities(fields))
            {
                var result = CheckWinner(p.values3);
                if (result != EnumGameResult.None)
                    return new GameResult
                    {
                        Result = result,
                        WinType = p.winType,
                    };
            }

            if (!fields.Where(r => string.IsNullOrEmpty(r)).Any())
                return new GameResult { Result = EnumGameResult.Tie };

            return new GameResult();
        }

        private static (WinType winType, string[] values3)[] GetWinPossibilities(IEnumerable<string> fields)
        {
            var possibilities = new List<(WinType winType, string[] values)>();
            for (int i = 0; i < 3; i++)
            {
                possibilities.Add(GetWinPossibility(fields, EnumWinDirection.Row, i));
                possibilities.Add(GetWinPossibility(fields, EnumWinDirection.Column, i));
            }
            possibilities.Add(GetWinPossibility(fields, EnumWinDirection.Diagonal, 0));
            possibilities.Add(GetWinPossibility(fields, EnumWinDirection.Diagonal, 1));

            return possibilities.ToArray();
        }

        private static (WinType winType, string[] values3) GetWinPossibility(IEnumerable<string> fields, EnumWinDirection direction, int index)
        {
            var winType = new WinType(direction, index);
            var values3 = GetWinFields(winType).Select(r => fields.ElementAt(r)).ToArray();
            return (winType, values3);
        }

        private static EnumGameResult CheckWinner(string[] values3)
        {
            var winner = values3[0] == values3[1] && values3[0] == values3[2] ? values3[0] : null;

            return !string.IsNullOrEmpty(winner)
                ? winner == EnumPlayerType.X.ToString() ? EnumGameResult.WinX : EnumGameResult.WinO
                : EnumGameResult.None;
        }

    }
}
