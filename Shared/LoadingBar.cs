using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NLang.DevelopmentKit.Shared;

public class LoadingBar : IDisposable
{
    public static double DefaultRefreshRate { get; set; } = 4;

    public int PartsDone
    {
        get => _partsDone;
        set
        {
            _partsDone = value;
            if (!continuous) Update();
        }
    }
    public int PartsTotal
    {
        get => _partsTotal;
        set
        {
            _partsTotal = value;
            if (!continuous) Update();
        }
    }
    public string? FinalText { get; set; }
    public bool Failed { get; set; } = false;
    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            if (!continuous) Update();
        }
    }
    public LoadingBarColor Color
    {
        get => _color;
        set
        {
            _color = value;
            if (!continuous) Update();
        }
    }
    public TimeSpan RefreshTime { get; init; }
    public DateTime StartTime { get; private set; }

    private int _partsDone, _partsTotal;
    private string _text;
    private LoadingBarColor _color;

    private bool disposed;
    private int previousWidth;
    private readonly bool continuous;
    private readonly int lineNumber;
    private readonly CancellationTokenSource? taskCancelToken;
    private readonly object LOCK;

    public LoadingBar(LoadingBarColor color) : this(DefaultRefreshRate, color) { }
    public LoadingBar(double refreshRate, LoadingBarColor color)
    {
        FinalText = null;
        _text = string.Empty;
        _partsDone = 0;
        _partsTotal = 1;
        _color = color;
        StartTime = DateTime.Now;

        disposed = false;
        lineNumber = Console.CursorTop;
        previousWidth = 0;
        LOCK = new();

        if (refreshRate <= 1e-3)
        {
            RefreshTime = TimeSpan.Zero;
            continuous = false;
            taskCancelToken = null;
            Update();
        }
        else
        {
            RefreshTime = TimeSpan.FromSeconds(1 / refreshRate);
            continuous = true;
            taskCancelToken = new();
            Task.Run(UpdateLoop, taskCancelToken.Token);
        }
    }

    public void Update()
    {
        lock (LOCK)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            lock (Console.Out)
            {
                Console.CursorVisible = false;
                int left = Console.CursorLeft, top = Console.CursorTop;
                Console.SetCursorPosition(0, lineNumber);
                int newTotalWidth = (int)(0.65 * Console.WindowWidth - 1);
                double value = (double)PartsDone / PartsTotal;

                StringBuilder bar = new();
                bar.Append($" {PartsDone} / {PartsTotal}  ({value * 100:0.0}%)  {Text} ");

                TimeSpan duration = DateTime.Now - StartTime;
                string footer = $" {FormatBasic(duration)} ";
                int barSpace = newTotalWidth - Math.Max(11, footer.Length) - 2; // 2 for the [ and the ]

                if (bar.Length > barSpace)
                {
                    // Trim excess.
                    bar.Remove(barSpace - 4, bar.Length - barSpace + 4);
                    bar.Append("... ");
                }
                else
                {
                    // Fill additional space.
                    bar.Append(new string(' ', barSpace - bar.Length));
                }

                int barPoint = (int)(barSpace * value);
                if (barPoint < barSpace) bar.Insert(barPoint, Color.GetBackFormat());
                if (barPoint > 0) bar.Insert(0, Color.GetFrontFormat());
                bar.Append("\x1b[0m");

                StringBuilder total = new StringBuilder()
                    .Append('[').Append(bar).Append("]\x1b[3;93m").Append($"{footer,11}").Append("\x1b[0m");
                int delta = previousWidth - newTotalWidth;
                if (delta > 0) total.Append(new string(' ', delta));
                Console.Write(total);

                Console.SetCursorPosition(left, top);
                previousWidth = newTotalWidth;
                Console.CursorVisible = true;
            }
        }
    }

    private async Task UpdateLoop()
    {
        // Simple and not super accurate but fine for a loading bar.
        while (!taskCancelToken!.IsCancellationRequested)
        {
            Update();
            await Task.Delay(RefreshTime);
        }
    }

    public void Dispose()
    {
        if (continuous) taskCancelToken!.Cancel();
        lock (LOCK)
        {
            if (disposed) return;

            GC.SuppressFinalize(this);
            disposed = true;

            lock (Console.Out)
            {
                int left = Console.CursorLeft, top = Console.CursorTop;

                // If done correctly this could be done in one print statement,
                // but I didn't feel like hard-coding indices to insert formatting
                // text.
                Console.SetCursorPosition(0, lineNumber);
                Console.Write("\x1b[0m" + new string(' ', previousWidth));

                TimeSpan duration = DateTime.Now - StartTime;
                Console.SetCursorPosition(0, lineNumber);
                string textFormat = Failed ? "\x1b[1;91m" : "\x1b[1;97m";
                Console.Write($"  {textFormat}{FinalText ?? Text}\x1b[22;37m in \x1b[3;93m{FormatBasic(duration)}\x1b[0m");

                if (top == lineNumber) Console.WriteLine();
                else Console.SetCursorPosition(left, top);
            }
        }
    }

    // If we ever use this elsewhere, we should move this to an extension file in a `.Extensions` namespace.
    // But I can't justify a whole namespace for one function.
    private static string FormatBasic(TimeSpan span)
    {
        if (span.TotalHours >= 1) return $"{span.TotalHours:0.0}hr";
        else if (span.TotalMinutes >= 1) return $"{span.Minutes:0}m:{span.Seconds:00}s";
        else if (span.TotalSeconds >= 1) return $"{span.TotalSeconds:0.000}s";
        else if (span.TotalMilliseconds >= 1) return $"{span.TotalMilliseconds:0.00}ms";
        else return $"{span.TotalNanoseconds:0}ns";
    }
}
