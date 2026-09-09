using System.Collections.Generic;
using TaskbarTactics.Core.Combat;
using TaskbarTactics.Core.Equipment;
using TaskbarTactics.Core.Models;
using TaskbarTactics.Core.Stats;

namespace TaskbarTactics.Content
{
    public sealed class ContentBlueprint
    {
        public List<HeroBlueprint> Heroes = new List<HeroBlueprint>();
        public List<SkillBlueprint> Skills = new List<SkillBlueprint>();
        public List<EnemyBlueprint> Enemies = new List<EnemyBlueprint>();
        public List<MapNodeBlueprint> MapNodes = new List<MapNodeBlueprint>();
        public List<ItemBlueprint> Items = new List<ItemBlueprint>();
        public List<ItemBonusBlueprint> ItemBonuses = new List<ItemBonusBlueprint>();
        public List<StatusEffectBlueprint> StatusEffects = new List<StatusEffectBlueprint>();

        public static ContentBlueprint CreateVerticalSlice()
        {
            ContentBlueprint blueprint = new ContentBlueprint();
            AddHeroesAndSkills(blueprint);
            AddEnemies(blueprint);
            AddItemsAndBonuses(blueprint);
            AddStatusEffects(blueprint);
            AddMap(blueprint);
            return blueprint;
        }

        private static void AddHeroesAndSkills(ContentBlueprint content)
        {
            AddHero(content, "warrior", "Warrior", HeroClass.Warrior,
                Stats(220, 60, 22f, 5f, 60f, 25f, 0.95f, 0.80f, 2,
                    8f, 1.75f, 95f, 5f, 1.20f, 0.60f, 0f),
                Growth(12, 2, 1.4f, 0.2f, 2f, 0.8f, 0.003f, 0.001f),
                "#4F78A8", new[] { "guard", "steel" },
                "shield_bash", "fortress", "stalwart", "retaliation");
            AddHero(content, "cleric", "Cleric", HeroClass.Cleric,
                Stats(160, 120, 15f, 22f, 45f, 45f, 0.80f, 1.05f, 3,
                    7f, 1.60f, 95f, 5f, 0.80f, 1.80f, 5f),
                Growth(8, 6, 0.6f, 1.3f, 1.2f, 1.5f, 0.002f, 0.004f),
                "#E8D27B", new[] { "guard", "sacred" },
                "healing_light", "aegis", "devotion", "radiance");
            AddHero(content, "mage", "Mage", HeroClass.Mage,
                Stats(110, 160, 8f, 32f, 15f, 55f, 0.70f, 1.25f, 5,
                    10f, 1.80f, 100f, 8f, 0.35f, 2.80f, 5f),
                Growth(5, 8, 0.2f, 1.7f, 0.5f, 1.8f, 0.001f, 0.006f),
                "#E2684B", new[] { "fire", "arcane" },
                "fireball", "flame_wave", "kindling", "overheat");
            AddHero(content, "archer", "Archer", HeroClass.Archer,
                Stats(125, 80, 26f, 8f, 25f, 20f, 1.05f, 0.95f, 5,
                    15f, 2f, 110f, 12f, 0.45f, 1f, 0f),
                Growth(6, 3, 1.5f, 0.3f, 0.7f, 0.6f, 0.004f, 0.002f),
                "#74B96B", new[] { "mark", "poison" },
                "piercing_arrow", "hunters_mark", "eagle_eye", "toxic_tips");
            AddHero(content, "rogue", "Rogue", HeroClass.Rogue,
                Stats(115, 70, 24f, 8f, 20f, 20f, 1.40f, 1f, 2,
                    20f, 2f, 105f, 20f, 0.40f, 1f, 0f),
                Growth(5, 3, 1.4f, 0.2f, 0.6f, 0.6f, 0.005f, 0.002f),
                "#A678B5", new[] { "bleed", "poison" },
                "flurry", "venom_blade", "opportunist", "deep_wounds");
            AddHero(content, "magic_warrior", "Magic Warrior", HeroClass.MagicWarrior,
                Stats(175, 110, 21f, 20f, 40f, 35f, 1.05f, 1.10f, 3,
                    10f, 1.75f, 100f, 8f, 0.70f, 1.50f, 3f),
                Growth(9, 5, 1f, 1f, 1.1f, 1f, 0.003f, 0.004f),
                "#56B8C6", new[] { "steel", "arcane" },
                "arcane_slash", "blink_strike", "mana_edge", "warded_mail");
        }

