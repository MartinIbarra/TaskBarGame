using System.Collections.Generic;
using TaskbarTactics.Core.Models;

namespace TaskbarTactics.Content
{
    public sealed class ContentBlueprint
    {
        public List<HeroBlueprint> Heroes = new List<HeroBlueprint>();
        public List<SkillBlueprint> Skills = new List<SkillBlueprint>();
        public List<EnemyBlueprint> Enemies = new List<EnemyBlueprint>();
        public List<MapNodeBlueprint> MapNodes = new List<MapNodeBlueprint>();
        public List<ItemBlueprint> Items = new List<ItemBlueprint>();
        public List<AffixBlueprint> Affixes = new List<AffixBlueprint>();

        public static ContentBlueprint CreateVerticalSlice()
        {
            ContentBlueprint blueprint = new ContentBlueprint();
            AddHeroesAndSkills(blueprint);
            AddEnemies(blueprint);
            AddItems(blueprint);
            AddMap(blueprint);
            return blueprint;
        }

        private static void AddHeroesAndSkills(ContentBlueprint content)
        {
            AddHero(content, "guardian", "Guardián", "Guardian", 220, 16, 8, 2,
                "#4F78A8", new[] { "guard", "steel" }, "shield_bash", "fortress",
                "stalwart", "retaliation");
            AddHero(content, "cleric", "Clériga", "Cleric", 145, 14, 5, 3,
                "#E8D27B", new[] { "guard", "sacred" }, "healing_light", "aegis",
                "devotion", "radiance");
            AddHero(content, "ranger", "Exploradora", "Ranger", 125, 24, 3, 5,
                "#74B96B", new[] { "mark", "poison" }, "piercing_arrow", "hunters_mark",
                "eagle_eye", "toxic_tips");
            AddHero(content, "rogue", "Pícara", "Rogue", 115, 27, 3, 2,
                "#A678B5", new[] { "bleed", "poison" }, "flurry", "venom_blade",
                "opportunist", "deep_wounds");
            AddHero(content, "pyromancer", "Piromante", "Pyromancer", 110, 29, 2, 5,
                "#E2684B", new[] { "fire", "arcane" }, "fireball", "flame_wave",
                "kindling", "overheat");
            AddHero(content, "spellblade", "Espada mágica", "Spellblade", 165, 22, 6, 3,
                "#56B8C6", new[] { "steel", "arcane" }, "arcane_slash", "blink_strike",
                "mana_edge", "warded_mail");
        }

        private static void AddHero(
            ContentBlueprint content,
            string id,
            string nameEs,
            string nameEn,
            int health,
            int power,
            int defense,
            int range,
            string colorHex,
            string[] tags,
            string activeOne,
            string activeTwo,
            string passiveOne,
            string passiveTwo)
        {
            content.Heroes.Add(new HeroBlueprint
            {
                Id = id,
                NameEs = nameEs,
                NameEn = nameEn,
                MaxHealth = health,
                Power = power,
                Defense = defense,
                Range = range,
                ColorHex = colorHex,
                TagIds = new List<string>(tags),
                ActiveSkillIds = new List<string> { activeOne, activeTwo },
                PassiveSkillIds = new List<string> { passiveOne, passiveTwo }
            });

            content.Skills.Add(new SkillBlueprint(activeOne, id, false, 8, 1.5f));
            content.Skills.Add(new SkillBlueprint(activeTwo, id, false, 12, 1.9f));
            content.Skills.Add(new SkillBlueprint(passiveOne, id, true, 0, 0.15f));
            content.Skills.Add(new SkillBlueprint(passiveTwo, id, true, 0, 0.22f));
        }

        private static void AddEnemies(ContentBlueprint content)
        {
            content.Enemies.Add(new EnemyBlueprint("goblin", "Goblin", 70, 12, 1, 2, false));
            content.Enemies.Add(new EnemyBlueprint("goblin_archer", "Arquero goblin", 58, 15, 1, 5, false));
            content.Enemies.Add(new EnemyBlueprint("wolf", "Lobo", 80, 16, 2, 2, false));
            content.Enemies.Add(new EnemyBlueprint("shaman", "Chamán", 65, 18, 2, 5, false));
            content.Enemies.Add(new EnemyBlueprint("skeleton", "Esqueleto", 90, 14, 4, 2, false));
            content.Enemies.Add(new EnemyBlueprint("cultist", "Cultista", 75, 20, 2, 4, false));
            content.Enemies.Add(new EnemyBlueprint("ogre", "Ogro", 185, 24, 6, 2, false));
            content.Enemies.Add(new EnemyBlueprint("wraith", "Espectro", 100, 25, 3, 4, false));
            content.Enemies.Add(new EnemyBlueprint("barrow_king", "Rey del Túmulo", 640, 32, 9, 4, true));
        }

