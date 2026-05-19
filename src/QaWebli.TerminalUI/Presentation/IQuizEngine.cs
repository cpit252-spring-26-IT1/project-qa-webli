using System;
using System.Threading.Tasks;
using QaWebli.Application.Services;
using QaWebli.Domain.Entities;

namespace QaWebli.TerminalUI.Presentation;

/// <summary>
/// Defines the execution engine contract for presenting a quiz, allowing different strategies
/// (e.g., manual keyboard input vs. automated game loop with timers) to run the session.
/// </summary>
public interface IQuizEngine
{
    /// <summary>
    /// Gets whether the engine is currently in the lobby stage waiting for players.
    /// </summary>
    bool InLobby { get; }

    /// <summary>
    /// Gets whether the engine is currently displaying a leaderboard.
    /// </summary>
    bool IsFinished { get; }

    /// <summary>
    /// Runs the quiz execution loop.
    /// </summary>
    Task RunAsync(Session session, SessionFacade facade, Action requestRender);
}
