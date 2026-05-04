using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TicTacToe.Helpers;
using TicTacToe.Models;

namespace TicTacToe.Logic;

public class GameHub : Hub<IGameClient>
{
    public const string HubUrl = "/gameHub";

    private readonly IGameStateService _stateService;
    private readonly GameSettings _settings;
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> _timers = new();

    public GameHub(IGameStateService stateService, IOptions<GameSettings> settings)
    {
        _stateService = stateService;
        _settings = settings.Value;
    }

    public async Task CreateGame(string gameId, string playerId, EnumPlayerType playerType)
    {
        var existingState = _stateService.GetState(gameId);
        if (existingState != null)
        {
            // Game already exists, return the stored opponent ID
            var oponentIdStored = playerType == EnumPlayerType.X ? existingState.PlayerO : existingState.PlayerX;
            await Clients.Caller.GameCreated(oponentIdStored);
            return;
        }

        var oponentId = IdGenerator.Generate();

        // Create new game state using configured default player time and interval
        var playerTimeSeconds = (decimal)_settings.DefaultPlayerTime.TotalSeconds;
        var timerIntervalSeconds = (decimal)_settings.TimerInterval.TotalSeconds;

        var state = GameLogic.CreateGameState(playerId, playerType, oponentId, playerTimeSeconds, timerIntervalSeconds);
        _stateService.SaveState(gameId, state);

        await Clients.Caller.GameCreated(oponentId);
    }

    public async Task JoinGame(string gameId, string playerId)
    {
        var state = _stateService.GetState(gameId);
        if (state is null)
        {
            await Clients.Caller.GameNotFound();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
        await Clients.Group(gameId).PlayerJoined(playerId);
    }

    public async Task StartCountdown(string gameId)
    {
        var state = _stateService.GetState(gameId);
        if (state is null || state.GameStage != EnumGameStage.Preparing) 
            return;

        state = GameLogic.StartCountdown(state);
        _stateService.SaveState(gameId, state);

        await Clients.Group(gameId).PlayersReady(state);
    }

    public async Task StartMatch(string gameId)
    {
        var state = _stateService.GetState(gameId);
        if (state is null || state.GameStage != EnumGameStage.Starting) 
            return;

        state = GameLogic.StartMatch(state);
        _stateService.SaveState(gameId, state);

        var timeLeft = state.ActivePlayer == state.PlayerX ? state.TimeLeftXSeconds : state.TimeLeftOSeconds;
        StartServerTimer(gameId, timeLeft);

        await Clients.Group(gameId).PlayersReady(state);
    }

    public async Task Play(string gameId, string playerId, int field)
    {
        var state = _stateService.GetState(gameId);
        if (state is null)
        {
            await Clients.Caller.GameNotFound();
            return;
        }

        state = GameLogic.PlayerMoved(playerId, field, state);
        _stateService.SaveState(gameId, state);

        if (state.GameResult.Result == EnumGameResult.None)
        {
            var timeLeft = state.ActivePlayer == state.PlayerX ? state.TimeLeftXSeconds : state.TimeLeftOSeconds;
            StartServerTimer(gameId, timeLeft);
        }
        else
        {
            _timers.TryRemove(gameId, out var old);
            old?.Cancel();
        }

        await Clients.Group(gameId).PlayerMoved(state);

        if (state.GameResult.Result != EnumGameResult.None)
        {
            await Clients.Group(gameId).GameOver(state);
        }
    }

    public async Task SynchronizeState(string gameId)
    {
        var state = _stateService.GetState(gameId);
        if (state is null)
        {
            await Clients.Caller.GameNotFound();
            return;
        }

        state = GameLogic.UpdateTime(state);
        _stateService.SaveState(gameId, state);

        await Clients.Caller.PlayersReady(state);
    }

    private void StartServerTimer(string gameId, decimal seconds)
    {
        _timers.TryRemove(gameId, out var old);
        old?.Cancel();

        var cts = new CancellationTokenSource();
        _timers[gameId] = cts;

        // Use a long-running task to wait for the timeout
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay((int)(seconds * 1000), cts.Token);
                if (!cts.Token.IsCancellationRequested)
                {
                    await EndGameByTimeout(gameId);
                }
            }
            catch (TaskCanceledException) { /* Expected on move */ }
            catch (Exception) { /* Log error */ }
            finally
            {
                _timers.TryRemove(gameId, out _);
            }
        });
    }

    private async Task EndGameByTimeout(string gameId)
    {
        var state = _stateService.GetState(gameId);
        if (state is null || state.GameResult.Result != EnumGameResult.None)
            return;

        state = GameLogic.UpdateTime(state);
        _stateService.SaveState(gameId, state);

        if (state.GameResult.Result != EnumGameResult.None)
        {
            await Clients.Group(gameId).GameOver(state);
        }
    }
}
