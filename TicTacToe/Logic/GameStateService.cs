using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using TicTacToe.Models;

namespace TicTacToe.Logic;

public class GameStateService : IGameStateService
{
    private readonly IMemoryCache _cache;
    private readonly GameSettings _settings;

    public GameStateService(IMemoryCache cache, IOptions<GameSettings> settings)
    {
        _cache = cache;
        _settings = settings.Value;
    }

    public GameState GetState(string gameId)
    {
        // Try to get the state using the gameId as the key.
        if (_cache.TryGetValue(gameId, out GameState state))
        {
            return state;
        }

        return null;
    }

    public void SaveState(string gameId, GameState state)
    {
        var options = new MemoryCacheEntryOptions();

        if (state.GameResult.Result != EnumGameResult.None)
        {
            // If the game is over, keep it for a short time to allow players to see the result.
            options.SetAbsoluteExpiration(_settings.GameOverPersistence);
        }
        else
        {
            // Calculate dynamic expiration: remaining time for both players + joining buffer.
            // This ensures the cache entry lives long enough for the game to complete.
            var totalRemainingTime = TimeSpan.FromSeconds((double)(state.TimeLeftXSeconds + state.TimeLeftOSeconds))
                                    .Add(_settings.JoiningTimeout);

            options.SetAbsoluteExpiration(totalRemainingTime);

            // Also add a sliding expiration to purge inactive/abandoned games faster.
            options.SetSlidingExpiration(_settings.JoiningTimeout);
        }

        _cache.Set(gameId, state, options);
    }
}
