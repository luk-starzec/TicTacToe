using System;
using System.Threading.Tasks;
using System.Timers;

namespace TicTacToe.Models;

public class Player
{
    public string PlayerId { get; }
    public EnumPlayerType? PlayerType { get; set; }
    public string PlayerName => PlayerType.ToString();
    public bool IsPlayer => PlayerType.HasValue;

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;

            if (_isActive)
                timer.Start();
            else
                timer.Stop();
        }
    }

    public decimal TimeLeftSeconds { get; set; }
    public decimal TimePercentLeft => _fullTimeSeconds > 0 ? TimeLeftSeconds / _fullTimeSeconds : 0;

    public event Func<string, decimal, Task> OnTimeTick;

    private readonly decimal _fullTimeSeconds;
    private readonly decimal _timeIntervalSeconds;
    private readonly Timer timer = new();

    public Player(string playerId, decimal playerTimeSeconds = 20, decimal timeIntervalSeconds = 1)
    {
        PlayerId = playerId;
        _fullTimeSeconds = playerTimeSeconds;
        _timeIntervalSeconds = timeIntervalSeconds;

        timer.Elapsed += OnTimerTick;
        timer.Interval = (double)(timeIntervalSeconds * 1000);
    }

    public Player(string playerId, EnumPlayerType playerType, decimal timeLeftSeconds, decimal playerTimeSeconds = 20, decimal timeIntervalSeconds = 1)
        : this(playerId, playerTimeSeconds, timeIntervalSeconds)
    {
        PlayerType = playerType;
        TimeLeftSeconds = timeLeftSeconds;
    }

    private void OnTimerTick(object sender, ElapsedEventArgs e)
    {
        TimeLeftSeconds -= _timeIntervalSeconds;
        OnTimeTick?.Invoke(PlayerId, TimeLeftSeconds);
    }
}