using System;
using System.Threading.Tasks;
using System.Timers;
using TicTacToe.Logic;

namespace TicTacToe.Models
{
    public class Player
    {
        public Guid PlayerId { get; }
        public EnumPlayerType? PlayerType { get; set; }
        public string PlayerName => PlayerType.ToString();
        public bool IsPlayer => PlayerType.HasValue;

        private bool isActive;
        public bool IsActive
        {
            get => isActive;
            set
            {
                isActive = value;

                if (isActive)
                    timer.Start();
                else
                    timer.Stop();
            }
        }

        public decimal TimeLeft { get; set; }
        public decimal TimePercentLeft => TimeLeft / fullTime;

        public event Func<Guid, decimal, Task> OnTimeTick;

        private decimal fullTime => GameLogic.PlayerTime;
        private decimal timeInterval = GameLogic.TimeInterval;
        private Timer timer = new Timer();

        public Player(Guid playerId)
        {
            PlayerId = playerId;

            timer.Elapsed += OnTimerTick;
            timer.Interval = (double)(timeInterval * 1000);
        }

        public Player(Guid playerId, EnumPlayerType playerType, decimal timeLeft)
            : this(playerId)
        {
            PlayerType = playerType;
            TimeLeft = timeLeft;
        }


        private void OnTimerTick(object sender, ElapsedEventArgs e)
        {
            TimeLeft -= timeInterval;
            OnTimeTick?.Invoke(PlayerId, TimeLeft);
        }
    }
}
