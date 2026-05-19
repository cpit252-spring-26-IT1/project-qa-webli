using System;
using System.IO;
using System.Linq;

namespace QaWebli.TerminalUI.Hosting;

public sealed record CliOptions(
    string QuizPath,
    int Port,
    string Bind,
    bool NoStudentUi,
    bool Https,
    bool Ngrok,
    string? NgrokAuthtoken,
    bool Game = false,
    int GameTimerSeconds = 10)
{
    public bool EnableStudentUi => !NoStudentUi;

    public static CliOptions? Parse(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help", StringComparer.OrdinalIgnoreCase) || args.Contains("-h", StringComparer.OrdinalIgnoreCase))
        {
            PrintHelp();
            return null;
        }

        string? quiz = null;
        var port = 8080;
        var bind = "0.0.0.0";
        var noStudentUi = false;
        var https = false;
        var ngrok = false;
        string? ngrokAuthtoken = null;
        var game = false;
        var gameTimerSeconds = 10;

        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (!a.StartsWith("-", StringComparison.Ordinal))
            {
                quiz ??= a;
                continue;
            }

            switch (a)
            {
                case "--quiz":
                    quiz = NextValue(args, ref i, "--quiz");
                    break;
                case "--port":
                    if (!int.TryParse(NextValue(args, ref i, "--port"), out port) || port is < 1 or > 65535)
                    {
                        Console.WriteLine("Invalid --port value (use an integer 1–65535).");
                        return null;
                    }
                    break;
                case "--bind":
                    bind = NextValue(args, ref i, "--bind");
                    break;
                case "--no-student-ui":
                    noStudentUi = true;
                    break;
                case "--https":
                    https = true;
                    break;
                case "--ngrok":
                    ngrok = true;
                    break;
                case "--ngrok-authtoken":
                    ngrokAuthtoken = NextValue(args, ref i, "--ngrok-authtoken");
                    break;
                case "-g":
                case "--game":
                    game = true;
                    break;
                case "--timer":
                    if (!int.TryParse(NextValue(args, ref i, "--timer"), out gameTimerSeconds) || gameTimerSeconds <= 0)
                    {
                        Console.WriteLine("Invalid --timer value (use an integer > 0).");
                        return null;
                    }
                    break;
                default:
                    Console.WriteLine($"Unknown argument: {a}");
                    PrintHelp();
                    return null;
            }
        }

        if (string.IsNullOrWhiteSpace(quiz))
        {
            Console.WriteLine("Missing quiz file path.");
            PrintHelp();
            return null;
        }

        if (ngrok && string.IsNullOrWhiteSpace(ngrokAuthtoken))
            ngrokAuthtoken = Environment.GetEnvironmentVariable("NGROK_AUTHTOKEN");

        return new CliOptions(quiz, port, bind, noStudentUi, https, ngrok, ngrokAuthtoken, game, gameTimerSeconds);
    }

    private static string NextValue(string[] args, ref int i, string flag)
    {
        if (i + 1 >= args.Length) throw new ArgumentException($"Missing value for {flag}");
        return args[++i];
    }

    private static void PrintHelp()
    {
        Console.WriteLine(
            """
            QA-CLI

            Usage:
              qa-cli <quiz.md> [options]

            Options:
              --quiz <path>         Path to quiz markdown (alternative to positional).
              --port <number>       Local server port (default: 8080).
              --bind <host|ip>      Host/IP to advertise in the join URL (default: 0.0.0.0).
              --no-student-ui       Presenter-only mode (no embedded join page).
              --https               Display join URL as https (useful behind tunnels).
              --ngrok               Start ngrok tunnel and print a public join URL.
              --ngrok-authtoken     Optional ngrok authtoken.
              -g, --game            Enable Kahoot-style game mode with timing and scores.
              --timer <seconds>     Time limit per question in game mode (default: 10).
              -h, --help            Show help.
            """);
    }

    public string ResolveQuizPath()
    {
        if (Path.IsPathRooted(QuizPath) && File.Exists(QuizPath)) return QuizPath;

        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            var candidate = Path.GetFullPath(Path.Combine(current.FullName, QuizPath));
            if (File.Exists(candidate)) return candidate;
            current = current.Parent;
        }

        throw new FileNotFoundException($"Could not find quiz file '{QuizPath}'.");
    }
}
