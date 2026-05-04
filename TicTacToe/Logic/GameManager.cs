using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using TicTacToe.Models;

namespace TicTacToe.Logic;

public class GameManager : INotifyPropertyChanged
{
    private readonly HubConnection _gameConnection;

    public string GameId { get; private set; }
    public GamePreparation GamePreparation { get; private set; }
    public bool IsPlayer { get; private set; }
    public bool IsNotFound { get; private set; }
    public Player Player { get; set; }
    public Player Opponent { get; private set; }
    public GameState State { get; private set; }
    public IEnumerable<string> Fields => State?.Fields;
    public EnumPlayerType FirstPlayer => State?.FirstPlayer ?? 0;
    public EnumGameStage? GameStage => State?.GameStage;
    public GameResult GameResult => State?.GameResult;
    public IEnumerable<GameLog> Logs => State?.Logs;

    public event PropertyChangedEventHandler PropertyChanged;

    public GameManager(string hubUrl, string gameId)
    {
        GameId = gameId;
        GamePreparation = new GamePreparation { GameId = GameId };

        _gameConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .Build();
        _gameConnection.On(EnumGameEvent.GameCreated.ToString(), (Action<string>)((opponentId) => GameCreated(opponentId)));
        _gameConnection.On(EnumGameEvent.PlayerJoined.ToString(), (Action<string>)(async (playerId) => await PlayerJoined(playerId)));
        _gameConnection.On(EnumGameEvent.PlayersReady.ToString(), (Action<GameState>)((state) => PlayersReady(state)));
        _gameConnection.On(EnumGameEvent.PlayerMoved.ToString(), (Action<GameState>)((state) => PlayerMoved(state)));
        _gameConnection.On(EnumGameEvent.GameOver.ToString(), (Action<GameState>)((state) => GameOver(state)));
        _gameConnection.On(EnumGameEvent.GameNotFound.ToString(), () => GameNotFound());
    }

    public async Task StartConnection() => await _gameConnection.StartAsync();

    public async Task CreateGame() => await _gameConnection.SendAsync(EnumGameAction.CreateGame.ToString(), GameId, Player.PlayerId, Player.PlayerType);

    public async Task JoinGame() => await _gameConnection.SendAsync(EnumGameAction.JoinGame.ToString(), GameId, Player?.PlayerId);

    public async Task SynchronizeState() => await _gameConnection.SendAsync(EnumGameAction.SynchronizeState.ToString(), GameId);

    public async Task StartCountdown() => await _gameConnection.SendAsync(EnumGameAction.StartCountdown.ToString(), GameId);

    public async Task StartMatch() => await _gameConnection.SendAsync(EnumGameAction.StartMatch.ToString(), GameId);

    public async Task Play(int field)
    {
        Player?.IsActive = false;
        await _gameConnection.SendAsync(EnumGameAction.Play.ToString(), GameId, Player.PlayerId, field);
    }

    private void GameCreated(string opponentId)
    {
        GamePreparation.OpponentId = opponentId;
        GamePreparation.OpponentName = GameLogic.OponentType(Player.PlayerType.Value).ToString();

        UpdateUI();
    }

    private async Task PlayerJoined(string playerId)
    {
        if (GameStage != EnumGameStage.Preparing || playerId != (GamePreparation?.OpponentId))
            return;

        await StartCountdown();
        UpdateUI();
    }

    private void PlayersReady(GameState state)
    {
        InitPlayers(state);
        UpdateGameState(state);

        if (state.GameStage == EnumGameStage.Preparing && IsPlayer && Player.PlayerType == state.FirstPlayer)
        {
            GamePreparation.OpponentId = Opponent.PlayerId;
            GamePreparation.OpponentName = Opponent.PlayerName;
        }

        UpdateUI();
    }

    private void InitPlayers(GameState state)
    {
        if (Player is not null && Opponent is not null)
            return;

        IsPlayer = Player?.PlayerId == state.PlayerX || Player?.PlayerId == state.PlayerO;

        var isX = state.PlayerX == Player?.PlayerId;

        var playerX = CreatePlayer(EnumPlayerType.X, state.PlayerX, state.TimeLeftXSeconds, state.BasePlayerTimeSeconds, state.TimerIntervalSeconds);
        var playerO = CreatePlayer(EnumPlayerType.O, state.PlayerO, state.TimeLeftOSeconds, state.BasePlayerTimeSeconds, state.TimerIntervalSeconds);

        Player = isX ? playerX : playerO;
        Opponent = isX ? playerO : playerX;
    }

    private Player CreatePlayer(EnumPlayerType playerType, string playerId, decimal timeLeftSeconds, decimal playerTimeSeconds, decimal timerIntervalSeconds)
    {
        var player = new Player(playerId, playerType, timeLeftSeconds, playerTimeSeconds, timerIntervalSeconds);
        player.OnTimeTick += Player_OnTimeTick;

        return player;
    }

    private async Task Player_OnTimeTick(string playerId, decimal timeLeftSeconds)
    {
        UpdateUI();
        await Task.CompletedTask;
    }

    private void PlayerMoved(GameState state)
    {
        UpdateGameState(state);

        if (IsPlayer)
        {
            Player.IsActive = state.ActivePlayer == Player.PlayerId;
            Opponent.IsActive = state.ActivePlayer == Opponent.PlayerId;
        }

        UpdateUI();
    }

    private void GameOver(GameState state)
    {
        UpdateGameState(state);

        Player?.IsActive = false;
        Opponent?.IsActive = false;

        UpdateUI();
    }

    private void GameNotFound()
    {
        IsNotFound = true;
        UpdateUI();
    }

    private void UpdateGameState(GameState state)
    {
        State = state;
        if (Player != null)
        {
            Player.TimeLeftSeconds = Player.PlayerId == state.PlayerX ? state.TimeLeftXSeconds : state.TimeLeftOSeconds;

            if (state.GameStage == EnumGameStage.Started)
            {
                Player.IsActive = state.ActivePlayer == Player.PlayerId;
            }
            else
            {
                Player.IsActive = false;
            }
        }
        if (Opponent != null)
        {
            Opponent.TimeLeftSeconds = Opponent.PlayerId == state.PlayerX ? state.TimeLeftXSeconds : state.TimeLeftOSeconds;

            if (state.GameStage == EnumGameStage.Started)
            {
                Opponent.IsActive = state.ActivePlayer == Opponent.PlayerId;
            }
            else
            {
                Opponent.IsActive = false;
            }
        }
    }

    private void UpdateUI()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));
    }
}
