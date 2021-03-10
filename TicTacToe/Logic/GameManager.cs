using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using TicTacToe.Models;

namespace TicTacToe.Logic
{
    public class GameManager : INotifyPropertyChanged
    {
        private readonly HubConnection gameConnection;

        public bool IsConnected => gameConnection?.State == HubConnectionState.Connected;

        public Guid GameId { get; private set; }
        public GamePreparation GamePreparation { get; private set; }
        public bool IsPlayer { get; private set; }
        public Player Player { get; set; }
        public Player Opponent { get; private set; }
        public GameState State { get; private set; }
        public IEnumerable<string> Fields => State?.Fields;
        public EnumPlayerType FirstPlayer => State?.FirstPlayer ?? 0;
        public EnumGameStage GameStage => State?.GameStage ?? EnumGameStage.Preparing;
        public GameResult GameResult => State?.GameResult;
        public IEnumerable<GameLog> Logs => State?.Logs;

        public event PropertyChangedEventHandler PropertyChanged;


        public GameManager(string hubUrl, Guid gameId)
        {
            GameId = gameId;
            GamePreparation = new GamePreparation { GameId = GameId };

            gameConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .Build();
            gameConnection.On(EnumGameEvent.GameCreated.ToString(), (Action<Guid>)((opponentId) => GameCreated(opponentId)));
            gameConnection.On(EnumGameEvent.PlayerJoined.ToString(), (Action<Guid?>)(async (playerId) => await PlayerJoined(playerId)));
            gameConnection.On(EnumGameEvent.PlayersReady.ToString(), (Action<GameState>)(async (state) => await PlayersReady(state)));
            gameConnection.On(EnumGameEvent.PlayerMoved.ToString(), (Action<GameState>)(async (state) => await PlayerMoved(state)));
            gameConnection.On(EnumGameEvent.PlayerTimeChanged.ToString(), (Action<GameState>)(async (state) => await PlayerTimeChanged(state)));
            gameConnection.On(EnumGameEvent.GameOver.ToString(), (Action<GameState>)(async (state) => await GameOver(state)));
        }

        private void GameCreated(Guid opponentId)
        {
            GamePreparation.OpponentId = opponentId;
            GamePreparation.OpponentName = GameLogic.OponentType(Player.PlayerType.Value).ToString();

            UpdateUI();
        }

        private async Task PlayerJoined(Guid? playerId)
        {
            if (playerId == GamePreparation?.OpponentId)
            {
                await gameConnection.SendAsync(EnumGameAction.StartGame.ToString(), GameId);
                UpdateUI();
            }
            if (playerId is null)
                await SynchronizeState();
        }

        private async Task PlayersReady(GameState state)
        {
            InitPlayers(state);
            await UpdateGameState(state);
            UpdateUI();
        }

        private void InitPlayers(GameState state)
        {
            if (Player is not null && Opponent is not null)
                return;

            IsPlayer = Player?.PlayerId == state.PlayerX || Player?.PlayerId == state.PlayerO;

            var isX = state.PlayerX == Player?.PlayerId;

            var playerX = CreatePlayer(EnumPlayerType.X, state.PlayerX, state.TimeLeftX);
            var playerO = CreatePlayer(EnumPlayerType.O, state.PlayerO, state.TimeLeftO);

            Player = isX ? playerX : playerO;
            Opponent = isX ? playerO : playerX;
        }

        private async Task PlayerMoved(GameState state)
        {
            await UpdateGameState(state);

            if (IsPlayer)
                Player.IsActive = state.ActivePlayer == Player.PlayerId;

            UpdateUI();
        }

        private async Task PlayerTimeChanged(GameState state)
        {
            await UpdateGameState(state);
            UpdateUI();
        }

        private async Task GameOver(GameState state)
        {
            await UpdateGameState(state);

            if (IsPlayer)
                Player.IsActive = false;

            UpdateUI();
        }


        public async Task StartConnection() => await gameConnection.StartAsync();

        public async Task CreateGame() => await gameConnection.SendAsync(EnumGameAction.CreateGame.ToString(), GameId, Player.PlayerId, Player.PlayerType);

        public async Task JoinGame() => await gameConnection.SendAsync(EnumGameAction.JoinGame.ToString(), GameId, Player?.PlayerId);

        public async Task SynchronizeState() => await gameConnection.SendAsync(EnumGameAction.SynchronizeState.ToString(), GameId);

        public async Task StartGame()
        {
            Player.IsActive = State.ActivePlayer == Player.PlayerId;
            State.GameStage = EnumGameStage.Started;
            await UpdateGameState(State);
        }

        public async Task Play(int field)
        {
            Player.IsActive = false;
            await gameConnection.SendAsync(EnumGameAction.Play.ToString(), GameId, Player.PlayerId, field);
        }

        private async Task UpdateGameState(GameState state)
        {
            State = state;
            if (Player != null)
                Player.TimeLeft = Player.PlayerId == state.PlayerX ? state.TimeLeftX : state.TimeLeftO;
            if (Opponent != null)
                Opponent.TimeLeft = Opponent.PlayerId == state.PlayerX ? state.TimeLeftX : state.TimeLeftO;

            await gameConnection.SendAsync(EnumGameAction.UpdateState.ToString(), GameId, state);
        }

        private void UpdateUI()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));
        }

        private Player CreatePlayer(EnumPlayerType playerType, Guid playerId, decimal timeLeft)
        {
            var player = new Player(playerId, playerType, timeLeft);
            if (IsPlayer)
                player.OnTimeTick += Player_OnTimeTick;
            return player;
        }

        private async Task Player_OnTimeTick(Guid playerId, decimal timeLeft)
        {
            await gameConnection.SendAsync(EnumGameAction.PlayerTimeChange.ToString(), GameId, playerId, timeLeft);
        }
    }
}
