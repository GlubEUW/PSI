using Api.Models;
using Api.Services;

namespace Api.Tests.Services;

public class TournamentStoreUnitTests
{
    [Fact]
    public void Sessions_CanStoreAndRetrieveSession()
    {
        var store = new TournamentStore();
        var session = new TournamentSession
        {
            Code = "CODE",
            TournamentId = Guid.NewGuid(),
            NumberOfRounds = 1,
            CurrentRound = 0,
            TournamentStarted = false
        };

        store.Sessions["CODE"] = session;

        Assert.True(store.Sessions.TryGetValue("CODE", out TournamentSession? found));
        Assert.Same(session, found);
    }
}
