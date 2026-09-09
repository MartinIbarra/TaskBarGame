using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private const float HeroArtworkScaleMultiplier = 1.1f;
        private const float BossArtworkScaleMultiplier = 1.5f;
        private const float BossHealthBarScaleMultiplier = 1.5f;
        private const float BossHealthBarYOffset = 0.2f;
        private const float SpawnIntroSeconds = 0.72f;
        private const float SpawnIntroStaggerSeconds = 0.08f;
        private const float HeroSpawnIntroOffsetX = -1.25f;
        private const float EnemySpawnIntroOffsetX = 1.25f;
        private const int SilverDropAnimationFrames = 26;
        private const float SilverDropJumpHeight = 0.52f;
        private const float SilverDropScaleMultiplier = 0.765f;
        private const float SilverDropApexScaleMultiplier = 0.55f;
        private const float SilverDropSoundVolume = 0.56f;
        private const float ChestOpenEffectYOffset = 0.16f;
        private const float ChestOpenEffectFrameSeconds = 0.12f;
        private const float RewardItemPopupDurationSeconds = 1.15f;

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
        private Sprite[] chestOpenEffectFrames;
        private Sprite[] silverCoinFrames;
        private Sprite[] silverCoinShineFrames;
        private AudioClip silverDropClip;

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
            string nodeId = null,
            IReadOnlyCollection<string> silverDropEnemyIds = null,
            Action onSilverCollected = null)
        {
            SetBattleback(nodeId);
            List<SpawnIntroEntry> spawnIntro = SpawnUnits(request, catalog);
            yield return PlaySpawnIntro(spawnIntro);
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
                    PlayEvent(combatEvent, silverDropEnemyIds, onSilverCollected);
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

        private void PlayEvent(
            CombatEvent combatEvent,
            IReadOnlyCollection<string> silverDropEnemyIds,
            Action onSilverCollected)
        {
            if (combatEvent.Kind == CombatEventKind.Healing)
            {
                if (unitViews.TryGetValue(combatEvent.ActorId, out UnitView healer))
                {
                    healer.PlaySkill();
                }

                if (unitViews.TryGetValue(combatEvent.TargetId, out UnitView healedTarget))
                {
                    healedTarget.ReceiveHealing(combatEvent.Amount);
                    StartCoroutine(PlayHealingEffect(healedTarget.transform));
                }

                return;
            }

            if (unitViews.TryGetValue(combatEvent.ActorId, out UnitView actor))
            {
                actor.PlayAttack();
            }

            if (unitViews.TryGetValue(combatEvent.TargetId, out UnitView target))
            {
                target.ReceiveDamage(combatEvent.Amount);
                if (target.IsDead &&
                    silverDropEnemyIds != null &&
                    silverDropEnemyIds.Contains(combatEvent.TargetId))
                {
                    StartCoroutine(PlaySilverDrop(target.transform.position, onSilverCollected));
                }
            }
        }

        private IEnumerator PlayHealingEffect(Transform target)
        {
            LoadSilverDropFrames();
            if (target == null || silverCoinShineFrames == null || silverCoinShineFrames.Length == 0)
            {
                yield break;
            }

            GameObject effectObject = new GameObject("Healing Effect");
            effectObject.transform.SetParent(target, false);
            effectObject.transform.localPosition = new Vector3(0f, 0.18f, -0.45f);
            effectObject.transform.localScale = Vector3.one * 0.85f;
            SpriteRenderer effectRenderer = effectObject.AddComponent<SpriteRenderer>();
            effectRenderer.sortingOrder = 42;

            const float frameSeconds = 0.08f;
            for (int frame = 0; frame < silverCoinShineFrames.Length * 2; frame++)
            {
                effectRenderer.sprite = silverCoinShineFrames[frame % silverCoinShineFrames.Length];
                yield return new WaitForSecondsRealtime(frameSeconds);
            }

            Destroy(effectObject);
        }

        private IEnumerator PlaySilverDrop(Vector3 startPosition, Action onCollected)
        {
            LoadSilverDropFrames();
            if (silverCoinFrames == null || silverCoinFrames.Length == 0)
            {
                onCollected?.Invoke();
                yield break;
            }

            GameObject coinObject = new GameObject("Silver Coin Drop");
            coinObject.transform.position = startPosition + new Vector3(0f, 0.08f, -0.4f);
            coinObject.transform.localScale = Vector3.one * SilverDropScaleMultiplier;
            SpriteRenderer coinRenderer = coinObject.AddComponent<SpriteRenderer>();
            coinRenderer.sprite = silverCoinFrames[0];
            coinRenderer.sortingOrder = 40;
            SpriteRenderer shineRenderer = null;
            if (silverCoinShineFrames != null && silverCoinShineFrames.Length > 0)
            {
                GameObject shineObject = new GameObject("Silver Coin Shine");
                shineObject.transform.SetParent(coinObject.transform, false);
                shineRenderer = shineObject.AddComponent<SpriteRenderer>();
                shineRenderer.sprite = silverCoinShineFrames[0];
                shineRenderer.sortingOrder = 41;
            }

            const int midpointFrame = SilverDropAnimationFrames / 2;
            int soundFrame = Mathf.Max(1, midpointFrame / 2);
            for (int frame = 0; frame <= midpointFrame; frame++)
            {
                float normalized = frame / (float)midpointFrame;
                float jump = normalized * (2f - normalized);
                float shrink = Mathf.SmoothStep(0f, 1f, normalized * normalized);
                coinObject.transform.position = startPosition + new Vector3(
                    0f,
                    0.08f + SilverDropJumpHeight * jump,
                    -0.4f);
                float scale = Mathf.Lerp(
                    SilverDropScaleMultiplier,
                    SilverDropScaleMultiplier * SilverDropApexScaleMultiplier,
                    shrink);
                coinObject.transform.localScale = Vector3.one * scale;
                coinRenderer.sprite = silverCoinFrames[frame % silverCoinFrames.Length];
                if (shineRenderer != null)
                {
                    shineRenderer.sprite = silverCoinShineFrames[frame % silverCoinShineFrames.Length];
                }

                if (frame == soundFrame)
                {
                    PlaySilverDropSound();
                }

                if (frame == midpointFrame)
                {
                    onCollected?.Invoke();
                    Destroy(coinObject);
                    yield break;
                }

                yield return null;
            }
        }

        private void PlaySilverDropSound()
        {
            silverDropClip = silverDropClip != null
                ? silverDropClip
                : Resources.Load<AudioClip>("Audio/Events/coin");
            if (silverDropClip == null)
            {
                return;
            }

            AudioSource source = FindFirstObjectByType<AudioSource>();
            if (source == null)
            {
                GameObject audioObject = new GameObject("Silver Coin Audio");
                source = audioObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                Destroy(audioObject, silverDropClip.length + 0.1f);
            }

            source.PlayOneShot(silverDropClip, SilverDropSoundVolume);
        }

        private void LoadSilverDropFrames()
        {
            if (silverCoinFrames == null)
            {
                silverCoinFrames = CreateHorizontalFrames(
                    Resources.Load<Texture2D>("UI/SilverCoinFrames"), 8);
            }

            if (silverCoinShineFrames == null)
            {
                silverCoinShineFrames = CreateVerticalFrames(
                    Resources.Load<Texture2D>("UI/SilverCoinDropFrames"), 4);
            }
        }

        private static Sprite[] CreateHorizontalFrames(Texture2D texture, int frameCount)
        {
            return CreateFrames(texture, frameCount, false);
        }

        private static Sprite[] CreateVerticalFrames(Texture2D texture, int frameCount)
        {
            return CreateFrames(texture, frameCount, true);
        }

        private static Sprite[] CreateFrames(Texture2D texture, int frameCount, bool vertical)
        {
            if (texture == null || frameCount <= 0)
            {
                return Array.Empty<Sprite>();
            }

            List<Sprite> frames = new List<Sprite>(frameCount);
            float frameWidth = vertical ? texture.width : texture.width / (float)frameCount;
            float frameHeight = vertical ? texture.height / (float)frameCount : texture.height;
            for (int i = 0; i < frameCount; i++)
            {
                float x = vertical ? 0f : i * frameWidth;
                float y = vertical ? texture.height - (i + 1) * frameHeight : 0f;
                frames.Add(Sprite.Create(
                    texture,
                    new Rect(x, y, frameWidth, frameHeight),
                    new Vector2(0.5f, 0.5f),
                    100f));
            }

            return frames.ToArray();
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
            Sprite chestSizeReference = sprite;
            Vector3 targetScale = new Vector3(0.55f, 0.6655f, 1f);
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
                Sprite[] effectFrames = LoadChestOpenEffectFrames();
                SpriteRenderer effectRenderer = CreateChestOpenEffect(
                    chest.transform.position,
                    effectFrames);
                yield return PlayRewardChestOpening(
                    renderer,
                    openingFrames,
                    chest.transform,
                    targetScale,
                    chestSizeReference,
                    effectRenderer,
                    effectFrames,
                    rewardSprite);
                if (effectRenderer != null)
                {
                    Destroy(effectRenderer.gameObject);
                }
            }
            else if (rewardSprite != null)
            {
                yield return PlayRewardItemPopup(chest.transform.position, rewardSprite);
            }

            Destroy(chest);
        }

        private List<SpawnIntroEntry> SpawnUnits(CombatRequest request, GameContentCatalog catalog)
        {
            Clear();
            List<SpawnIntroEntry> spawnIntro = new List<SpawnIntroEntry>();
            foreach (CombatantState hero in request.Heroes)
            {
                UnitView view = Spawn(hero, heroCells, heroColumns);
                spawnIntro.Add(new SpawnIntroEntry(view, HeroSpawnIntroOffsetX));
                HeroDefinition definition = catalog.FindHero(hero.Id);
                view.Initialize(
                    definition != null ? definition.DisplayNameEs : hero.Id,
                    definition != null ? definition.Color : Color.cyan,
                    hero.MaxHealth,
                    definition != null ? definition.Artwork : null,
                    HeroArtworkScaleMultiplier,
                    false,
                    true,
                    $"Heroes/{LegacyHeroAnimationId(hero.Id)}");
                view.SetCurrentHealth(hero.CurrentHealth);
            }

            int bossOrdinal = 0;
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
                    isBoss
                        ? BossCellIndex(enemyCells, enemyColumns, bossOrdinal++)
                        : (int?)null);
                spawnIntro.Add(new SpawnIntroEntry(view, EnemySpawnIntroOffsetX));
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

            return spawnIntro;
        }

        private static IEnumerator PlaySpawnIntro(IReadOnlyList<SpawnIntroEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                yield break;
            }

            float elapsed = 0f;
            float totalSeconds = SpawnIntroSeconds + SpawnIntroStaggerSeconds * Mathf.Max(0, entries.Count - 1);
            foreach (SpawnIntroEntry entry in entries)
            {
                if (entry.View == null)
                {
                    continue;
                }

                entry.View.transform.localPosition = new Vector3(entry.OffsetX, 0f, 0f);
                entry.View.SetMovement(1f);
            }

            while (elapsed < totalSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                for (int i = 0; i < entries.Count; i++)
                {
                    SpawnIntroEntry entry = entries[i];
                    if (entry.View == null)
                    {
                        continue;
                    }

                    float unitElapsed = elapsed - i * SpawnIntroStaggerSeconds;
                    float t = Mathf.Clamp01(unitElapsed / SpawnIntroSeconds);
                    float eased = Smooth(t);
                    entry.View.transform.localPosition = Vector3.Lerp(
                        new Vector3(entry.OffsetX, 0f, 0f),
                        Vector3.zero,
                        eased);
                    entry.View.SetMovement(t < 1f ? 1f : 0f);
                }

                yield return null;
            }

            foreach (SpawnIntroEntry entry in entries)
            {
                if (entry.View == null)
                {
                    continue;
                }

                entry.View.transform.localPosition = Vector3.zero;
                entry.View.SetMovement(0f);
            }
        }

        private readonly struct SpawnIntroEntry
        {
            public readonly UnitView View;
            public readonly float OffsetX;

            public SpawnIntroEntry(UnitView view, float offsetX)
            {
                View = view;
                OffsetX = offsetX;
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

        private static int BossCellIndex(IReadOnlyList<Transform> cells, int columns, int bossOrdinal)
        {
            if (cells == null || cells.Count == 0)
            {
                return 0;
            }

            int safeColumns = Mathf.Max(1, columns);
            int row = Mathf.Max(0, Mathf.CeilToInt(cells.Count / (float)safeColumns) - 1);
            int column = Mathf.Clamp(bossOrdinal, 0, safeColumns - 1);
            return Mathf.Clamp(row * safeColumns + column, 0, cells.Count - 1);
        }

        private static float Smooth(float value)
        {
            return value * value * (3f - 2f * value);
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

            battlebackRenderer.sprite = LoadBattleback(BattlebackIdForNode(nodeId)) ?? LoadBattleback("default");
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

        private static string BattlebackIdForNode(string nodeId)
        {
            switch (nodeId)
            {
                case "mt_secret":
                case "mountain_pass_act2":
                    return "cave";
                case "ancient_ruins":
                case "black_tower":
                    return "last_bastion";
                case "city2":
                case "corrupt_pass":
                case "lo_hueso":
                case "arbol_morto":
                case "port":
                case "lost_bay":
                    return "corruptland";
                default:
                    return nodeId;
            }
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

        private IEnumerator PlayRewardChestOpening(
            SpriteRenderer renderer,
            IReadOnlyList<Sprite> frames,
            Transform chest,
            Vector3 baseScale,
            Sprite sizeReference,
            SpriteRenderer effectRenderer,
            IReadOnlyList<Sprite> effectFrames,
            Sprite rewardSprite)
        {
            if (effectRenderer != null)
            {
                effectRenderer.gameObject.SetActive(false);
            }

            for (int i = 0; i < frames.Count; i++)
            {
                Sprite frame = frames[i];
                if (frame != null)
                {
                    renderer.sprite = frame;
                    ApplyRewardChestFrameScale(chest, baseScale, sizeReference, frame);
                }

                yield return new WaitForSecondsRealtime(ChestOpenEffectFrameSeconds);
            }

            yield return new WaitForSecondsRealtime(ChestOpenEffectFrameSeconds * 2f);

            if (effectRenderer == null || effectFrames == null || effectFrames.Count == 0)
            {
                if (rewardSprite != null)
                {
                    yield return PlayRewardItemPopup(chest.position, rewardSprite);
                }

                yield break;
            }

            effectRenderer.gameObject.SetActive(true);
            if (rewardSprite != null)
            {
                StartCoroutine(PlayRewardItemPopup(chest.position, rewardSprite));
            }

            for (int i = 0; i < effectFrames.Count; i++)
            {
                effectRenderer.sprite = effectFrames[i];
                yield return new WaitForSecondsRealtime(ChestOpenEffectFrameSeconds);
            }

            float effectDuration = effectFrames.Count * ChestOpenEffectFrameSeconds;
            float remainingPopupDuration = RewardItemPopupDurationSeconds - effectDuration;
            if (rewardSprite != null && remainingPopupDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(remainingPopupDuration);
            }
        }

        private Sprite[] LoadChestOpenEffectFrames()
        {
            if (chestOpenEffectFrames == null)
            {
                chestOpenEffectFrames = CreateHorizontalFrames(
                    Resources.Load<Texture2D>("Events/Chest/chestopen"), 5);
            }

            return chestOpenEffectFrames;
        }

        private static SpriteRenderer CreateChestOpenEffect(
            Vector3 position,
            IReadOnlyList<Sprite> frames)
        {
            if (frames == null || frames.Count == 0 || frames[0] == null)
            {
                return null;
            }

            GameObject effect = new GameObject("Chest Open Effect");
            effect.transform.position = position + new Vector3(0f, ChestOpenEffectYOffset, 0.1f);
            effect.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
            SpriteRenderer renderer = effect.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.sortingOrder = 29;
            return renderer;
        }

        private static void ApplyRewardChestFrameScale(
            Transform chest,
            Vector3 baseScale,
            Sprite sizeReference,
            Sprite frame)
        {
            if (chest == null || sizeReference == null || frame == null)
            {
                return;
            }

            float referenceHeight = Mathf.Max(0.01f, sizeReference.bounds.size.y);
            float frameHeight = Mathf.Max(0.01f, frame.bounds.size.y);
            chest.localScale = new Vector3(
                baseScale.x,
                baseScale.y * (referenceHeight / frameHeight),
                baseScale.z);
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
            Vector3 expandedScale = new Vector3(0.9f, 0.9f, 1f);
            Vector3 fadeScale = new Vector3(0.945f, 0.945f, 1f);
            const float riseSeconds = 0.35f;
            const float holdSeconds = 0.55f;
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
                float t = Mathf.Clamp01(elapsed / holdSeconds);
                popup.transform.position = floatPosition + new Vector3(0f, Mathf.Sin(elapsed * 16f) * 0.015f, 0f);
                popup.transform.localScale = Vector3.Lerp(
                    targetScale,
                    expandedScale,
                    Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            popup.transform.localScale = expandedScale;
            elapsed = 0f;
            Color color = renderer.color;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeSeconds);
                color.a = 1f - t;
                renderer.color = color;
                popup.transform.localScale = Vector3.Lerp(
                    expandedScale,
                    fadeScale,
                    t);
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