        private static void AddHero(
            ContentBlueprint content,
            string id,
            string name,
            HeroClass heroClass,
            HeroStats baseStats,
            HeroStats growth,
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
                NameEs = name,
                NameEn = name,
                HeroClass = heroClass,
                BaseStats = baseStats,
                GrowthPerLevel = growth,
                EquipmentProfile = HeroEquipmentProfiles.Create(heroClass),
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

        private static HeroStats Stats(
            int health,
            int mana,
            float attack,
            float spell,
            float defense,
            float magicResistance,
            float attackSpeed,
            float castSpeed,
            int range,
            float criticalChance,
            float criticalDamage,
            float accuracy,
            float evasion,
            float healthRegeneration,
            float manaRegeneration,
            float cooldownReduction)
        {
            return new HeroStats
            {
                MaxHealth = health,
                MaxMana = mana,
                AttackPower = attack,
                SpellPower = spell,
                Defense = defense,
                MagicResistance = magicResistance,
                AttackSpeed = attackSpeed,
                CastSpeed = castSpeed,
                AttackRange = range,
                CriticalChance = criticalChance,
                CriticalDamage = criticalDamage,
                Accuracy = accuracy,
                Evasion = evasion,
                HealthRegeneration = healthRegeneration,
                ManaRegeneration = manaRegeneration,
                CooldownReduction = cooldownReduction
            };
        }

        private static HeroStats Growth(
            int health,
            int mana,
            float attack,
            float spell,
            float defense,
            float magicResistance,
            float attackSpeed,
            float castSpeed)
        {
            return new HeroStats
            {
                MaxHealth = health,
                MaxMana = mana,
                AttackPower = attack,
                SpellPower = spell,
                Defense = defense,
                MagicResistance = magicResistance,
                AttackSpeed = attackSpeed,
                CastSpeed = castSpeed
            };
        }

        private static void AddEnemies(ContentBlueprint content)
        {
            content.Enemies.Add(new EnemyBlueprint("goblin", "Goblin", 98, 12, 1, 2, false));
            content.Enemies.Add(new EnemyBlueprint("goblin_archer", "Arquero goblin", 81, 15, 1, 5, false));
            content.Enemies.Add(new EnemyBlueprint("goblin_mage", "Mago goblin", 94, 15, 1, 5, false));
            content.Enemies.Add(new EnemyBlueprint("wolf", "Lobo", 80, 16, 2, 2, false));
            content.Enemies.Add(new EnemyBlueprint("shaman", "Chamán", 65, 18, 2, 5, false));
            content.Enemies.Add(new EnemyBlueprint("skeleton", "Esqueleto", 90, 14, 4, 2, false));
            content.Enemies.Add(new EnemyBlueprint("cultist", "Cultista", 75, 20, 2, 4, false));
            content.Enemies.Add(new EnemyBlueprint("bog_slime", "Slime pantanoso", 105, 13, 5, 2, false));
            content.Enemies.Add(new EnemyBlueprint("ogre", "Ogro", 185, 24, 6, 2, false));
            content.Enemies.Add(new EnemyBlueprint("wraith", "Espectro", 100, 25, 3, 4, false));
            content.Enemies.Add(new EnemyBlueprint("soul_fury", "Furia Alma", 95, 22, 3, 4, false));
            content.Enemies.Add(new EnemyBlueprint("wyvern", "Wyvern", 150, 28, 4, 4, false));
            content.Enemies.Add(new EnemyBlueprint("barrow_king", "Rey del Túmulo", 640, 32, 9, 4, true));
        }

        private static void AddItemsAndBonuses(ContentBlueprint content)
        {
            content.Items.Add(ItemBlueprint.Weapon("wooden_sword", WeaponType.Sword,
                Handedness.OneHanded, "steel", Flat(HeroStatType.AttackPower, 1.5f)));
            content.Items.Add(ItemBlueprint.Weapon("wooden_mace", WeaponType.Mace,
                Handedness.OneHanded, "sacred", Flat(HeroStatType.AttackPower, 1.5f)));
            content.Items.Add(ItemBlueprint.Weapon("wooden_staff", WeaponType.Staff,
                Handedness.TwoHanded, "fire", Flat(HeroStatType.SpellPower, 2.5f)));
            content.Items.Add(ItemBlueprint.Weapon("wooden_bow", WeaponType.Bow,
                Handedness.TwoHanded, "mark", Flat(HeroStatType.AttackPower, 2.5f)));
            content.Items.Add(ItemBlueprint.Weapon("wooden_dagger", WeaponType.Dagger,
                Handedness.OneHanded, "bleed", Flat(HeroStatType.AttackPower, 1.25f)));
            content.Items.Add(ItemBlueprint.Weapon("iron_sword", WeaponType.Sword,
                Handedness.OneHanded, "steel", Flat(HeroStatType.AttackPower, 3f)));
            content.Items.Add(ItemBlueprint.Weapon("greatsword", WeaponType.Sword,
                Handedness.TwoHanded, "steel", Flat(HeroStatType.AttackPower, 6f)));
            content.Items.Add(ItemBlueprint.Weapon("iron_mace", WeaponType.Mace,
                Handedness.OneHanded, "sacred", Flat(HeroStatType.AttackPower, 3f)));
            content.Items.Add(ItemBlueprint.Weapon("war_maul", WeaponType.Mace,
                Handedness.TwoHanded, "guard", Flat(HeroStatType.AttackPower, 6f)));
            content.Items.Add(ItemBlueprint.Weapon("hand_axe", WeaponType.Axe,
                Handedness.OneHanded, "steel", Flat(HeroStatType.AttackPower, 3f)));
            content.Items.Add(ItemBlueprint.Weapon("greataxe", WeaponType.Axe,
                Handedness.TwoHanded, "steel", Flat(HeroStatType.AttackPower, 6f)));
            content.Items.Add(ItemBlueprint.Weapon("ember_staff", WeaponType.Staff,
                Handedness.TwoHanded, "fire", Flat(HeroStatType.SpellPower, 7f)));
            content.Items.Add(ItemBlueprint.Weapon("arcane_wand", WeaponType.Wand,
                Handedness.OneHanded, "arcane", Flat(HeroStatType.SpellPower, 4f)));
            content.Items.Add(ItemBlueprint.Weapon("archer_bow", WeaponType.Bow,
                Handedness.TwoHanded, "mark", Flat(HeroStatType.AttackPower, 6f)));
            content.Items.Add(ItemBlueprint.Weapon("field_crossbow", WeaponType.Crossbow,
                Handedness.TwoHanded, "mark", Flat(HeroStatType.AttackPower, 7f)));
            content.Items.Add(ItemBlueprint.Weapon("steel_dagger", WeaponType.Dagger,
                Handedness.OneHanded, "bleed", Flat(HeroStatType.AttackPower, 2.5f)));
            content.Items.Add(ItemBlueprint.OffHand("tower_shield", OffHandType.Shield,
                "guard", Flat(HeroStatType.Defense, 12f)));
            content.Items.Add(ItemBlueprint.OffHand("arcane_book", OffHandType.Book,
                "arcane", Flat(HeroStatType.SpellPower, 4f)));
            content.Items.Add(ItemBlueprint.Armor("tower_mail", EquipmentSlot.Chest,
                ArmorType.Plate, "guard", Flat(HeroStatType.Defense, 10f)));
            content.Items.Add(ItemBlueprint.Armor("cleric_mail", EquipmentSlot.Chest,
                ArmorType.Mail, "sacred", Flat(HeroStatType.Defense, 7f)));
            content.Items.Add(ItemBlueprint.Armor("venom_leathers", EquipmentSlot.Chest,
                ArmorType.Leather, "poison", Flat(HeroStatType.Evasion, 4f)));
            content.Items.Add(ItemBlueprint.Armor("arcane_robe", EquipmentSlot.Chest,
                ArmorType.Cloth, "arcane", Flat(HeroStatType.SpellPower, 3f)));
            content.Items.Add(ItemBlueprint.Accessory("sun_medallion", EquipmentSlot.Neck,
                "sacred", Flat(HeroStatType.MaxMana, 15f)));
            content.Items.Add(ItemBlueprint.Accessory("warding_ring", EquipmentSlot.Ring1,
                "guard", Flat(HeroStatType.MagicResistance, 5f)));
            content.Items.Add(ItemBlueprint.Accessory("silver_earring", EquipmentSlot.Earring1,
                "arcane", Flat(HeroStatType.CastSpeed, 0.04f)));
            content.Items.Add(ItemBlueprint.Accessory("steel_belt", EquipmentSlot.Belt,
                "steel", Flat(HeroStatType.MaxHealth, 12f)));
            content.Items.Add(ItemBlueprint.Accessory("cinder_cloak", EquipmentSlot.Cloak,
                "fire", Flat(HeroStatType.MagicResistance, 5f)));

            AddBonus(content, "attack_power", HeroStatType.AttackPower,
                StatModifierOperation.Flat, 3f);
            AddBonus(content, "spell_power", HeroStatType.SpellPower,
                StatModifierOperation.Flat, 3f);
            AddBonus(content, "vitality", HeroStatType.MaxHealth,
                StatModifierOperation.Flat, 18f);
            AddBonus(content, "mana", HeroStatType.MaxMana,
                StatModifierOperation.Flat, 15f);
            AddBonus(content, "guard", HeroStatType.Defense,
                StatModifierOperation.Flat, 8f);
            AddBonus(content, "critical", HeroStatType.CriticalChance,
                StatModifierOperation.Flat, 5f);
            AddBonus(content, "haste", HeroStatType.AttackSpeed,
                StatModifierOperation.AdditivePercent, 5f);
            AddBonus(content, "cast_haste", HeroStatType.CastSpeed,
                StatModifierOperation.AdditivePercent, 5f);
            AddBonus(content, "accuracy", HeroStatType.Accuracy,
                StatModifierOperation.Flat, 5f);
            AddBonus(content, "evasion", HeroStatType.Evasion,
                StatModifierOperation.Flat, 5f);
        }

        private static StatModifier Flat(HeroStatType stat, float value)
        {
            return new StatModifier(stat, StatModifierOperation.Flat, value, "item_base");
        }

        private static void AddBonus(
            ContentBlueprint content,
            string id,
            HeroStatType stat,
            StatModifierOperation operation,
            float value)
        {
            content.ItemBonuses.Add(new ItemBonusBlueprint
            {
                Id = id,
                Modifiers = new List<StatModifier>
                {
                    new StatModifier(stat, operation, value, id)
                }
            });
        }

        private static void AddStatusEffects(ContentBlueprint content)
        {
            content.StatusEffects.Add(new StatusEffectBlueprint
            {
                Data = new StatusEffectDefinitionData
                {
                    Id = "stun",
                    Kind = StatusEffectKind.Debuff,
                    DurationMilliseconds = 1500,
                    PreventsBasicAttacks = true,
                    PreventsCasting = true,
                    Tags = new List<string> { "control", "stun" }
                }
            });
            content.StatusEffects.Add(new StatusEffectBlueprint
            {
                Data = new StatusEffectDefinitionData
                {
                    Id = "silence",
                    Kind = StatusEffectKind.Debuff,
                    DurationMilliseconds = 2500,
                    PreventsCasting = true,
                    Tags = new List<string> { "control", "silence" }
                }
            });
            content.StatusEffects.Add(PeriodicStatus(
                "poison", StatusEffectKind.Debuff, StatusPeriodicEffect.Damage, 2f, 5000, 3));
            content.StatusEffects.Add(PeriodicStatus(
                "bleed", StatusEffectKind.Debuff, StatusPeriodicEffect.Damage, 3f, 4000, 3));
            content.StatusEffects.Add(PeriodicStatus(
                "regeneration", StatusEffectKind.Buff, StatusPeriodicEffect.Healing, 3f, 5000, 1));
            content.StatusEffects.Add(new StatusEffectBlueprint
            {
                Data = new StatusEffectDefinitionData
                {
                    Id = "haste",
                    Kind = StatusEffectKind.Buff,
                    DurationMilliseconds = 5000,
                    MaxStacks = 1,
                    StackPolicy = StatusStackPolicy.RefreshDuration,
                    Modifiers = new List<StatModifier>
                    {
                        new StatModifier(HeroStatType.AttackSpeed,
                            StatModifierOperation.AdditivePercent, 15f, "haste")
                    },
                    Tags = new List<string> { "haste" }
                }
            });
        }

        private static StatusEffectBlueprint PeriodicStatus(
            string id,
            StatusEffectKind kind,
            StatusPeriodicEffect periodicEffect,
            float amount,
            int duration,
            int maxStacks)
        {
            return new StatusEffectBlueprint
            {
                Data = new StatusEffectDefinitionData
                {
                    Id = id,
                    Kind = kind,
                    DurationMilliseconds = duration,
                    MaxStacks = maxStacks,
                    StackPolicy = StatusStackPolicy.StackAndRefresh,
                    PeriodicEffect = periodicEffect,
                    PeriodicAmount = amount,
                    PeriodMilliseconds = 1000,
                    Tags = new List<string> { id }
                }
            };
        }

        private static void AddMap(ContentBlueprint content)
        {
            content.MapNodes.Add(Node("town", MapNodeType.Event, 0, "narrow_bridge"));
            content.MapNodes.Add(Node("narrow_bridge", MapNodeType.Combat, 3, "cave", "cemetery"));
            content.MapNodes.Add(Node("cave", MapNodeType.Treasure, 4, "goblin_village"));
            content.MapNodes.Add(Node("cemetery", MapNodeType.Combat, 4, "goblin_village"));
            content.MapNodes.Add(Node("goblin_village", MapNodeType.Elite, 4, "tomb_pass", "mountain_pass"));
            content.MapNodes.Add(Node("tomb_pass", MapNodeType.Elite, 6, "lost_forest"));
            content.MapNodes.Add(Node("mountain_pass", MapNodeType.Treasure, 4, "lost_forest"));
            content.MapNodes.Add(Node("lost_forest", MapNodeType.Combat, 7, "last_bastion"));
            content.MapNodes.Add(Node("last_bastion", MapNodeType.Boss, 10));
            content.MapNodes.Add(Node("city2", MapNodeType.Event, 0, "corrupt_pass"));
            content.MapNodes.Add(Node("corrupt_pass", MapNodeType.Combat, 3, "lo_hueso"));
            content.MapNodes.Add(Node("lo_hueso", MapNodeType.Combat, 4, "mt_secret", "ancient_ruins"));
            content.MapNodes.Add(Node("mt_secret", MapNodeType.Combat, 5, "ancient_ruins"));
            content.MapNodes.Add(Node("ancient_ruins", MapNodeType.Combat, 5, "arbol_morto"));
            content.MapNodes.Add(Node("arbol_morto", MapNodeType.Combat, 6, "mountain_pass_act2"));
            content.MapNodes.Add(Node("mountain_pass_act2", MapNodeType.Combat, 7, "black_tower", "port"));
            content.MapNodes.Add(Node("black_tower", MapNodeType.Combat, 8, "port"));
            content.MapNodes.Add(Node("port", MapNodeType.Combat, 9, "lost_bay"));
            content.MapNodes.Add(Node("lost_bay", MapNodeType.Boss, 10));
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
        public HeroClass HeroClass;
        public HeroStats BaseStats;
        public HeroStats GrowthPerLevel;
        public HeroEquipmentProfile EquipmentProfile;
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
        public EquipmentDescriptor Descriptor;
        public string TagId;

        public static ItemBlueprint Weapon(
            string id,
            WeaponType type,
            Handedness handedness,
            string tagId,
            params StatModifier[] modifiers)
        {
            return Create(id, EquipmentDescriptor.Weapon(id, type, handedness, modifiers), tagId);
        }

        public static ItemBlueprint OffHand(
            string id,
            OffHandType type,
            string tagId,
            params StatModifier[] modifiers)
        {
            return Create(id, EquipmentDescriptor.OffHand(id, type, modifiers), tagId);
        }

        public static ItemBlueprint Armor(
            string id,
            EquipmentSlot slot,
            ArmorType armorType,
            string tagId,
            params StatModifier[] modifiers)
        {
            return Create(id, EquipmentDescriptor.Armor(id, slot, armorType, modifiers), tagId);
        }

        public static ItemBlueprint Accessory(
            string id,
            EquipmentSlot slot,
            string tagId,
            params StatModifier[] modifiers)
        {
            return Create(id, EquipmentDescriptor.Accessory(id, slot, modifiers), tagId);
        }

        private static ItemBlueprint Create(
            string id,
            EquipmentDescriptor descriptor,
            string tagId)
        {
            return new ItemBlueprint { Id = id, Descriptor = descriptor, TagId = tagId };
        }
    }

    public sealed class ItemBonusBlueprint
    {
        public string Id;
        public List<StatModifier> Modifiers = new List<StatModifier>();
    }

    public sealed class StatusEffectBlueprint
    {
        public StatusEffectDefinitionData Data = new StatusEffectDefinitionData();
    }

}
