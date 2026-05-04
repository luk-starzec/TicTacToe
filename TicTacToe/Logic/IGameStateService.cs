using TicTacToe.Models;

namespace TicTacToe.Logic;

public interface IGameStateService
{
    GameState GetState(string gameId);

    void SaveState(string gameId, GameState state);
}
