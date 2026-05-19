using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QaWebli.Application.Services;
using QaWebli.Domain.Entities;

namespace QaWebli.TerminalUI.Presentation;

/// <summary>
/// Execution engine strategy for automated game mode with active countdowns and intermediate leaderboards.
/// </summary>
public sealed class AutomatedGameQuizEngine : IQuizEngine
{
    private bool _inLobby = true;
    private bool _isFinished = false;

    public bool InLobby => _inLobby;
    public bool IsFinished => _isFinished;

    public async Task RunAsync(Session session, SessionFacade facade, Action requestRender)
    {
        // 1. Wait in Lobby until presenter starts
        bool isTerminated = await WaitForLobbyToStartAsync(facade, requestRender);
        if (isTerminated) return;

        while (!isTerminated)
        {
            var questionIndex = session.CurrentQuestionIndex;
            var isLastQuestion = questionIndex == session.Quiz.TotalQuestions - 1;

            int lastRemainingSecs = -1;

            // Countdown ticks
            var startTime = session.GetQuestionStartTime(questionIndex);
            var duration = TimeSpan.FromSeconds(session.GameTimerSeconds);

            while (!isTerminated && !session.IsAnswerRevealed(questionIndex))
            {
                var elapsed = DateTime.UtcNow - startTime;
                var remaining = duration - elapsed;

                if (remaining <= TimeSpan.Zero)
                {
                    break;
                }

                // Periodically update the terminal with the countdown seconds
                var secs = (int)Math.Ceiling(remaining.TotalSeconds);
                if (secs != lastRemainingSecs)
                {
                    lastRemainingSecs = secs;
                    requestRender();
                }

                // Check termination or early answer reveal
                if (CheckTerminationKey())
                {
                    isTerminated = true;
                    break;
                }

                // Early exit if everyone has voted
                var votes = session.GetVoteCounts(questionIndex);
                var totalVotes = votes.Values.Sum();
                if (totalVotes > 0 && totalVotes >= session.StudentCount)
                {
                    break;
                }

                await Task.Delay(100);
            }

            if (isTerminated) break;

            // 2. Reveal correct answer
            await facade.RevealAnswerAsync();
            requestRender();

            // Ignore intermediate leaderboard display on the final question
            if (isLastQuestion)
            {
                break;
            }

            // Keep the answer revealed on screen for 5 seconds before intermediate leaderboard
            var revealEnd = DateTime.UtcNow.AddSeconds(5);
            while (DateTime.UtcNow < revealEnd && !isTerminated)
            {
                if (CheckTerminationKey())
                {
                    isTerminated = true;
                    break;
                }
                await Task.Delay(100);
            }

            if (isTerminated) break;

            // 3. Show intermediate Leaderboard (wait for 5 seconds)
            _isFinished = true; // temporarily flags leaderboard rendering
            requestRender();

            var leaderboardEnd = DateTime.UtcNow.AddSeconds(5);
            while (DateTime.UtcNow < leaderboardEnd && !isTerminated)
            {
                if (CheckTerminationKey())
                {
                    isTerminated = true;
                    break;
                }
                await Task.Delay(100);
            }

            if (isTerminated) break;

            // 4. Advance to the next question
            _isFinished = false;
            await facade.NextQuestionAsync();
        }

        // Final Leaderboard / Finish Game
        if (!isTerminated)
        {
            _isFinished = true;
            await facade.FinishGameAsync(new Dictionary<string, int>(session.StudentScores));
            requestRender();

            // Keep showing the leaderboard until the presenter presses Q to exit
            while (true)
            {
                try
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Q)
                        {
                            break;
                        }
                    }
                }
                catch (InvalidOperationException)
                {
                    // Non-interactive console fallback
                    await Task.Delay(5000);
                    break;
                }
                await Task.Delay(100);
            }
        }
    }

    private async Task<bool> WaitForLobbyToStartAsync(SessionFacade facade, Action requestRender)
    {
        _inLobby = true;
        requestRender();

        while (_inLobby)
        {
            bool keyAvailable = false;
            try
            {
                keyAvailable = Console.KeyAvailable;
            }
            catch (InvalidOperationException)
            {
                // Non-interactive/redirected console: auto-start after 3 seconds
                await Task.Delay(3000);
                _inLobby = false;
                break;
            }

            if (!keyAvailable)
            {
                await Task.Delay(100);
                continue;
            }

            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
            {
                _inLobby = false;
                break;
            }
            else if (key.Key == ConsoleKey.Q)
            {
                return true;
            }
        }

        await facade.StartGameAsync();
        return false;
    }

    private bool CheckTerminationKey()
    {
        try
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Q)
                {
                    return true;
                }
            }
        }
        catch (InvalidOperationException) { }
        return false;
    }
}
