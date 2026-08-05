using System.Collections;
using System.Collections.Generic;
using TaskbarTactics.Content;
using TaskbarTactics.Core.Combat;
using TaskbarTactics.Core.Models;
using UnityEngine;

namespace TaskbarTactics.Presentation
{
    public sealed class CombatPresenter : MonoBehaviour
    {
        [Header("Reusable presentation")]
        [SerializeField] private UnitView unitPrefab;
        [SerializeField] private List<Transform> heroCells = new List<Transform>();
        [SerializeField] private List<Transform> enemyCells = new List<Transform>();
        [SerializeField] private SpriteRenderer battlebackRenderer;
        [SerializeField] private Color enemyColor = new Color(0.78f, 0.24f, 0.25f);

        private readonly Dictionary<string, UnitView> unitViews = new Dictionary<string, UnitView>();
        private readonly Dictionary<string, Sprite> battlebackCache = new Dictionary<string, Sprite>();

        public void Configure(
            UnitView prefab,
            IEnumerable<Transform> heroes,
            IEnumerable<Transform> enemies,
            SpriteRenderer battleback = null)
        {
            unitPrefab = prefab;
            heroCells = new List<Transform>(heroes);
            enemyCells = new List<Transform>(enemies);
            battlebackRenderer = battleback;
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
            float eventDelay = result.Events.Count > 0
                ? Mathf.Max(0.04f, durationSeconds / result.Events.Count)
                : durationSeconds;

            foreach (CombatEvent combatEvent in result.Events)
            {
                if (unitViews.TryGetValue(combatEvent.ActorId, out UnitView actor))
                {
                    actor.PlayAttack();
                }

                if (unitViews.TryGetValue(combatEvent.TargetId, out UnitView target))
                {
                    target.ReceiveDamage(combatEvent.Amount);
                }

                yield return new WaitForSecondsRealtime(eventDelay);
            }

            if (result.Events.Count == 0)
            {
                yield return new WaitForSecondsRealtime(durationSeconds);
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
        }

        public void ShowBattleback(string nodeId = null)
        {
            SetBattleback(nodeId);
        }

        private void SpawnUnits(CombatRequest request, GameContentCatalog catalog)
        {
            Clear();
            foreach (CombatantState hero in request.Heroes)
            {
                UnitView view = Spawn(hero, heroCells);
                HeroDefinition definition = catalog.FindHero(hero.Id);
                view.Initialize(
                    definition != null ? definition.DisplayNameEs : hero.Id,
                    definition != null ? definition.Color : Color.cyan,
                    hero.MaxHealth,
                    definition != null ? definition.Artwork : null);
            }

            foreach (CombatantState enemy in request.Enemies)
            {
                UnitView view = Spawn(enemy, enemyCells);
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

            return definition != null && definition.IsBoss ? 1.45f : 1f;
        }

        private UnitView Spawn(CombatantState state, IReadOnlyList<Transform> cells)
        {
            int index = Mathf.Clamp(state.Position.Row * 3 + state.Position.Column, 0, cells.Count - 1);
            UnitView view = Instantiate(unitPrefab, cells[index]);
            view.transform.localPosition = Vector3.zero;
            unitViews[state.Id] = view;
            return view;
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
            FitBattlebackToCamera();
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

        private void FitBattlebackToCamera()
        {
            if (battlebackRenderer?.sprite == null || Camera.main == null)
            {
                return;
            }

            float cameraHeight = Camera.main.orthographicSize * 2f;
            float cameraWidth = cameraHeight * Camera.main.aspect;
            Vector2 spriteSize = battlebackRenderer.sprite.bounds.size;
            float scale = Mathf.Max(cameraWidth / spriteSize.x, cameraHeight / spriteSize.y);
            battlebackRenderer.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
