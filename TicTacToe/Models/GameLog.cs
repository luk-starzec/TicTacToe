using System;

namespace TicTacToe.Models;

public class GameLog(string message)
{
    public DateTime Time { get; set; } = DateTime.Now;
    public string Message { get; set; } = message;

    public override string ToString()
    {
        return $"[{Time:t}]: {Message}";
    }
}