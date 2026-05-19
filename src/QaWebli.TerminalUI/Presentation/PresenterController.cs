// DISCLAIMER: This was an AI abstraction created after my human got frustrated trying to not over-clump Program.cs.
using System;
using System.Threading.Tasks;
using QaWebli.Application.Services;
using QaWebli.Domain.Entities;

namespace QaWebli.TerminalUI.Presentation;

/// <summary>
/// Controls the presenter console's execution by delegating to the appropriate IQuizEngine strategy
/// and dispatching rendering to the concrete view classes.
/// </summary>
public sealed class PresenterController
{
    private readonly Session _session;
    private readonly SessionFacade _facade;
    private readonly IQuizEngine _engine;

    private readonly LobbyConsoleView _lobbyView;
    private readonly QuestionConsoleView _questionView;
    private readonly LeaderboardRenderer _leaderboardView;

    public PresenterController(Session session, SessionFacade facade, string joinUrl)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _facade = facade ?? throw new ArgumentNullException(nameof(facade));

        _engine = session.IsGameMode
            ? new AutomatedGameQuizEngine()
            : new ManualQuizEngine();

        _lobbyView = new LobbyConsoleView(session, joinUrl);
        _questionView = new QuestionConsoleView(session, joinUrl);
        _leaderboardView = new LeaderboardRenderer(session);

        // Subscribe to facade updates to automatically re-render when state changes
        _facade.Subscribe(new ConsoleObserver(RequestRender));
    }

    public async Task RunAsync()
    {
        await _engine.RunAsync(_session, _facade, RequestRender);
    }

    private void RequestRender()
    {
        if (_engine.InLobby)
            _lobbyView.Render();
        else if (_engine.IsFinished)
            _leaderboardView.Render();
        else
            _questionView.Render();
    }
}
