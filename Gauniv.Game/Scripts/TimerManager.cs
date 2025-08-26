using System;
using System.Collections.Generic;
using Godot;

public partial class TimerManager : Node
{
    private static double _elapsedTime = 0;
    private static bool _timerRunning = false;
    private static List<Label> _timerLabels = new();

    // Event handler for timer updates
    public delegate void TimerUpdateHandler(double time);
    public static event TimerUpdateHandler OnTimerUpdate;

    public static void Update(double delta)
    {
        if (_timerRunning)
        {
            _elapsedTime += delta;
            OnTimerUpdate?.Invoke(_elapsedTime);

            foreach (var label in _timerLabels)
            {
                label.Text = _elapsedTime.ToString("F3");
            }
        }
    }

    public static void RegisterTimerLabel(Label label)
    {
        if (!_timerLabels.Contains(label))
        {
            _timerLabels.Add(label);
            label.Text = _elapsedTime.ToString("F3");
        }
    }

    public static void UnregisterTimerLabel(Label label)
    {
        _timerLabels.Remove(label);
    }

    public static void Start()
    {
        _elapsedTime = 0;
        _timerRunning = true;
    }

    public static void Stop()
    {
        _timerRunning = false;
    }

    public static void Reset()
    {
        _elapsedTime = 0;
        foreach (var label in _timerLabels)
        {
            label.Text = "0.000";
        }
        _timerRunning = false;
    }

    public static double GetElapsedTime()
    {
        return Math.Round(_elapsedTime, 3);
    }

    public static bool IsRunning()
    {
        return _timerRunning;
    }
}