        private static void AddItems(ContentBlueprint content)
        {
            content.Items.Add(new ItemBlueprint("iron_sword", EquipmentSlot.Weapon, "steel"));
            content.Items.Add(new ItemBlueprint("ember_staff", EquipmentSlot.Weapon, "fire"));
            content.Items.Add(new ItemBlueprint("rangers_bow", EquipmentSlot.Weapon, "mark"));
            content.Items.Add(new ItemBlueprint("tower_mail", EquipmentSlot.Armor, "guard"));
            content.Items.Add(new ItemBlueprint("venom_leathers", EquipmentSlot.Armor, "poison"));
            content.Items.Add(new ItemBlueprint("arcane_robe", EquipmentSlot.Armor, "arcane"));
            content.Items.Add(new ItemBlueprint("sun_medallion", EquipmentSlot.Accessory, "sacred"));
            content.Items.Add(new ItemBlueprint("blood_brooch", EquipmentSlot.Accessory, "bleed"));
            content.Items.Add(new ItemBlueprint("warding_ring", EquipmentSlot.Accessory, "guard"));
            content.Items.Add(new ItemBlueprint("steel_relic", EquipmentSlot.Charm, "steel"));
            content.Items.Add(new ItemBlueprint("cinder_relic", EquipmentSlot.Charm, "fire"));
            content.Items.Add(new ItemBlueprint("serpent_relic", EquipmentSlot.Charm, "poison"));

            content.Affixes.Add(new AffixBlueprint("power", 3));
            content.Affixes.Add(new AffixBlueprint("vitality", 18));
            content.Affixes.Add(new AffixBlueprint("guard", 2));
            content.Affixes.Add(new AffixBlueprint("critical", 5));
            content.Affixes.Add(new AffixBlueprint("speed", 2));
            content.Affixes.Add(new AffixBlueprint("range", 1));
            content.Affixes.Add(new AffixBlueprint("fire", 2));
            content.Affixes.Add(new AffixBlueprint("poison", 2));
            content.Affixes.Add(new AffixBlueprint("arcane", 2));
            content.Affixes.Add(new AffixBlueprint("sacred", 2));
            content.Affixes.Add(new AffixBlueprint("steel", 2));
            content.Affixes.Add(new AffixBlueprint("bleed", 2));
        }

        private static void AddMap(ContentBlueprint content)
        {
            content.MapNodes.Add(Node("town", MapNodeType.Event, 0, "narrow_bridge"));
            content.MapNodes.Add(Node("narrow_bridge", MapNodeType.Combat, 1, "cave", "cemetery"));
            content.MapNodes.Add(Node("cave", MapNodeType.Treasure, 3, "goblin_village"));
            content.MapNodes.Add(Node("cemetery", MapNodeType.Combat, 2, "goblin_village"));
            content.MapNodes.Add(Node("goblin_village", MapNodeType.Elite, 4, "tomb_pass", "mountain_pass"));
            content.MapNodes.Add(Node("tomb_pass", MapNodeType.Elite, 6, "lost_forest"));
            content.MapNodes.Add(Node("mountain_pass", MapNodeType.Treasure, 4, "lost_forest"));
            content.MapNodes.Add(Node("lost_forest", MapNodeType.Combat, 7, "last_bastion"));
            content.MapNodes.Add(Node("last_bastion", MapNodeType.Boss, 10));
        }

        private static MapNodeBlueprint Node(
            string id,
            MapNodeType type,
            int difficulty,
            params string[] next)
        {
            return new MapNodeBlueprint
            {
                Id = id,
                Type = type,
                Difficulty = difficulty,
                NextNodeIds = new List<string>(next)
            };
        }
    }

    public sealed class HeroBlueprint
    {
        public string Id;
        public string NameEs;
        public string NameEn;
        public int MaxHealth;
        public int Power;
        public int Defense;
        public int Range;
        public string ColorHex;
        public List<string> TagIds = new List<string>();
        public List<string> ActiveSkillIds = new List<string>();
        public List<string> PassiveSkillIds = new List<string>();
    }

    public sealed class SkillBlueprint
    {
        public string Id;
        public string HeroId;
        public bool IsPassive;
        public int CooldownTicks;
        public float Magnitude;

        public SkillBlueprint(string id, string heroId, bool isPassive, int cooldownTicks, float magnitude)
        {
            Id = id;
            HeroId = heroId;
            IsPassive = isPassive;
            CooldownTicks = cooldownTicks;
            Magnitude = magnitude;
        }
    }

    public sealed class EnemyBlueprint
    {
        public string Id;
        public string NameEs;
        public int MaxHealth;
        public int Power;
        public int Defense;
        public int Range;
        public bool IsBoss;

        public EnemyBlueprint(
            string id,
            string nameEs,
            int maxHealth,
            int power,
            int defense,
            int range,
            bool isBoss)
        {
            Id = id;
            NameEs = nameEs;
            MaxHealth = maxHealth;
            Power = power;
            Defense = defense;
            Range = range;
            IsBoss = isBoss;
        }
    }

    public sealed class MapNodeBlueprint
    {
        public string Id;
        public MapNodeType Type;
        public int Difficulty;
        public List<string> NextNodeIds = new List<string>();
    }

    public sealed class ItemBlueprint
    {
        public string Id;
        public EquipmentSlot Slot;
        public string TagId;

        public ItemBlueprint(string id, EquipmentSlot slot, string tagId)
        {
            Id = id;
            Slot = slot;
            TagId = tagId;
        }
    }

    public sealed class AffixBlueprint
    {
        public string Id;
        public int Value;

        public AffixBlueprint(string id, int value)
        {
            Id = id;
            Value = value;
        }
    }
}
