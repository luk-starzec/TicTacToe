using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using TicTacToe.Models;

namespace TicTacToe.Logic
{
    public class GameHub : Hub
    {
        public const string HubUrl = "/gameHub";

        public async Task CreateGame(Guid gameId, Guid playerId, EnumPlayerType playerType)
        {
            var oponentId = Guid.NewGuid();

            var state = GameLogic.CreateGameState(playerId, playerType, oponentId);
            Context.Items.Add(gameId, state);

            await Clients.Caller.SendAsync(EnumGameEvent.GameCreated.ToString(), oponentId);
        }

        public async Task SynchronizeState(Guid gameId)
        {
            var state = GetState(gameId);
            if (state is not null)
                await Clients.OthersInGroup(gameId.ToString()).SendAsync(EnumGameEvent.PlayersReady.ToString(), state);
        }

        public void UpdateState(Guid gameId, GameState gameState)
        {
            var state = GetState(gameId);
            if (state == null)
                Context.Items.Add(gameId, gameState);
            else
                Context.Items[gameId] = gameState;
        }

        public async Task JoinGame(Guid gameId, Guid? playerId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId.ToString());
            await Clients.Group(gameId.ToString()).SendAsync(EnumGameEvent.PlayerJoined.ToString(), playerId);
        }

        public async Task StartGame(Guid gameId)
        {
            var state = GetState(gameId);
            state = GameLogic.StartGame(state);

            await Clients.Group(gameId.ToString()).SendAsync(EnumGameEvent.PlayersReady.ToString(), state);
        }

        public async Task Play(Guid gameId, Guid playerId, int field)
        {
            var state = GetState(gameId);
            state = GameLogic.PlayerMoved(playerId, field, state);

            await Clients.Group(gameId.ToString()).SendAsync(EnumGameEvent.PlayerMoved.ToString(), state);

            if (state.GameResult.Result != EnumGameResult.None)
                await Clients.Group(gameId.ToString()).SendAsync(EnumGameEvent.GameOver.ToString(), state);
        }

        public async Task PlayerTimeChange(Guid gameId, Guid playerId, decimal timeLeft)
        {
            var state = GetState(gameId);
            state = GameLogic.PlayerTimeChange(playerId, timeLeft, state);

            await Clients.Group(gameId.ToString()).SendAsync(EnumGameEvent.PlayerTimeChanged.ToString(), state);

            if (state.GameResult.Result != EnumGameResult.None)
                await Clients.Group(gameId.ToString()).SendAsync(EnumGameEvent.GameOver.ToString(), state);
        }

        private GameState GetState(Guid gameId) => Context.Items[gameId] as GameState;
    }
}
