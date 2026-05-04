using System;
using System.Collections.Generic;
using System.Linq;
using TicTacToe.Models;

namespace TicTacToe.Logic;

public static class GameLogic
{
    public static EnumPlayerType OponentType(EnumPlayerType playerType) => (EnumPlayerType)((int)playerType * (-1));

    public static string GetSpectatorUrl(string gameId) => $"/game/{gameId}";
    public static string GetJoinUrl(string gameId, string playerId) => $"/game/{gameId}/{playerId}";
    public static string GetStartUrl(string gameId, string playerId, EnumPlayerType playerType) => $"/game/{gameId}/{playerId}?init={playerType}";

    public static GameState CreateGameState(string playerId, EnumPlayerType playerType, string oponentId, decimal playerTimeSeconds, decimal timerIntervalSeconds)
    {
        var playerX = playerType == EnumPlayerType.X ? playerId : oponentId;
        var playerO = playerType == EnumPlayerType.O ? playerId : oponentId;

        var state = new GameState
        {
            PlayerX = playerX,
            PlayerO = playerO,
            TimeLeftXSeconds = playerTimeSeconds,
            TimeLeftOSeconds = playerTimeSeconds,
            BasePlayerTimeSeconds = playerTimeSeconds,
            TimerIntervalSeconds = timerIntervalSeconds,
            ActivePlayer = playerId,
            FirstPlayer = playerType,
            LastMoveUtc = null // Timer starts only when match starts
        };

        state.AddLog($"Game created");

        return state;
    }

    public static GameState UpdateTime(GameState state)
    {
        if (!state.LastMoveUtc.HasValue || state.GameStage != EnumGameStage.Started)
        {
            state.LastMoveUtc = state.GameStage == EnumGameStage.Started ? DateTime.UtcNow : null;
            return state;
        }

        var elapsed = (decimal)(DateTime.UtcNow - state.LastMoveUtc.Value).TotalSeconds;
        if (state.ActivePlayer == state.PlayerX)
            state.TimeLeftXSeconds -= elapsed;
        else
            state.TimeLeftOSeconds -= elapsed;

        state.LastMoveUtc = DateTime.UtcNow;

        if (state.TimeLeftXSeconds <= 0)
        {
            state.TimeLeftXSeconds = 0;
            state.GameResult.Result = EnumGameResult.TimeOutX;
            state.ActivePlayer = null;
            state.AddLog($"Game Over (timeout X)");
        }
        else if (state.TimeLeftOSeconds <= 0)
        {
            state.TimeLeftOSeconds = 0;
            state.GameResult.Result = EnumGameResult.TimeOutO;
            state.ActivePlayer = null;
            state.AddLog($"Game Over (timeout O)");
        }

        return state;
    }

    public static GameState StartCountdown(GameState state)
    {
        if (state.GameStage != EnumGameStage.Preparing)
            return state;

        state.GameStage = EnumGameStage.Starting;

        var player = state.ActivePlayer == state.PlayerX ? EnumPlayerType.X : EnumPlayerType.O;
        state.AddLog($"Game starting (active {player})");

        return state;
    }

    public static GameState StartMatch(GameState state)
    {
        if (state.GameStage != EnumGameStage.Starting)
            return state;

        state.GameStage = EnumGameStage.Started;
        state.LastMoveUtc = DateTime.UtcNow; // Set start time for the first turn
        state.AddLog($"Ready, set, go!");

        return state;
    }

    public static GameState PlayerMoved(string playerId, int field, GameState state)
    {
        state = UpdateTime(state); // Calculate time before moving

        if (state.GameResult.Result != EnumGameResult.None)
            return state;

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
            state.LastMoveUtc = DateTime.UtcNow; // Reset turn start time
            state.AddLog($"Next round");
        }
        else
        {
            state.ActivePlayer = null;
            state.LastMoveUtc = null;
            state.AddLog($"Game Over ({state.GameResult.Result.GetDescription()})");
        }

        return state;
    }

    public static int[] GetWinFields(WinType winType)
    {
        var index = winType.Index;
        return winType.Direction switch
        {
            EnumWinDirection.Row => [index * 3, index * 3 + 1, index * 3 + 2],
            EnumWinDirection.Column => [index, index + 3, index + 6],
            EnumWinDirection.Diagonal => index == 0 ? [0, 4, 8] : [2, 4, 6],
            _ => null,
        };
    }

    private static GameResult GetGameResult(IEnumerable<string> fields)
    {
        foreach (var (winType, values3) in GetWinPossibilities(fields))
        {
            var result = CheckWinner(values3);
            if (result != EnumGameResult.None)
                return new GameResult
                {
                    Result = result,
                    WinType = winType,
                };
        }

        if (!fields.Any(r => string.IsNullOrEmpty(r)))
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

        return [.. possibilities];
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
