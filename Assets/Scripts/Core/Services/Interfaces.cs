using System;
using TaskbarTactics.Core.Models;

namespace TaskbarTactics.Core.Services
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    public interface ISaveStore
    {
        void Save(GameState state);
        GameState LoadOrDefault();
    }

    public enum WindowMode
    {
        Strip,
        Management
    }

    public interface IWindowController
    {
        WindowMode CurrentMode { get; }
        void SetMode(WindowMode mode);
        void Reposition();
    }
}
