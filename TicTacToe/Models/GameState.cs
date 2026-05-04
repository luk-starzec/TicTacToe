using System;
using System.Collections.Generic;

namespace TicTacToe.Models;

public class GameState
{
    public string PlayerX { get; init; }
    public string PlayerO { get; init; }
    public string ActivePlayer { get; set; }
    public decimal TimeLeftXSeconds { get; set; }
    public decimal TimeLeftOSeconds { get; set; }
    public decimal BasePlayerTimeSeconds { get; set; }
    public decimal TimerIntervalSeconds { get; set; }
    public IEnumerable<string> Fields { get; init; }
    public IEnumerable<GameLog> Logs { get; init; }
    public EnumPlayerType FirstPlayer { get; set; }
    public EnumGameStage GameStage { get; set; }
    public GameResult GameResult { get; set; }
    public DateTime? LastMoveUtc { get; set; }

    public GameState()
    {
        Fields = new string[9];
        Logs = new List<GameLog>();
        GameStage = EnumGameStage.Preparing;
        GameResult = new GameResult();
    }

    public void AddLog(string message) => ((List<GameLog>)Logs).Add(new(message));

    public void SetField(int field, string value) => ((IList<string>)Fields)[field] = value;

    public EnumPlayerType? GetPlayerType(string playerId)
    {
        if (playerId == PlayerX)
            return EnumPlayerType.X;
        if (playerId == PlayerO)
            return EnumPlayerType.O;
        return null;
    }
}