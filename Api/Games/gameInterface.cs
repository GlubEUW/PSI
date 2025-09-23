using System.Data;

namespace Games
{
    public interface ITurnBasedGame
    {
        string State { get; }
        int CurrentPlayer { get; }
        bool MakeMove(object move);

    }
}