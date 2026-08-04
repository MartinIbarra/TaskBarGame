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
        [SerializeField] private Color enemyColor = new Color(0.78f, 0.24f, 0.25f);

        private readonly Dictionary<string, UnitView> unitViews = new Dictionary<string, UnitView>();

        public void Configure(
            UnitView prefab,
            IEnumerable<Transform> heroes,
            IEnumerable<Transform> enemies)
        {
            unitPrefab = prefab;
            heroCells = new List<Transform>(heroes);
            enemyCells = new List<Transform>(enemies);
        }

        public IEnumerator Play(
            CombatRequest request,
            CombatResult result,
            GameContentCatalog catalog,
            float durationSeconds)
        {
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
                    enemy.MaxHealth);
            }
        }

        private UnitView Spawn(CombatantState state, IReadOnlyList<Transform> cells)
        {
            int index = Mathf.Clamp(state.Position.Row * 3 + state.Position.Column, 0, cells.Count - 1);
            UnitView view = Instantiate(unitPrefab, cells[index]);
            view.transform.localPosition = Vector3.zero;
            unitViews[state.Id] = view;
            return view;
        }
    }
}
