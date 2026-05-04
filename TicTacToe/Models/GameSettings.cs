using System;

namespace TicTacToe.Models;

public class GameSettings
{
    /// <summary>
    /// Base time for each player.
    /// </summary>
    public TimeSpan DefaultPlayerTime { get; set; }

    /// <summary>
    /// Frequency of the timer tick.
    /// </summary>
    public TimeSpan TimerInterval { get; set; }

    /// <summary>
    /// How long to wait for a second player to join before expiring the game session.
    /// </summary>
    public TimeSpan JoiningTimeout { get; set; }

    /// <summary>
    /// How long to keep the game in cache after it is finished (for result viewing).
    /// </summary>
    public TimeSpan GameOverPersistence { get; set; }
}
