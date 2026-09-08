using System;
using System.IO;
using TaskbarTactics.Core.Models;
using TaskbarTactics.Core.Services;
using UnityEngine;

namespace TaskbarTactics.Infrastructure.Persistence
{
    public sealed class JsonSaveStore : ISaveStore
    {
        private const string SaveFileName = "profile.json";
        private const string BackupFileName = "profile.backup.json";
        private const string TemporaryFileName = "profile.tmp.json";

        private readonly string directory;

        public string PrimaryPath => Path.Combine(directory, SaveFileName);
        public string BackupPath => Path.Combine(directory, BackupFileName);

        public JsonSaveStore(string directory)
        {
            this.directory = directory ?? throw new ArgumentNullException(nameof(directory));
        }

        public void Save(GameState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            Directory.CreateDirectory(directory);
            state.Version = GameState.CurrentVersion;
            state.LastSavedUtcTicks = DateTime.UtcNow.Ticks;
            string temporaryPath = Path.Combine(directory, TemporaryFileName);
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(state, true));

            if (File.Exists(PrimaryPath))
            {
                File.Copy(PrimaryPath, BackupPath, true);
            }

            File.Copy(temporaryPath, PrimaryPath, true);
            File.Delete(temporaryPath);
        }

        public GameState LoadOrDefault()
        {
            GameState state = TryLoad(PrimaryPath) ?? TryLoad(BackupPath);
            return state ?? GameState.CreateDefault();
        }

        private static GameState TryLoad(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                GameState state = JsonUtility.FromJson<GameState>(File.ReadAllText(path));
                if (state == null || state.Version > GameState.CurrentVersion ||
                    state.Party == null ||
                    state.Inventory == null || state.Expedition == null)
                {
                    return null;
                }

                if (state.Version < 2)
                {
                    return GameState.CreateDefault();
                }

                if (state.Version < GameState.CurrentVersion)
                {
                    state.Silver = 0;
                    state.Version = GameState.CurrentVersion;
                }

                return state;
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is IOException ||
                exception is UnauthorizedAccessException)
            {
                return null;
            }
        }
    }
}
