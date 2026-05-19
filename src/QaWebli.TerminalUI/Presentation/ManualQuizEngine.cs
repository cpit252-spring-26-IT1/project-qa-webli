using System;
using System.Threading.Tasks;
using QaWebli.Application.Services;
using QaWebli.Domain.Entities;

namespace QaWebli.TerminalUI.Presentation;

/// <summary>
/// Execution engine strategy for presenter-guided manual keyboard navigation.
/// </summary>
public sealed class ManualQuizEngine : IQuizEngine
{
    public bool InLobby => false;
    public bool IsFinished => false;

    public async Task RunAsync(Session session, SessionFacade facade, Action requestRender)
    {
        requestRender();

        bool isTerminated = false;
        while (!isTerminated)
        {
            bool keyAvailable = false;
            try
            {
                keyAvailable = Console.KeyAvailable;
            }
            catch (InvalidOperationException)
            {
                // Fallback for redirected console environments (like testing pipelines)
                await Task.Delay(2000);
                break;
            }

            if (!keyAvailable)
            {
                await Task.Delay(100);
                continue;
            }

            var key = Console.ReadKey(true);
            switch (key.Key)
            {
                case ConsoleKey.LeftArrow:
                    await facade.PreviousQuestionAsync();
                    break;
                case ConsoleKey.RightArrow:
                    await facade.NextQuestionAsync();
                    break;
                case ConsoleKey.Spacebar:
                    await facade.RevealAnswerAsync();
                    break;
                case ConsoleKey.Q:
                    isTerminated = true;
                    break;
            }
        }
    }
}
