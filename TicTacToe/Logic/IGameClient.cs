using System.Threading.Tasks;
using TicTacToe.Models;

namespace TicTacToe.Logic;

public interface IGameClient
{
    Task GameCreated(string opponentId);
    Task PlayerJoined(string playerId);
    Task PlayersReady(GameState state);
    Task PlayerMoved(GameState state);
    Task GameOver(GameState state);
    Task GameNotFound();
}
