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
        private const float BossArtworkScaleMultiplier = 1.25f;
        private const float BossHealthBarScaleMultiplier = 1.5f;
        private const float BossHealthBarYOffset = 0.2f;

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
        private Sprite[] rewardChestOpeningFrames;

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

        public IEnumerator ShowRewardChest(string rewardItemId = null, float timeoutSeconds = 5f)
        {
            ClearEnemies();
            Sprite[] openingFrames = LoadRewardChestOpeningFrames();
            Sprite sprite = openingFrames.Length > 0 ? openingFrames[0] : LoadRewardChest();
            Sprite rewardSprite = LoadRewardItemSprite(rewardItemId);
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

            if (openingFrames.Length > 1)
            {
                yield return PlayRewardChestOpening(renderer, openingFrames);
            }

            if (rewardSprite != null)
            {
                yield return PlayRewardItemPopup(chest.transform.position, rewardSprite);
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
                enemyViewIds.Add(enemy.Id);
                string definitionId = enemy.Id.Split('-')[0];
                EnemyDefinition definition = catalog.FindEnemy(definitionId);
                bool isBoss = definition != null && definition.IsBoss;
                UnitView view = Spawn(
                    enemy,
                    enemyCells,
                    enemyColumns,
                    isBoss ? BossCellIndex(enemyCells, enemyColumns) : (int?)null);
                view.Initialize(
                    definition != null ? definition.DisplayNameEs : definitionId,
                    enemyColor,
                    enemy.MaxHealth,
                    definition != null ? definition.Artwork : null,
                    EnemyArtworkScale(definitionId, definition),
                    true,
                    true,
                    $"Enemies/{definitionId}",
                    isBoss ? BossHealthBarScaleMultiplier : 1f,
                    isBoss ? BossHealthBarYOffset : 0f);
            }
        }

        private static float EnemyArtworkScale(string enemyId, EnemyDefinition definition)
        {
            if (enemyId == "wolf")
            {
                return 0.8f;
            }

            return definition != null && definition.IsBoss ? BossArtworkScaleMultiplier : 1f;
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

        private UnitView Spawn(
            CombatantState state,
            IReadOnlyList<Transform> cells,
            int columns,
            int? forcedCellIndex = null)
        {
            int requestedIndex = forcedCellIndex ?? state.Position.Row * columns + state.Position.Column;
            int index = Mathf.Clamp(requestedIndex, 0, cells.Count - 1);
            UnitView view = Instantiate(unitPrefab, cells[index]);
            view.transform.localPosition = Vector3.zero;
            unitViews[state.Id] = view;
            return view;
        }

        private static int BossCellIndex(IReadOnlyList<Transform> cells, int columns)
        {
            if (cells == null || cells.Count == 0)
            {
                return 0;
            }

            int safeColumns = Mathf.Max(1, columns);
            int row = Mathf.Max(0, Mathf.CeilToInt(cells.Count / (float)safeColumns) - 1);
            int column = safeColumns / 2;
            return Mathf.Clamp(row * safeColumns + column, 0, cells.Count - 1);
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

        private Sprite[] LoadRewardChestOpeningFrames()
        {
            if (rewardChestOpeningFrames != null)
            {
                return rewardChestOpeningFrames;
            }

            List<Sprite> frames = new List<Sprite>();
            for (int i = 1; i <= 4; i++)
            {
                Texture2D texture = Resources.Load<Texture2D>($"Events/Chest/chest{i}");
                if (texture == null)
                {
                    continue;
                }

                frames.Add(Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f));
            }

            rewardChestOpeningFrames = frames.ToArray();
            return rewardChestOpeningFrames;
        }

        private static Sprite LoadRewardItemSprite(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            Texture2D texture = Resources.Load<Texture2D>($"Items/{itemId}");
            if (texture == null)
            {
                return null;
            }

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        private static IEnumerator PlayRewardChestOpening(SpriteRenderer renderer, IReadOnlyList<Sprite> frames)
        {
            const float frameSeconds = 0.12f;
            foreach (Sprite frame in frames)
            {
                if (frame != null)
                {
                    renderer.sprite = frame;
                }

                yield return new WaitForSecondsRealtime(frameSeconds);
            }
        }

        private static IEnumerator PlayRewardItemPopup(Vector3 chestPosition, Sprite rewardSprite)
        {
            GameObject popup = new GameObject("Reward Item Popup");
            SpriteRenderer renderer = popup.AddComponent<SpriteRenderer>();
            renderer.sprite = rewardSprite;
            renderer.sortingOrder = 36;

            Vector3 start = chestPosition + new Vector3(0f, 0.12f, -0.08f);
            Vector3 floatPosition = chestPosition + new Vector3(0f, 0.48f, -0.08f);
            Vector3 targetScale = new Vector3(0.78f, 0.78f, 1f);
            const float riseSeconds = 0.35f;
            const float holdSeconds = 0.35f;
            const float fadeSeconds = 0.25f;

            float elapsed = 0f;
            while (elapsed < riseSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / riseSeconds);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                popup.transform.position = Vector3.Lerp(start, floatPosition, eased);
                popup.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, eased);
                yield return null;
            }

            popup.transform.position = floatPosition;
            popup.transform.localScale = targetScale;
            elapsed = 0f;
            while (elapsed < holdSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                popup.transform.position = floatPosition + new Vector3(0f, Mathf.Sin(elapsed * 16f) * 0.015f, 0f);
                yield return null;
            }

            elapsed = 0f;
            Color color = renderer.color;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeSeconds);
                color.a = 1f - t;
                renderer.color = color;
                popup.transform.position += new Vector3(0f, Time.unscaledDeltaTime * 0.12f, 0f);
                yield return null;
            }

            Destroy(popup);
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
