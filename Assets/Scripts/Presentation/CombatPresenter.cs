using System.Collections;
using System.Collections.Generic;
using TaskbarTactics.Content;
using TaskbarTactics.Core.Combat;
using TaskbarTactics.Core.Models;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TaskbarTactics.Presentation
{
    public sealed class CombatPresenter : MonoBehaviour
    {
        private const float FinalBossDeathVolume = 0.7f;

        [Header("Reusable presentation")]
        [SerializeField] private UnitView unitPrefab;
        [SerializeField] private List<Transform> heroCells = new List<Transform>();
        [SerializeField] private List<Transform> enemyCells = new List<Transform>();
        [SerializeField, Min(1)] private int heroColumns = 4;
        [SerializeField, Min(1)] private int enemyColumns = 3;
        [SerializeField] private SpriteRenderer battlebackRenderer;
        [SerializeField] private Color enemyColor = new Color(0.78f, 0.24f, 0.25f);
        [SerializeField] private AudioClip finalBossDeathClip;
        [SerializeField, Min(0.1f), Tooltip("Timeline playback multiplier. Higher values replay combat faster.")]
        private float playbackSpeed = 1f;

        private readonly Dictionary<string, UnitView> unitViews = new Dictionary<string, UnitView>();
        private readonly Dictionary<string, Sprite> battlebackCache = new Dictionary<string, Sprite>();
        private readonly HashSet<string> enemyViewIds = new HashSet<string>();
        private Sprite rewardChestSprite;

        public void Configure(
            UnitView prefab,
            IEnumerable<Transform> heroes,
            IEnumerable<Transform> enemies,
            SpriteRenderer battleback = null)
        {
            Configure(prefab, heroes, enemies, battleback, 4, 3);
        }

        public void Configure(
            UnitView prefab,
            IEnumerable<Transform> heroes,
            IEnumerable<Transform> enemies,
            SpriteRenderer battleback,
            int heroColumnCount,
            int enemyColumnCount)
        {
            unitPrefab = prefab;
            heroCells = new List<Transform>(heroes);
            enemyCells = new List<Transform>(enemies);
            battlebackRenderer = battleback;
            heroColumns = Mathf.Max(1, heroColumnCount);
            enemyColumns = Mathf.Max(1, enemyColumnCount);
        }

        public IEnumerator Play(
            CombatRequest request,
            CombatResult result,
            GameContentCatalog catalog,
            float durationSeconds,
            string nodeId = null)
        {
            SetBattleback(nodeId);
            SpawnUnits(request, catalog);
            CombatPlaybackSchedule schedule = CombatPlaybackTimeline.Build(
                result.Events,
                result.ElapsedMilliseconds,
                durationSeconds,
                playbackSpeed);
            foreach (CombatPlaybackFrame frame in schedule.Frames)
            {
                if (frame.DelaySeconds > 0f)
                {
                    yield return new WaitForSecondsRealtime(frame.DelaySeconds);
                }

                foreach (CombatEvent combatEvent in frame.Events)
                {
                    PlayEvent(combatEvent);
                }
            }

            if (schedule.TailDelaySeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(schedule.TailDelaySeconds);
            }

            if (result.Outcome == CombatOutcome.Victory && HasBossEnemy(request, catalog))
            {
                PlayFinalBossDeathSound();
            }
        }

        private void PlayEvent(CombatEvent combatEvent)
        {
            if (unitViews.TryGetValue(combatEvent.ActorId, out UnitView actor))
            {
                actor.PlayAttack();
            }

            if (unitViews.TryGetValue(combatEvent.TargetId, out UnitView target))
            {
                target.ReceiveDamage(combatEvent.Amount);
            }
        }

        public void Clear()
        {
            foreach (UnitView view in unitViews.Values)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }

            unitViews.Clear();
            enemyViewIds.Clear();
        }

        public void ShowBattleback(string nodeId = null)
        {
            SetBattleback(nodeId);
        }

        public IEnumerator ShowRewardChest(float timeoutSeconds = 5f)
        {
            ClearEnemies();
            Sprite sprite = LoadRewardChest();
            if (sprite == null || enemyCells.Count == 0)
            {
                yield return new WaitForSecondsRealtime(timeoutSeconds);
                yield break;
            }

            GameObject chest = new GameObject("Reward Chest");
            int cellIndex = Mathf.Clamp(1, 0, enemyCells.Count - 1);
            chest.transform.position = enemyCells[cellIndex].position + new Vector3(0f, -0.05f, -0.2f);
            SpriteRenderer renderer = chest.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 30;
            Vector3 targetScale = new Vector3(0.55f, 0.55f, 1f);
            float emergeSeconds = 0.35f;
            float elapsed = 0f;
            while (elapsed < emergeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / emergeSeconds);
                chest.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
                yield return null;
            }

            chest.transform.localScale = targetScale;
            float deadline = Time.unscaledTime + timeoutSeconds;
            while (Time.unscaledTime < deadline)
            {
                if (Mouse.current != null &&
                    Mouse.current.leftButton.wasPressedThisFrame &&
                    IsPointerOver(chest, renderer))
                {
                    break;
                }

                yield return null;
            }

            Destroy(chest);
        }

        private void SpawnUnits(CombatRequest request, GameContentCatalog catalog)
        {
            Clear();
            foreach (CombatantState hero in request.Heroes)
            {
                UnitView view = Spawn(hero, heroCells, heroColumns);
                HeroDefinition definition = catalog.FindHero(hero.Id);
                view.Initialize(
                    definition != null ? definition.DisplayNameEs : hero.Id,
                    definition != null ? definition.Color : Color.cyan,
                    hero.MaxHealth,
                    definition != null ? definition.Artwork : null,
                    1f,
                    false,
                    true,
                    $"Heroes/{LegacyHeroAnimationId(hero.Id)}");
            }

            foreach (CombatantState enemy in request.Enemies)
            {
                UnitView view = Spawn(enemy, enemyCells, enemyColumns);
                enemyViewIds.Add(enemy.Id);
                string definitionId = enemy.Id.Split('-')[0];
                EnemyDefinition definition = catalog.FindEnemy(definitionId);
                view.Initialize(
                    definition != null ? definition.DisplayNameEs : definitionId,
                    enemyColor,
                    enemy.MaxHealth,
                    definition != null ? definition.Artwork : null,
                    EnemyArtworkScale(definitionId, definition),
                    true,
                    true,
                    $"Enemies/{definitionId}");
            }
        }

        private static float EnemyArtworkScale(string enemyId, EnemyDefinition definition)
        {
            if (enemyId == "wolf")
            {
                return 0.8f;
            }

            return definition != null && definition.IsBoss ? 1.1f : 1f;
        }

        private static string LegacyHeroAnimationId(string heroId)
        {
            switch (heroId)
            {
                case "warrior": return "guardian";
                case "mage": return "pyromancer";
                case "archer": return "ranger";
                case "magic_warrior": return "spellblade";
                default: return heroId;
            }
        }

        private static bool HasBossEnemy(CombatRequest request, GameContentCatalog catalog)
        {
            if (request == null || catalog == null)
            {
                return false;
            }

            foreach (CombatantState enemy in request.Enemies)
            {
                string definitionId = enemy.Id.Split('-')[0];
                EnemyDefinition definition = catalog.FindEnemy(definitionId);
                if (definition != null && definition.IsBoss)
                {
                    return true;
                }
            }

            return false;
        }

        private void PlayFinalBossDeathSound()
        {
            finalBossDeathClip = finalBossDeathClip != null
                ? finalBossDeathClip
                : Resources.Load<AudioClip>("Audio/Events/boss_death_hurr");
            if (finalBossDeathClip == null)
            {
                return;
            }

            AudioSource source = FindFirstObjectByType<AudioSource>();
            if (source != null)
            {
                source.PlayOneShot(finalBossDeathClip, FinalBossDeathVolume);
            }
        }

        private UnitView Spawn(CombatantState state, IReadOnlyList<Transform> cells, int columns)
        {
            int index = Mathf.Clamp(state.Position.Row * columns + state.Position.Column, 0, cells.Count - 1);
            UnitView view = Instantiate(unitPrefab, cells[index]);
            view.transform.localPosition = Vector3.zero;
            unitViews[state.Id] = view;
            return view;
        }

        private void ClearEnemies()
        {
            foreach (string enemyId in new List<string>(enemyViewIds))
            {
                if (unitViews.TryGetValue(enemyId, out UnitView view) && view != null)
                {
                    Destroy(view.gameObject);
                }

                unitViews.Remove(enemyId);
            }

            enemyViewIds.Clear();
        }

        private void SetBattleback(string nodeId)
        {
            if (battlebackRenderer == null)
            {
                return;
            }

            battlebackRenderer.sprite = LoadBattleback(nodeId) ?? LoadBattleback("default");
            battlebackRenderer.color = battlebackRenderer.sprite == null
                ? Color.clear
                : Color.white;
            battlebackRenderer.transform.localScale = Vector3.one;
        }

        private Sprite LoadBattleback(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                return null;
            }

            if (battlebackCache.TryGetValue(nodeId, out Sprite cached))
            {
                return cached;
            }

            Texture2D texture = Resources.Load<Texture2D>($"Battlebacks/{nodeId}");
            if (texture == null)
            {
                battlebackCache[nodeId] = null;
                return null;
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            battlebackCache[nodeId] = sprite;
            return sprite;
        }

        private Sprite LoadRewardChest()
        {
            if (rewardChestSprite != null)
            {
                return rewardChestSprite;
            }

            Texture2D texture = Resources.Load<Texture2D>("Events/Chest/cofre");
            if (texture == null)
            {
                return null;
            }

            rewardChestSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            return rewardChestSprite;
        }

        private static bool IsPointerOver(GameObject target, SpriteRenderer renderer)
        {
            Camera camera = Camera.main;
            if (camera == null || target == null || renderer == null)
            {
                return false;
            }

            Vector2 screenPoint = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            Vector3 worldPoint = camera.ScreenToWorldPoint(screenPoint);
            worldPoint.z = target.transform.position.z;
            return renderer.bounds.Contains(worldPoint);
        }

    }
}
