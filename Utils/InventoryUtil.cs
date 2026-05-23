using Archipelago.MultiClient.Net.Packets;

using Newtonsoft.Json.Linq;

using Reloaded.Mod.Interfaces;

using RnSArchipelago.Data;

namespace RnSArchipelago.Utils
{
    internal class InventoryUtil
    {
        private readonly ILogger logger;
        private readonly SharedData data;

        public InventoryUtil(ILogger logger, SharedData data)
        {
            this.logger = logger;
            this.data = data;
            Reset();
        }

        internal bool isActive;
        internal bool isKingdomSanity;
        internal RunTypeSetting run_type = RunTypeSetting.Kingdom;
        internal bool isProgressive;
        internal bool useKingdomOrderWithKingdomSanity;
        internal long maxKingdoms;
        internal string? seed;
        internal Dictionary<int, List<string>> kingdomOrder = [];

        internal bool isClassSanity;
        internal List<string> checksPerClass = [];
        private List<long> availableItems = [];
        internal bool checksPerItemInChest;
        private List<string> availablePotions = [];
        private GoalSetting goal = GoalSetting.Shira;
        internal long shiraKills;
        internal long witchKills;
        private HashSet<string> shira_victories = [];
        private HashSet<string> witch_victories = [];
        private ShopSetting shop_sanity = ShopSetting.None;

        internal delegate void AddChestDelegate();
        internal event AddChestDelegate? AddChest;

        internal delegate void SendGoalDelegate();
        internal event SendGoalDelegate? SendGoal;

        internal delegate void UpdateHallwayOnItemRecieveDelegate();
        internal event UpdateHallwayOnItemRecieveDelegate? UpdateHallwayOnItemRecieve;

        internal enum RunTypeSetting
        {
            Kingdom = 0,
            Extra = 1,
            Either = 2,
            Chaotic = 3
        }

        internal RunTypeSetting RunType => run_type;

        internal enum ItemSetting
        {
            None = 0,
            Itemset = 1,
            Full = 2
        }

        internal ItemSetting ItemSanity { get; set; }

        internal enum UpgradeSetting
        {
            None = 0,
            Simple = 1,
            Full = 2
        }

        internal UpgradeSetting UpgradeSanity { get; set; }

        internal enum PotionSetting
        {
            None = 0,
            Locked = 1,
            Roulette = 2
        }

        internal PotionSetting PotionSanity { get; set; }

        internal enum GoalSetting
        {
            Shira = 1,
            Witch = 2,
            Both = 3
        }

        internal GoalSetting Goal => goal;

        internal enum ShopSetting
        {
            None = 0,
            Global = 1,
            Regional = 2
        }

        internal ShopSetting ShopSanity => shop_sanity;

        internal void Reset()
        {
            isActive = false;
            run_type = RunTypeSetting.Kingdom;
            AvailableKingdoms = KingdomFlags.None;
            ProgressiveRegions = 0;
            kingdomOrder = [];
            AvailableClasses = ClassFlags.None;
            checksPerClass = [];
            availableItems = [];
            AvailableTreasurespheres = 0;
            ItemSanity = ItemSetting.None;
            UpgradeSanity = UpgradeSetting.None;
            PotionSanity = PotionSetting.None;
            availablePotions = [];
            goal = GoalSetting.Shira;
            shiraKills = 0;
            witchKills = 0;
            shira_victories = [];
            witch_victories = [];
            shop_sanity = ShopSetting.None;
        }

        // Init function to get the options the user has selected
        internal void GetOptions()
        {
            isActive = true;

            isKingdomSanity = data.options.Get<long>("kingdom_sanity") == 1;
            run_type = (RunTypeSetting)data.options.Get<long>("run_type");
            isProgressive = data.options.Get<long>("progressive_regions") == 1;
            useKingdomOrderWithKingdomSanity = data.options.Get<long>("kingdom_sanity_kingdom_order") == 1;
            maxKingdoms = data.options.Get<long>("max_kingdoms_per_run");
            seed = data.options.Get<string>("seed");

            if (!isKingdomSanity)
            {
                AvailableKingdoms = KingdomFlags.All;

                List<string> excluded_kingdoms = data.options.Get<JArray>("excluded_kingdoms")?.ToObject<List<string>>()!;

                foreach (var kingdom in excluded_kingdoms)
                {
                    AvailableKingdoms = AvailableKingdoms & ~(KingdomFlags)Enum.Parse(typeof(KingdomFlags), kingdom.Replace(" ", "_").Replace("'", ""));
                }
            }

            var kingdomOrderDict = data.options.Get<JObject>("kingdom_order")?.ToObject<Dictionary<string, int>>();
            kingdomOrder = [];
            foreach (var entry in kingdomOrderDict!)
            {
                if (!kingdomOrder.ContainsKey(entry.Value - 1))
                {
                    kingdomOrder[entry.Value - 1] = [KingdomNameToNotch(entry.Key)];
                }
                else
                {

                    kingdomOrder[entry.Value - 1].Add(KingdomNameToNotch(entry.Key));
                }
            }

            isClassSanity = data.options.Get<long>("class_sanity") == 1;

            if (!isClassSanity)
            {
                AvailableClasses = ClassFlags.All;
            }

            checksPerClass = data.options.Get<JArray>("checks_per_class")?.ToObject<List<string>>()!;
            this.logger.PrintMessage(String.Join(", ", checksPerClass), System.Drawing.Color.DarkOrange);

            ItemSanity = (ItemSetting)data.options.Get<long>("item_sanity");
            checksPerItemInChest = data.options.Get<long>("checks_per_item_in_chest") == 1;

            UpgradeSanity = (UpgradeSetting)data.options.Get<long>("upgrade_sanity");
            this.logger.PrintMessage(UpgradeSanity.ToString(), System.Drawing.Color.DarkOrange);

            PotionSanity = (PotionSetting)data.options.Get<long>("potion_sanity");
            this.logger.PrintMessage(PotionSanity.ToString(), System.Drawing.Color.DarkOrange);

            goal = (GoalSetting)data.options.Get<long>("goal_condition");
            this.logger.PrintMessage(goal.ToString(), System.Drawing.Color.DarkOrange);

            shiraKills = data.options.Get<long>("shira_defeats")!;
            witchKills = data.options.Get<long>("witch_defeats")!;

            shop_sanity = (ShopSetting)data.options.Get<long>("shop_sanity");
            this.logger.PrintMessage(shop_sanity.ToString(), System.Drawing.Color.DarkOrange);
        }

        [Flags]
        internal enum KingdomFlags
        {
            None = 0b00000000,
            Kingdom_Outskirts = 0b00000001,
            Scholars_Nest = 0b00000010,
            Kings_Arsenal = 0b00000100,
            Red_Darkhouse = 0b00001000,
            Churchmouse_Streets = 0b00010000,
            Emerald_Lakeside = 0b00100000,
            The_Pale_Keep = 0b01000000,
            Moonlit_Pinnacle = 0b10000000,
            All_Base = 0b11111111,
            Crack_in_the_Geode = 0b100000000,
            Darkhouse_Depths = 0b1000000000,
            Atelier_Aurum = 0b10000000000,
            Subterra_Sanctum = 0b100000000000,
            Looping_Hallway = 0b1000000000000,
            Reflecting_Pool = 0b10000000000000,
            All_Extra = 0b11111100000000,
            All = 0b11111111111111
        }

        private static readonly string[] KINGDOMS = ["Kingdom Outskirts", "Scholar's Nest", "King's Arsenal", "Red Darkhouse", "Churchmouse Streets", "Emerald Lakeside", "The Pale Keep", "Moonlit Pinnacle",
                                                    "Crack in the Geode", "Darkhouse Depths", "Atelier Aurum", "Subterra Sanctum", "Looping Hallway", "Reflecting Pool"];

        internal KingdomFlags AvailableKingdoms { get; set; }
        internal int ProgressiveRegions { get; set; }

        [Flags]
        internal enum ClassFlags
        {
            None = 0b00000000000000,
            Wizard = 0b00000000000001,
            Assassin = 0b00000000000010,
            Heavyblade = 0b00000000000100,
            Dancer = 0b00000000001000,
            Druid = 0b00000000010000,
            Spellsword = 0b00000000100000,
            Sniper = 0b00000001000000,
            Bruiser = 0b00000010000000,
            Defender = 0b00000100000000,
            Ancient = 0b00001000000000,
            Hammermaid = 0b00010000000000,
            Pyromancer = 0b00100000000000,
            Grenadier = 0b01000000000000,
            Shadow = 0b10000000000000,
            All = 0b11111111111111
        }

        internal static readonly string[] CLASSES = ["Wizard", "Assassin", "Heavyblade", "Dancer", "Druid", "Spellsword", "Sniper", "Bruiser", "Defender", "Ancient",
                                                    "Hammermaid", "Pyromancer", "Grenadier", "Shadow"];

        internal ClassFlags AvailableClasses { get; set; }

        internal int AvailableClassesCount => CLASSES.Length;

        private static readonly string[] ITEMSETS = [ "Arcane Set", "Night Set","Timespace Set", "Wind Set", "Bloodwolf Set", "Assassin Set", "Rockdragon Set", "Flame Set",
                                                    "Gem Set", "Lightning Set", "Shrine Set", "Lucky Set", "Life Set", "Poison Set", "Depth Set", "Darkbite Set", "Timegem Set",
                                                    "Youkai Set", "Haunted Set", "Gladiator Set", "Sparkblade Set", "Swiftflight Set", "Sacredflame Set", "Ruins Set", "Lakeshrine Set",
                                                    "Glacier Set", "Memory Set", "Cultist Set", "Painters Set", "Daynight Set", "Sharpedge Set", "Oceans Set", "Performers Set",
                                                    "Miners Set", "Teaparty Set"];

        internal List<long> AvailableItems => availableItems;

        #region Individual Items
        private static int itemId = 399;

        private static readonly long RAVEN_GRIMOIRE = itemId++;
        private static readonly long BLACKWING_STAFF = itemId++;
        private static readonly long CURSE_TALON = itemId++;
        private static readonly long DARKMAGIC_BLADE = itemId++;
        private static readonly long WITCHS_CLOAK = itemId++;
        private static readonly long CROWFEATHER_HAIRPIN = itemId++;
        private static readonly long REDBLACK_RIBBON = itemId++;
        private static readonly long OPAL_NECKLACE = itemId++;

        private static readonly long SLEEPING_GREATBOW = itemId++;
        private static readonly long CRESCENTMOON_DAGGER = itemId++;
        private static readonly long LULLABY_HARP = itemId++;
        private static readonly long NIGHTSTAR_GRIMOIRE = itemId++;
        private static readonly long MOON_PENDANT = itemId++;
        private static readonly long PAJAMA_HAT = itemId++;
        private static readonly long STUFFED_RABBIT = itemId++;
        private static readonly long NIGHTINGALE_GOWN = itemId++;

        private static readonly long ETERNITY_FLUTE = itemId++;
        private static readonly long TIMEWARP_WAND = itemId++;
        private static readonly long CHROME_SHIELD = itemId++;
        private static readonly long CLOCKWORK_TOME = itemId++;
        private static readonly long METRONOME_BOOTS = itemId++;
        private static readonly long TIMEMAGE_CAP = itemId++;
        private static readonly long STARRY_CLOAK = itemId++;
        private static readonly long GEMINI_NECKLACE = itemId++;

        private static readonly long HAWKFEATHER_FAN = itemId++;
        private static readonly long WINDBITE_DAGGER = itemId++;
        private static readonly long PIDGEON_BOW = itemId++;
        private static readonly long SHINSOKU_KATANA = itemId++;
        private static readonly long EAGLEWING_CHARM = itemId++;
        private static readonly long SPARROW_FEATHER = itemId++;
        private static readonly long WINGED_CAP = itemId++;
        private static readonly long THIEFS_COAT = itemId++;

        private static readonly long VAMPRIC_DAGGER = itemId++;
        private static readonly long BLOODY_BANDAGE = itemId++;
        private static readonly long LEECH_STAFF = itemId++;
        private static readonly long BLOODHOUND_GREATSWORD = itemId++;
        private static readonly long REAPER_CLOAK = itemId++;
        private static readonly long BLOODFLOWER_BROOCH = itemId++;
        private static readonly long WOLF_HOOD = itemId++;
        private static readonly long BLOOD_VIAL = itemId++;

        private static readonly long BLACK_WAKIZASHI = itemId++;
        private static readonly long THROWING_DAGGER = itemId++;
        private static readonly long ASSASSINS_KNIFE = itemId++;
        private static readonly long NINJUTSU_SCROLL = itemId++;
        private static readonly long SHADOW_BRACELET = itemId++;
        private static readonly long NINJA_ROBE = itemId++;
        private static readonly long KUNOICHI_HOOD = itemId++;
        private static readonly long SHINOBI_TABI = itemId++;

        private static readonly long DRAGONHEAD_SPEAR = itemId++;
        private static readonly long GRANITE_GREATSWORD = itemId++;
        private static readonly long GREYSTEEL_SHIELD = itemId++;
        private static readonly long STONEBREAKER_STAFF = itemId++;
        private static readonly long TOUGH_GAUNTLET = itemId++;
        private static readonly long ROCKDRAGON_MAIL = itemId++;
        private static readonly long OBSIDIAN_HAIRPIN = itemId++;
        private static readonly long IRON_GREAVES = itemId++;

        private static readonly long VOLCANO_SPEAR = itemId++;
        private static readonly long REDDRAGON_BLADE = itemId++;
        private static readonly long FLAME_BOW = itemId++;
        private static readonly long METEOR_STAFF = itemId++;
        private static readonly long PHOENIX_CHARM = itemId++;
        private static readonly long FIRESCALE_CORSET = itemId++;
        private static readonly long DEMON_HORNS = itemId++;
        private static readonly long FLAMEWALKER_BOOTS = itemId++;

        private static readonly long DIAMOND_SHIELD = itemId++;
        private static readonly long PERIDOT_RAPIER = itemId++;
        private static readonly long GARNET_STAFF = itemId++;
        private static readonly long SAPPHIRE_VIOLIN = itemId++;
        private static readonly long EMERALD_CHESTPLATE = itemId++;
        private static readonly long AMETHYST_BRACELET = itemId++;
        private static readonly long TOPAZ_CHARM = itemId++;
        private static readonly long RUBY_CIRCLET = itemId++;

        private static readonly long BRIGHTSTORM_SPEAR = itemId++;
        private static readonly long BOLT_STAFF = itemId++;
        private static readonly long LIGHTNING_BOW = itemId++;
        private static readonly long DARKSTORM_KNIFE = itemId++;
        private static readonly long DARKCLOUD_NECKLACE = itemId++;
        private static readonly long CROWN_OF_STORMS = itemId++;
        private static readonly long THUNDERCLAP_GLOVES = itemId++;
        private static readonly long STORM_PETTICOAT = itemId++;

        private static readonly long HOLY_GREATSWORD = itemId++;
        private static readonly long SACRED_BOW = itemId++;
        private static readonly long PURIFICATION_ROD = itemId++;
        private static readonly long ORNAMENTAL_BELL = itemId++;
        private static readonly long SHRINEMAIDENS_KOSODE = itemId++;
        private static readonly long REDWHITE_RIBBON = itemId++;
        private static readonly long DIVINE_MIRROR = itemId++;
        private static readonly long GOLDEN_CHIME = itemId++;

        private static readonly long BOOK_OF_CHEATS = itemId++;
        private static readonly long GOLDEN_KATANA = itemId++;
        private static readonly long GLITTERING_TRUMPET = itemId++;
        private static readonly long ROYAL_STAFF = itemId++;
        private static readonly long BALLROOM_GOWN = itemId++;
        private static readonly long SILVER_COIN = itemId++;
        private static readonly long QUEENS_CROWN = itemId++;
        private static readonly long MIMICK_RABBITFOOT = itemId++;

        private static readonly long BUTTERFLY_OCARINA = itemId++;
        private static readonly long FAIRY_SPEAR = itemId++;
        private static readonly long MOSS_SHIELD = itemId++;
        private static readonly long FLORAL_BOW = itemId++;
        private static readonly long BLUE_ROSE = itemId++;
        private static readonly long SUNFLOWER_CROWN = itemId++;
        private static readonly long MIDSUMMER_DRESS = itemId++;
        private static readonly long GRASSWOVEN_BRACELET = itemId++;

        private static readonly long SNAKEFANG_DAGGER = itemId++;
        private static readonly long IVY_STAFF = itemId++;
        private static readonly long DEATHCAP_TOME = itemId++;
        private static readonly long SPIDERBITE_BOW = itemId++;
        private static readonly long COMPOUND_GLOVES = itemId++;
        private static readonly long POISONFROG_CHARM = itemId++;
        private static readonly long VENOM_HOOD = itemId++;
        private static readonly long CHEMISTS_COAT = itemId++;

        private static readonly long SEASHEEL_SHIELD = itemId++;
        private static readonly long NECRONOMICON = itemId++;
        private static readonly long TIDAL_GREATSWORD = itemId++;
        private static readonly long OCCULT_DAGGER = itemId++;
        private static readonly long MERMAID_SCALEMAIL = itemId++;
        private static readonly long HYDROUS_BLOB = itemId++;
        private static readonly long ABYSS_ARTIFACT = itemId++;
        private static readonly long LOST_PENDANT = itemId++;

        private static readonly long SAWTOOTH_CLEAVER = itemId++;
        private static readonly long RAVENS_DAGGER = itemId++;
        private static readonly long KILLING_NOTE = itemId++;
        private static readonly long BLACKSTEEL_BUCKLER = itemId++;
        private static readonly long NIGHTGUARD_GLOVES = itemId++;
        private static readonly long SNIPERS_EYEGLASSES = itemId++;
        private static readonly long DARKMAGE_CHARM = itemId++;
        private static readonly long FIRSTSTRIKE_BRACELET = itemId++;

        private static readonly long OBSIDIAN_ROD = itemId++;
        private static readonly long DARKGLASS_SPEAR = itemId++;
        private static readonly long TIMESPACE_DAGGER = itemId++;
        private static readonly long QUARTZ_SHIELD = itemId++;
        private static readonly long POCKETWATCH = itemId++;
        private static readonly long NOVA_CROWN = itemId++;
        private static readonly long BLACKHOLE_CHARM = itemId++;
        private static readonly long TWINSTAR_EARRINGS = itemId++;

        private static readonly long KYOU_NO_OMIKUJI = itemId++;
        private static readonly long YOUKAI_BRACELET = itemId++;
        private static readonly long ONI_STAFF = itemId++;
        private static readonly long KAPPA_SHIELD = itemId++;
        private static readonly long USAGI_KAMEN = itemId++;
        private static readonly long RED_TANZAKU = itemId++;
        private static readonly long VEGA_SPEAR = itemId++;
        private static readonly long ALTAIR_DAGGER = itemId++;

        private static readonly long GHOST_SPEAR = itemId++;
        private static readonly long PHANTOM_DAGGER = itemId++;
        private static readonly long CURSED_CANDLESTAFF = itemId++;
        private static readonly long HAUNTED_GLOVES = itemId++;
        private static readonly long OLD_BONNET = itemId++;
        private static readonly long MAID_OUTFIT = itemId++;
        private static readonly long CALLING_BELL = itemId++;
        private static readonly long SMOKE_SHIELD = itemId++;

        private static readonly long GRANDMASTER_SPEAR = itemId++;
        private static readonly long TEACHER_KNIFE = itemId++;
        private static readonly long TACTICIAN_ROD = itemId++;
        private static readonly long SPIKED_SHIELD = itemId++;
        private static readonly long BATTLEMAIDEN_ARMOR = itemId++;
        private static readonly long GLADIATOR_HELMET = itemId++;
        private static readonly long LANCER_GAUNTLETS = itemId++;
        private static readonly long LION_CHARM = itemId++;

        private static readonly long BLUEBOLT_STAFF = itemId++;
        private static readonly long LAPIS_SWORD = itemId++;
        private static readonly long SHOCKWAVE_TOME = itemId++;
        private static readonly long BATTERY_SHIELD = itemId++;
        private static readonly long RAIJU_CROWN = itemId++;
        private static readonly long STATICSHOCK_EARRINGS = itemId++;
        private static readonly long STORMDANCE_GOWN = itemId++;
        private static readonly long BLACKBOLT_RIBBON = itemId++;

        private static readonly long CRANE_KATANA = itemId++;
        private static readonly long FALCONFEATHER_DAGGER = itemId++;
        private static readonly long TORNADO_STAFF = itemId++;
        private static readonly long CLOUD_GUARD = itemId++;
        private static readonly long HERMES_BOW = itemId++;
        private static readonly long TALON_CHARM = itemId++;
        private static readonly long TINY_WINGS = itemId++;
        private static readonly long FEATHERED_OVERCOAT = itemId++;

        private static readonly long SANDPRIESTESS_SPEAR = itemId++;
        private static readonly long FLAMEDANCER_DAGGER = itemId++;
        private static readonly long WHITEFLAME_STAFF = itemId++;
        private static readonly long SACRED_SHIELD = itemId++;
        private static readonly long MARBLE_CLASP = itemId++;
        private static readonly long SUN_PENDANT = itemId++;
        private static readonly long TINY_HOURGLASS = itemId++;
        private static readonly long DESERT_EARRINGS = itemId++;

        private static readonly long GIANT_STONE_CLUB = itemId++;
        private static readonly long RUINS_SWORD = itemId++;
        private static readonly long MOUNTAIN_STAFF = itemId++;
        private static readonly long BOULDER_SHIELD = itemId++;
        private static readonly long GOLEMS_CLAYMORE = itemId++;
        private static readonly long STONEPLATE_ARMOR = itemId++;
        private static readonly long SACREDSTONE_CHARM = itemId++;
        private static readonly long CLAY_RABBIT = itemId++;

        private static readonly long WATERFALL_POLEARM = itemId++;
        private static readonly long VORPAL_DAO = itemId++;
        private static readonly long JADE_STAFF = itemId++;
        private static readonly long REFLECTION_SHIELD = itemId++;
        private static readonly long BUTTERFLY_HAIRPIN = itemId++;
        private static readonly long WATERMAGE_PENDANT = itemId++;
        private static readonly long RAINDROP_EARRINGS = itemId++;
        private static readonly long AQUAMARINE_BRACELET = itemId++;

        private static readonly long GLACIER_SPEAR = itemId++;
        private static readonly long FROST_DAGGER = itemId++;
        private static readonly long FROZEN_STAFF = itemId++;
        private static readonly long COLDSTEEL_SHIELD = itemId++;
        private static readonly long POLAR_COAT = itemId++;
        private static readonly long ICICLE_EARRINGS = itemId++;
        private static readonly long WINTER_HAT = itemId++;
        private static readonly long SNOW_BOOTS = itemId++;

        private static readonly long SPEAR_OF_REMORSE = itemId++;
        private static readonly long MEMORY_GREATSWORD = itemId++;
        private static readonly long STAFF_OF_SORROW = itemId++;
        private static readonly long SHIELD_OF_SMILES = itemId++;
        private static readonly long LONESOME_PENDANT = itemId++;
        private static readonly long SPARK_OF_DETERMINATION = itemId++;
        private static readonly long CROWN_OF_LOVE = itemId++;
        private static readonly long COMFORTING_COAT = itemId++;

        private static readonly long RIGHTHAND_CAST = itemId++;
        private static readonly long LEFTHAND_CAST = itemId++;
        private static readonly long HEXED_BLINDFOLD = itemId++;
        private static readonly long ANGELS_HALO = itemId++;
        private static readonly long UNSACRED_PENDANT = itemId++;
        private static readonly long WHITEWING_BRACELET = itemId++;
        private static readonly long DARKCRYSTAL_ROSE = itemId++;
        private static readonly long DARK_WINGS = itemId++;

        private static readonly long GIANT_PAINTBRUSH = itemId++;
        private static readonly long SEWING_SWORD = itemId++;
        private static readonly long SKETCHBOOK = itemId++;
        private static readonly long PALETTE_SHIELD = itemId++;
        private static readonly long HANDMADE_CHARM = itemId++;
        private static readonly long PAINTERS_BERET = itemId++;
        private static readonly long ARTIST_SMOCK = itemId++;
        private static readonly long COLORFUL_EARRINGS = itemId++;

        private static readonly long DAYLIGHT_SWORD = itemId++;
        private static readonly long NIGHTGLEAM_SWORD = itemId++;
        private static readonly long SPEAR_OF_WINDS = itemId++;
        private static readonly long SPEAR_OF_RAINS = itemId++;
        private static readonly long HEAVENS_CODEX = itemId++;
        private static readonly long HELLS_CODEX = itemId++;
        private static readonly long ROBE_OF_LIGHTS = itemId++;
        private static readonly long ROBE_OF_DARK = itemId++;

        private static readonly long HOOKED_STAFF = itemId++;
        private static readonly long SPRINGLOADED_SCYTHE = itemId++;
        private static readonly long HIDDEN_BLADE = itemId++;
        private static readonly long SHARPEDGED_SHIELD = itemId++;
        private static readonly long POINTED_RING = itemId++;
        private static readonly long CROWN_OF_SWORDS = itemId++;
        private static readonly long BLADED_CLOAK = itemId++;
        private static readonly long GREATSWORD_PENDANT = itemId++;

        private static readonly long RUSTED_GREATSWORD = itemId++;
        private static readonly long SAND_SHOVEL = itemId++;
        private static readonly long SALTWATER_STAFF = itemId++;
        private static readonly long LARGE_UMBRELLA = itemId++;
        private static readonly long ONEPIECE_SWIMSUIT = itemId++;
        private static readonly long STRAW_HAT = itemId++;
        private static readonly long LARGE_ANCHOR = itemId++;
        private static readonly long BEACH_SANDALS = itemId++;

        private static readonly long STRONGMANS_BARD = itemId++;
        private static readonly long SPINNING_CHAKRAM = itemId++;
        private static readonly long RIBBONED_STAFF = itemId++;
        private static readonly long TRICK_SHIELD = itemId++;
        private static readonly long ROSERED_LEOTARD = itemId++;
        private static readonly long JESTERS_HAT = itemId++;
        private static readonly long RAINBOW_CAPE = itemId++;
        private static readonly long PERFORMERS_SHOES = itemId++;

        private static readonly long IRON_PICKAXE = itemId++;
        private static readonly long DYNAMITE_STAFF = itemId++;
        private static readonly long FOSSIL_DAGGER = itemId++;
        private static readonly long DRILL_SHIELD = itemId++;
        private static readonly long CANARY_CHARM = itemId++;
        private static readonly long PYRITE_EARRINGS = itemId++;
        private static readonly long CAVERS_CLOAK = itemId++;
        private static readonly long MINERS_HEADLAMP = itemId++;

        private static readonly long TINY_FORK = itemId++;
        private static readonly long STIRRING_SPOON = itemId++;
        private static readonly long FANCIFUL_BOOK = itemId++;
        private static readonly long APPLE_PLATE = itemId++;
        private static readonly long VANILLA_WAFERS = itemId++;
        private static readonly long CARAMEL_TEA = itemId++;
        private static readonly long STRAWBERRY_CAKE = itemId++;
        private static readonly long SWEET_TAFFY = itemId++;

        #region Item Dictionary
        private static readonly Dictionary<String, long> ITEMS = new()
        {
            ["Raven Grimoire"] = RAVEN_GRIMOIRE,
            ["Blackwing Staff"] = BLACKWING_STAFF,
            ["Curse Talon"] = CURSE_TALON,
            ["Darkmagic Blade"] = DARKMAGIC_BLADE,
            ["Witch's Cloak"] = WITCHS_CLOAK,
            ["Crowfeather Hairpin"] = CROWFEATHER_HAIRPIN,
            ["Redblack Ribbon"] = REDBLACK_RIBBON,
            ["Opal Necklace"] = OPAL_NECKLACE,
            ["Sleeping Greatbow"] = SLEEPING_GREATBOW,
            ["Crescentmoon Dagger"] = CRESCENTMOON_DAGGER,
            ["Lullaby Harp"] = LULLABY_HARP,
            ["Nightstar Grimoire"] = NIGHTSTAR_GRIMOIRE,
            ["Moon Pendant"] = MOON_PENDANT,
            ["Pajama Hat"] = PAJAMA_HAT,
            ["Stuffed Rabbit"] = STUFFED_RABBIT,
            ["Nightingale Gown"] = NIGHTINGALE_GOWN,
            ["Eternity Flute"] = ETERNITY_FLUTE,
            ["Timewarp Wand"] = TIMEWARP_WAND,
            ["Chrome Shield"] = CHROME_SHIELD,
            ["Clockwork Tome"] = CLOCKWORK_TOME,
            ["Metronome Boots"] = METRONOME_BOOTS,
            ["Timemage Cap"] = TIMEMAGE_CAP,
            ["Starry Cloak"] = STARRY_CLOAK,
            ["Gemini Necklace"] = GEMINI_NECKLACE,
            ["Hawkfeather Fan"] = HAWKFEATHER_FAN,
            ["Windbite Dagger"] = WINDBITE_DAGGER,
            ["Pidgeon Bow"] = PIDGEON_BOW,
            ["Shinsoku Katana"] = SHINSOKU_KATANA,
            ["Eaglewing Charm"] = EAGLEWING_CHARM,
            ["Sparrow Feather"] = SPARROW_FEATHER,
            ["Winged Cap"] = WINGED_CAP,
            ["Thief's Coat"] = THIEFS_COAT,
            ["Vampiric Dagger"] = VAMPRIC_DAGGER,
            ["Bloody Bandage"] = BLOODY_BANDAGE,
            ["Leech Staff"] = LEECH_STAFF,
            ["Bloodhound Greatsword"] = BLOODHOUND_GREATSWORD,
            ["Reaper Cloak"] = REAPER_CLOAK,
            ["Bloodflower Brooch"] = BLOODFLOWER_BROOCH,
            ["Wolf Hood"] = WOLF_HOOD,
            ["Blood Vial"] = BLOOD_VIAL,
            ["Black Wakizashi"] = BLACK_WAKIZASHI,
            ["Throwing Dagger"] = THROWING_DAGGER,
            ["Assassin's Knife"] = ASSASSINS_KNIFE,
            ["Ninjutsu Scroll"] = NINJUTSU_SCROLL,
            ["Shadow Bracelet"] = SHADOW_BRACELET,
            ["Ninja Robe"] = NINJA_ROBE,
            ["Kunoichi Hood"] = KUNOICHI_HOOD,
            ["Shinobi Tabi"] = SHINOBI_TABI,
            ["Dragonhead Spear"] = DRAGONHEAD_SPEAR,
            ["Granite Greatsword"] = GRANITE_GREATSWORD,
            ["Greysteel Shield"] = GREYSTEEL_SHIELD,
            ["Stonebreaker Staff"] = STONEBREAKER_STAFF,
            ["Tough Gauntlet"] = TOUGH_GAUNTLET,
            ["Rockdragon Mail"] = ROCKDRAGON_MAIL,
            ["Obsidian Hairpin"] = OBSIDIAN_HAIRPIN,
            ["Iron Greaves"] = IRON_GREAVES,
            ["Volcano Spear"] = VOLCANO_SPEAR,
            ["Reddragon Blade"] = REDDRAGON_BLADE,
            ["Flame Bow"] = FLAME_BOW,
            ["Meteor Staff"] = METEOR_STAFF,
            ["Phoenix Charm"] = PHOENIX_CHARM,
            ["Firescale Corset"] = FIRESCALE_CORSET,
            ["Demon Horns"] = DEMON_HORNS,
            ["Flamewalker Boots"] = FLAMEWALKER_BOOTS,
            ["Diamond Shield"] = DIAMOND_SHIELD,
            ["Peridot Rapier"] = PERIDOT_RAPIER,
            ["Garnet Staff"] = GARNET_STAFF,
            ["Sapphire Violin"] = SAPPHIRE_VIOLIN,
            ["Emerald Chestplate"] = EMERALD_CHESTPLATE,
            ["Amethyst Bracelet"] = AMETHYST_BRACELET,
            ["Topaz Charm"] = TOPAZ_CHARM,
            ["Ruby Circlet"] = RUBY_CIRCLET,
            ["Brightstorm Spear"] = BRIGHTSTORM_SPEAR,
            ["Bolt Staff"] = BOLT_STAFF,
            ["Lightning Bow"] = LIGHTNING_BOW,
            ["Darkstorm Knife"] = DARKSTORM_KNIFE,
            ["Darkcloud Necklace"] = DARKCLOUD_NECKLACE,
            ["Crown of Storms"] = CROWN_OF_STORMS,
            ["Thunderclap Gloves"] = THUNDERCLAP_GLOVES,
            ["Storm Petticoat"] = STORM_PETTICOAT,
            ["Holy Greatsword"] = HOLY_GREATSWORD,
            ["Sacred Bow"] = SACRED_BOW,
            ["Purification Rod"] = PURIFICATION_ROD,
            ["Ornamental Bell"] = ORNAMENTAL_BELL,
            ["Shrinemaiden's Kosode"] = SHRINEMAIDENS_KOSODE,
            ["Redwhite Ribbon"] = REDWHITE_RIBBON,
            ["Divine Mirror"] = DIVINE_MIRROR,
            ["Golden Chime"] = GOLDEN_CHIME,
            ["Book of Cheats"] = BOOK_OF_CHEATS,
            ["Golden Katana"] = GOLDEN_KATANA,
            ["Glittering Trumpet"] = GLITTERING_TRUMPET,
            ["Royal Staff"] = ROYAL_STAFF,
            ["Ballroom Gown"] = BALLROOM_GOWN,
            ["Silver Coin"] = SILVER_COIN,
            ["Queen's Crown"] = QUEENS_CROWN,
            ["Mimick Rabbitfoot"] = MIMICK_RABBITFOOT,
            ["Butterfly Ocarina"] = BUTTERFLY_OCARINA,
            ["Fairy Spear"] = FAIRY_SPEAR,
            ["Moss Shield"] = MOSS_SHIELD,
            ["Floral Bow"] = FLORAL_BOW,
            ["Blue Rose"] = BLUE_ROSE,
            ["Sunflower Crown"] = SUNFLOWER_CROWN,
            ["Midsummer Dress"] = MIDSUMMER_DRESS,
            ["Grasswoven Bracelet"] = GRASSWOVEN_BRACELET,
            ["Snakefang Dagger"] = SNAKEFANG_DAGGER,
            ["Ivy Staff"] = IVY_STAFF,
            ["Deathcap Tome"] = DEATHCAP_TOME,
            ["Spiderbite Bow"] = SPIDERBITE_BOW,
            ["Compound Gloves"] = COMPOUND_GLOVES,
            ["Poisonfrog Charm"] = POISONFROG_CHARM,
            ["Venom Hood"] = VENOM_HOOD,
            ["Chemist's Coat"] = CHEMISTS_COAT,
            ["Seashell Shield"] = SEASHEEL_SHIELD,
            ["Necronomicon"] = NECRONOMICON,
            ["Tidal Greatsword"] = TIDAL_GREATSWORD,
            ["Occult Dagger"] = OCCULT_DAGGER,
            ["Mermaid Scalemail"] = MERMAID_SCALEMAIL,
            ["Hydrous Blob"] = HYDROUS_BLOB,
            ["Abyss Artifact"] = ABYSS_ARTIFACT,
            ["Lost Pendant"] = LOST_PENDANT,
            ["Sawtooth Cleaver"] = SAWTOOTH_CLEAVER,
            ["Raven's Dagger"] = RAVENS_DAGGER,
            ["Killing Note"] = KILLING_NOTE,
            ["Blacksteel Buckler"] = BLACKSTEEL_BUCKLER,
            ["Nightguard Gloves"] = NIGHTGUARD_GLOVES,
            ["Sniper's Eyeglasses"] = SNIPERS_EYEGLASSES,
            ["Darkmage Charm"] = DARKMAGE_CHARM,
            ["Firststrike Bracelet"] = FIRSTSTRIKE_BRACELET,
            ["Obsidian Rod"] = OBSIDIAN_ROD,
            ["Darkglass Spear"] = DARKGLASS_SPEAR,
            ["Timespace Dagger"] = TIMESPACE_DAGGER,
            ["Quartz Shield"] = QUARTZ_SHIELD,
            ["Pocketwatch"] = POCKETWATCH,
            ["Nova Crown"] = NOVA_CROWN,
            ["Blackhole Charm"] = BLACKHOLE_CHARM,
            ["Twinstar Earrings"] = TWINSTAR_EARRINGS,
            ["Kyou No Omikuji"] = KYOU_NO_OMIKUJI,
            ["Youkai Bracelet"] = YOUKAI_BRACELET,
            ["Oni Staff"] = ONI_STAFF,
            ["Kappa Shield"] = KAPPA_SHIELD,
            ["Usagi Kamen"] = USAGI_KAMEN,
            ["Red Tanzaku"] = RED_TANZAKU,
            ["Vega Spear"] = VEGA_SPEAR,
            ["Altair Dagger"] = ALTAIR_DAGGER,
            ["Ghost Spear"] = GHOST_SPEAR,
            ["Phantom Dagger"] = PHANTOM_DAGGER,
            ["Cursed Candlestaff"] = CURSED_CANDLESTAFF,
            ["Haunted Gloves"] = HAUNTED_GLOVES,
            ["Old Bonnet"] = OLD_BONNET,
            ["Maid Outfit"] = MAID_OUTFIT,
            ["Calling Bell"] = CALLING_BELL,
            ["Smoke Shield"] = SMOKE_SHIELD,
            ["Grandmaster Spear"] = GRANDMASTER_SPEAR,
            ["Teacher Knife"] = TEACHER_KNIFE,
            ["Tactician Rod"] = TACTICIAN_ROD,
            ["Spiked Shield"] = SPIKED_SHIELD,
            ["Battlemaiden Armor"] = BATTLEMAIDEN_ARMOR,
            ["Gladiator Helmet"] = GLADIATOR_HELMET,
            ["Lancer Gauntlets"] = LANCER_GAUNTLETS,
            ["Lion Charm"] = LION_CHARM,
            ["Bluebolt Staff"] = BLUEBOLT_STAFF,
            ["Lapis Sword"] = LAPIS_SWORD,
            ["Shockwave Tome"] = SHOCKWAVE_TOME,
            ["Battery Shield"] = BATTERY_SHIELD,
            ["Raiju Crown"] = RAIJU_CROWN,
            ["Staticshock Earrings"] = STATICSHOCK_EARRINGS,
            ["Stormdance Gown"] = STORMDANCE_GOWN,
            ["Blackbolt Ribbon"] = BLACKBOLT_RIBBON,
            ["Crane Katana"] = CRANE_KATANA,
            ["Falconfeather Dagger"] = FALCONFEATHER_DAGGER,
            ["Tornado Staff"] = TORNADO_STAFF,
            ["Cloud Guard"] = CLOUD_GUARD,
            ["Hermes Bow"] = HERMES_BOW,
            ["Talon Charm"] = TALON_CHARM,
            ["Tiny Wings"] = TINY_WINGS,
            ["Feathered Overcoat"] = FEATHERED_OVERCOAT,
            ["Sandpriestess Spear"] = SANDPRIESTESS_SPEAR,
            ["Flamedancer Dagger"] = FLAMEDANCER_DAGGER,
            ["Whiteflame Staff"] = WHITEFLAME_STAFF,
            ["Sacred Shield"] = SACRED_SHIELD,
            ["Marble Clasp"] = MARBLE_CLASP,
            ["Sun Pendant"] = SUN_PENDANT,
            ["Tiny Hourglass"] = TINY_HOURGLASS,
            ["Desert Earrings"] = DESERT_EARRINGS,
            ["Giant Stone Club"] = GIANT_STONE_CLUB,
            ["Ruins Sword"] = RUINS_SWORD,
            ["Mountain Staff"] = MOUNTAIN_STAFF,
            ["Boulder Shield"] = BOULDER_SHIELD,
            ["Golem's Claymore"] = GOLEMS_CLAYMORE,
            ["Stoneplate Armor"] = STONEPLATE_ARMOR,
            ["Sacredstone Charm"] = SACREDSTONE_CHARM,
            ["Clay Rabbit"] = CLAY_RABBIT,
            ["Waterfall Polearm"] = WATERFALL_POLEARM,
            ["Vorpal Dao"] = VORPAL_DAO,
            ["Jade Staff"] = JADE_STAFF,
            ["Reflection Shield"] = REFLECTION_SHIELD,
            ["Butterfly Hairpin"] = BUTTERFLY_HAIRPIN,
            ["Watermage Pendant"] = WATERMAGE_PENDANT,
            ["Raindrop Earrings"] = RAINDROP_EARRINGS,
            ["Aquamarine Bracelet"] = AQUAMARINE_BRACELET,
            ["Glacier Spear"] = GLACIER_SPEAR,
            ["Frost Dagger"] = FROST_DAGGER,
            ["Frozen Staff"] = FROZEN_STAFF,
            ["Coldsteel Shield"] = COLDSTEEL_SHIELD,
            ["Polar Coat"] = POLAR_COAT,
            ["Icicle Earrings"] = ICICLE_EARRINGS,
            ["Winter Hat"] = WINTER_HAT,
            ["Snow Boots"] = SNOW_BOOTS,
            ["Spear of Remorse"] = SPEAR_OF_REMORSE,
            ["Memory Greatsword"] = MEMORY_GREATSWORD,
            ["Staff of Sorrow"] = STAFF_OF_SORROW,
            ["Shield of Smiles"] = SHIELD_OF_SMILES,
            ["Lonesome Pendant"] = LONESOME_PENDANT,
            ["Spark of Determination"] = SPARK_OF_DETERMINATION,
            ["Crown of Love"] = CROWN_OF_LOVE,
            ["Comforting Coat"] = COMFORTING_COAT,
            ["Righthand Cast"] = RIGHTHAND_CAST,
            ["Lefthand Cast"] = LEFTHAND_CAST,
            ["Hexed Blindfold"] = HEXED_BLINDFOLD,
            ["Angel's Halo"] = ANGELS_HALO,
            ["Unsacred Pendant"] = UNSACRED_PENDANT,
            ["Whitewing Bracelet"] = WHITEWING_BRACELET,
            ["Darkcrystal Rose"] = DARKCRYSTAL_ROSE,
            ["Dark Wings"] = DARK_WINGS,
            ["Giant Paintbrush"] = GIANT_PAINTBRUSH,
            ["Sewing Sword"] = SEWING_SWORD,
            ["Sketchbook"] = SKETCHBOOK,
            ["Palette Shield"] = PALETTE_SHIELD,
            ["Handmade Charm"] = HANDMADE_CHARM,
            ["Painter's Beret"] = PAINTERS_BERET,
            ["Artist Smock"] = ARTIST_SMOCK,
            ["Colorful Earrings"] = COLORFUL_EARRINGS,
            ["Daylight Sword"] = DAYLIGHT_SWORD,
            ["Nightgleam Sword"] = NIGHTGLEAM_SWORD,
            ["Spear of Winds"] = SPEAR_OF_WINDS,
            ["Spear of Rains"] = SPEAR_OF_RAINS,
            ["Heaven's Codex"] = HEAVENS_CODEX,
            ["Hell's Codex"] = HELLS_CODEX,
            ["Robe of Light"] = ROBE_OF_LIGHTS,
            ["Robe of Dark"] = ROBE_OF_DARK,
            ["Hooked Staff"] = HOOKED_STAFF,
            ["Springloaded Scythe"] = SPRINGLOADED_SCYTHE,
            ["Hidden Blade"] = HIDDEN_BLADE,
            ["Sharpedged Shield"] = SHARPEDGED_SHIELD,
            ["Pointed Ring"] = POINTED_RING,
            ["Crown of Swords"] = CROWN_OF_SWORDS,
            ["Bladed Cloak"] = BLADED_CLOAK,
            ["Greatsword Pendant"] = GREATSWORD_PENDANT,
            ["Rusted Greatsword"] = RUSTED_GREATSWORD,
            ["Sand Shovel"] = SAND_SHOVEL,
            ["Saltwater Staff"] = SALTWATER_STAFF,
            ["Large Umbrella"] = LARGE_UMBRELLA,
            ["Onepiece Swimsuit"] = ONEPIECE_SWIMSUIT,
            ["Straw Hat"] = STRAW_HAT,
            ["Large Anchor"] = LARGE_ANCHOR,
            ["Beach Sandals"] = BEACH_SANDALS,
            ["Strongman's Bar"] = STRONGMANS_BARD,
            ["Spinning Chakram"] = SPINNING_CHAKRAM,
            ["Ribboned Staff"] = RIBBONED_STAFF,
            ["Trick Shield"] = TRICK_SHIELD,
            ["Rosered Leotard"] = ROSERED_LEOTARD,
            ["Jester's Hat"] = JESTERS_HAT,
            ["Rainbow Cape"] = RAINBOW_CAPE,
            ["Performer's Shoes"] = PERFORMERS_SHOES,
            ["Iron Pickaxe"] = IRON_PICKAXE,
            ["Dynamite Staff"] = DYNAMITE_STAFF,
            ["Fossil Dagger"] = FOSSIL_DAGGER,
            ["Drill Shield"] = DRILL_SHIELD,
            ["Canary Charm"] = CANARY_CHARM,
            ["Pyrite Earrings"] = PYRITE_EARRINGS,
            ["Caver's Cloak"] = CAVERS_CLOAK,
            ["Miner's Headlamp"] = MINERS_HEADLAMP,
            ["Tiny Fork"] = TINY_FORK,
            ["Stirring Spoon"] = STIRRING_SPOON,
            ["Fanciful Book"] = FANCIFUL_BOOK,
            ["Apple Plate"] = APPLE_PLATE,
            ["Vanilla Wafers"] = VANILLA_WAFERS,
            ["Caramel Tea"] = CARAMEL_TEA,
            ["Strawberry Cake"] = STRAWBERRY_CAKE,
            ["Sweet Taffy"] = SWEET_TAFFY
        };
        #endregion

        #endregion

        #region Itemsets
        private static readonly long[] ARCANE_SET = [RAVEN_GRIMOIRE, BLACKWING_STAFF, CURSE_TALON, DARKMAGIC_BLADE, WITCHS_CLOAK, CROWFEATHER_HAIRPIN, REDBLACK_RIBBON, OPAL_NECKLACE];

        private static readonly long[] NIGHT_SET = [SLEEPING_GREATBOW, CRESCENTMOON_DAGGER, LULLABY_HARP, NIGHTSTAR_GRIMOIRE, MOON_PENDANT, PAJAMA_HAT, STUFFED_RABBIT, NIGHTINGALE_GOWN];

        private static readonly long[] TIMESPACE_SET = [ETERNITY_FLUTE, TIMEWARP_WAND, CHROME_SHIELD, CLOCKWORK_TOME, METRONOME_BOOTS, TIMEMAGE_CAP, STARRY_CLOAK, GEMINI_NECKLACE];

        private static readonly long[] WIND_SET = [HAWKFEATHER_FAN, WINDBITE_DAGGER, PIDGEON_BOW, SHINSOKU_KATANA, EAGLEWING_CHARM, SPARROW_FEATHER, WINGED_CAP, THIEFS_COAT];

        private static readonly long[] BLOODWOLF_SET = [VAMPRIC_DAGGER, BLOODY_BANDAGE, LEECH_STAFF, BLOODHOUND_GREATSWORD, REAPER_CLOAK, BLOODFLOWER_BROOCH, WOLF_HOOD, BLOOD_VIAL];

        private static readonly long[] ASSASSIN_SET = [BLACK_WAKIZASHI, THROWING_DAGGER, ASSASSINS_KNIFE, NINJUTSU_SCROLL, SHADOW_BRACELET, NINJA_ROBE, KUNOICHI_HOOD, SHINOBI_TABI];

        private static readonly long[] ROCKDRAGON_SET = [DRAGONHEAD_SPEAR, GRANITE_GREATSWORD, GREYSTEEL_SHIELD, STONEBREAKER_STAFF, TOUGH_GAUNTLET, ROCKDRAGON_MAIL, OBSIDIAN_HAIRPIN, IRON_GREAVES];

        private static readonly long[] FLAME_SET = [VOLCANO_SPEAR, REDDRAGON_BLADE, FLAME_BOW, METEOR_STAFF, PHOENIX_CHARM, FIRESCALE_CORSET, DEMON_HORNS, FLAMEWALKER_BOOTS];

        private static readonly long[] GEM_SET = [DIAMOND_SHIELD, PERIDOT_RAPIER, GARNET_STAFF, SAPPHIRE_VIOLIN, EMERALD_CHESTPLATE, AMETHYST_BRACELET, TOPAZ_CHARM, RUBY_CIRCLET];

        private static readonly long[] LIGHTNING_SET = [BRIGHTSTORM_SPEAR, BOLT_STAFF, LIGHTNING_BOW, DARKSTORM_KNIFE, DARKCLOUD_NECKLACE, CROWN_OF_STORMS, THUNDERCLAP_GLOVES, STORM_PETTICOAT];

        private static readonly long[] SHRINE_SET = [HOLY_GREATSWORD, SACRED_BOW, PURIFICATION_ROD, ORNAMENTAL_BELL, SHRINEMAIDENS_KOSODE, REDWHITE_RIBBON, DIVINE_MIRROR, GOLDEN_CHIME];

        private static readonly long[] LUCKY_SET = [BOOK_OF_CHEATS, GOLDEN_KATANA, GLITTERING_TRUMPET, ROYAL_STAFF, BALLROOM_GOWN, SILVER_COIN, QUEENS_CROWN, MIMICK_RABBITFOOT];

        private static readonly long[] LIFE_SET = [BUTTERFLY_OCARINA, FAIRY_SPEAR, MOSS_SHIELD, FLORAL_BOW, BLUE_ROSE, SUNFLOWER_CROWN, MIDSUMMER_DRESS, GRASSWOVEN_BRACELET];

        private static readonly long[] POISON_SET = [SNAKEFANG_DAGGER, IVY_STAFF, DEATHCAP_TOME, SPIDERBITE_BOW, COMPOUND_GLOVES, POISONFROG_CHARM, VENOM_HOOD, CHEMISTS_COAT];

        private static readonly long[] DEPTH_SET = [SEASHEEL_SHIELD, NECRONOMICON, TIDAL_GREATSWORD, OCCULT_DAGGER, MERMAID_SCALEMAIL, HYDROUS_BLOB, ABYSS_ARTIFACT, LOST_PENDANT];

        private static readonly long[] DARKBITE_SET = [SAWTOOTH_CLEAVER, RAVENS_DAGGER, KILLING_NOTE, BLACKSTEEL_BUCKLER, NIGHTGUARD_GLOVES, SNIPERS_EYEGLASSES, DARKMAGE_CHARM, FIRSTSTRIKE_BRACELET];

        private static readonly long[] TIMEGEM_SET = [OBSIDIAN_ROD, DARKGLASS_SPEAR, TIMESPACE_DAGGER, QUARTZ_SHIELD, POCKETWATCH, NOVA_CROWN, BLACKHOLE_CHARM, TWINSTAR_EARRINGS];

        private static readonly long[] YOUKAI_SET = [KYOU_NO_OMIKUJI, YOUKAI_BRACELET, ONI_STAFF, KAPPA_SHIELD, USAGI_KAMEN, RED_TANZAKU, VEGA_SPEAR, ALTAIR_DAGGER];

        private static readonly long[] HAUNTED_SET = [GHOST_SPEAR, PHANTOM_DAGGER, CURSED_CANDLESTAFF, HAUNTED_GLOVES, OLD_BONNET, MAID_OUTFIT, CALLING_BELL, SMOKE_SHIELD];

        private static readonly long[] GLADIATOR_SET = [GRANDMASTER_SPEAR, TEACHER_KNIFE, TACTICIAN_ROD, SPIKED_SHIELD, BATTLEMAIDEN_ARMOR, GLADIATOR_HELMET, LANCER_GAUNTLETS, LION_CHARM];

        private static readonly long[] SPARKBLADE_SET = [BLUEBOLT_STAFF, LAPIS_SWORD, SHOCKWAVE_TOME, BATTERY_SHIELD, RAIJU_CROWN, STATICSHOCK_EARRINGS, STORMDANCE_GOWN, BLACKBOLT_RIBBON];

        private static readonly long[] SWIFTFLIGHT_SET = [CRANE_KATANA, FALCONFEATHER_DAGGER, TORNADO_STAFF, CLOUD_GUARD, HERMES_BOW, TALON_CHARM, TINY_WINGS, FEATHERED_OVERCOAT];

        private static readonly long[] SACREDFLAME_SET = [SANDPRIESTESS_SPEAR, FLAMEDANCER_DAGGER, WHITEFLAME_STAFF, SACRED_SHIELD, MARBLE_CLASP, SUN_PENDANT, TINY_HOURGLASS, DESERT_EARRINGS];

        private static readonly long[] RUINS_SET = [GIANT_STONE_CLUB, RUINS_SWORD, MOUNTAIN_STAFF, BOULDER_SHIELD, GOLEMS_CLAYMORE, STONEPLATE_ARMOR, SACREDSTONE_CHARM, CLAY_RABBIT];

        private static readonly long[] LAKESHRINE_SET = [WATERFALL_POLEARM, VORPAL_DAO, JADE_STAFF, REFLECTION_SHIELD, BUTTERFLY_HAIRPIN, WATERMAGE_PENDANT, RAINDROP_EARRINGS, AQUAMARINE_BRACELET];

        private static readonly long[] GLACIER_SET = [GLACIER_SPEAR, FROST_DAGGER, FROZEN_STAFF, COLDSTEEL_SHIELD, POLAR_COAT, ICICLE_EARRINGS, WINTER_HAT, SNOW_BOOTS];

        private static readonly long[] MEMORY_SET = [SPEAR_OF_REMORSE, MEMORY_GREATSWORD, STAFF_OF_SORROW, SHIELD_OF_SMILES, LONESOME_PENDANT, SPARK_OF_DETERMINATION, CROWN_OF_LOVE, COMFORTING_COAT];

        private static readonly long[] CULTIST_SET = [RIGHTHAND_CAST, LEFTHAND_CAST, HEXED_BLINDFOLD, ANGELS_HALO, UNSACRED_PENDANT, WHITEWING_BRACELET, DARKCRYSTAL_ROSE, DARK_WINGS];

        private static readonly long[] PAINTERS_SET = [GIANT_PAINTBRUSH, SEWING_SWORD, SKETCHBOOK, PALETTE_SHIELD, HANDMADE_CHARM, PAINTERS_BERET, ARTIST_SMOCK, COLORFUL_EARRINGS];

        private static readonly long[] DAYNIGHT_SET = [DAYLIGHT_SWORD, NIGHTGLEAM_SWORD, SPEAR_OF_WINDS, SPEAR_OF_RAINS, HEAVENS_CODEX, HELLS_CODEX, ROBE_OF_LIGHTS, ROBE_OF_DARK];

        private static readonly long[] SHARPEDGE_SET = [HOOKED_STAFF, SPRINGLOADED_SCYTHE, HIDDEN_BLADE, SHARPEDGED_SHIELD, POINTED_RING, CROWN_OF_SWORDS, BLADED_CLOAK, GREATSWORD_PENDANT];

        private static readonly long[] OCEANS_SET = [RUSTED_GREATSWORD, SAND_SHOVEL, SALTWATER_STAFF, LARGE_UMBRELLA, ONEPIECE_SWIMSUIT, STRAW_HAT, LARGE_ANCHOR, BEACH_SANDALS];

        private static readonly long[] PERFORMERS_SET = [STRONGMANS_BARD, SPINNING_CHAKRAM, RIBBONED_STAFF, TRICK_SHIELD, ROSERED_LEOTARD, JESTERS_HAT, RAINBOW_CAPE, PERFORMERS_SHOES];

        private static readonly long[] MINERS_SET = [IRON_PICKAXE, DYNAMITE_STAFF, FOSSIL_DAGGER, DRILL_SHIELD, CANARY_CHARM, PYRITE_EARRINGS, CAVERS_CLOAK, MINERS_HEADLAMP];

        private static readonly long[] TEAPARTY_SET = [TINY_FORK, STIRRING_SPOON, FANCIFUL_BOOK, APPLE_PLATE, VANILLA_WAFERS, CARAMEL_TEA, STRAWBERRY_CAKE, SWEET_TAFFY];
        #endregion

        internal int AvailableTreasurespheres;

        internal static readonly string[] UPGRADES = ["Emerald Gem", "Garnet Gem", "Ruby Gem", "Sapphire Gem", "Opal Gem",
            "Primary Emerald Gem", "Primary Garnet Gem", "Primary Ruby Gem", "Primary Sapphire Gem", "Primary Opal Gem",
            "Secondary Emerald Gem", "Secondary Garnet Gem", "Secondary Ruby Gem", "Secondary Sapphire Gem", "Secondary Opal Gem",
            "Special Emerald Gem", "Special Garnet Gem", "Special Ruby Gem", "Special Sapphire Gem", "Special Opal Gem",
            "Defensive Emerald Gem", "Defensive Garnet Gem", "Defensive Ruby Gem", "Defensive Sapphire Gem", "Defensive Opal Gem"];

        [Flags]
        internal enum PrimaryUpgradeFlags
        {
            None = 0,
            PrimaryEmeraldGem = 0x00001,
            PrimaryGarnetGem = 0x00010,
            PrimaryRubyGem = 0x00100,
            PrimarySapphireGem = 0x01000,
            PrimaryOpalGem = 0x10000,
        }

        internal PrimaryUpgradeFlags AvailablePrimaryUpgrades { get; set; }

        [Flags]
        internal enum SecondaryUpgradeFlags
        {
            None = 0,
            SecondaryEmeraldGem = 0x00001,
            SecondaryGarnetGem = 0x00010,
            SecondaryRubyGem = 0x00100,
            SecondarySapphireGem = 0x01000,
            SecondaryOpalGem = 0x10000,
        }

        internal SecondaryUpgradeFlags AvailableSecondaryUpgrades { get; set; }

        [Flags]
        internal enum SpecialUpgradeFlags
        {
            None = 0,
            SpecialEmeraldGem = 0x00001,
            SpecialGarnetGem = 0x00010,
            SpecialRubyGem = 0x00100,
            SpecialSapphireGem = 0x01000,
            SpecialOpalGem = 0x10000,
        }

        internal SpecialUpgradeFlags AvailableSpecialUpgrades { get; set; }

        [Flags]
        internal enum DefensiveUpgradeFlags
        {
            None = 0,
            DefensiveEmeraldGem = 0x00001,
            DefensiveGarnetGem = 0x00010,
            DefensiveRubyGem = 0x00100,
            DefensiveSapphireGem = 0x01000,
            DefensiveOpalGem = 0x10000,
        }

        internal DefensiveUpgradeFlags AvailableDefensiveUpgrades { get; set; }

        internal static readonly string[] POTIONS = ["Full Heal Potion", "Level Up Potion", "Regen Potion", "Essence of Spell", "Darkness Potion", "Quickening Potion", "Winged Potion",
            "Essence of Wit", "Swifthand Potion", "Fire Potion", "Strength Potion", "Gold Potion", "Luck Potion", "Essence of Steel", "Evasion Potion", "Longarm Potion", "Vitality Potion"];

        internal List<string> AvailablePotions => availablePotions;

        // Handle receiving kingdom related items
        internal void ReceiveItem(ReceivedItemsPacket receivedItem)
        {
            foreach (var item in receivedItem.Items)
            {
                var itemName = data.idToItem.Get<string>(item.Item);
                if (itemName != default)
                {
                    if (KINGDOMS.Contains(itemName))
                    {
                        AvailableKingdoms = AvailableKingdoms | (KingdomFlags)Enum.Parse(typeof(KingdomFlags), itemName.Replace(" ", "_").Replace("'", ""));
                        this.logger.PrintMessage("Kingdoms: " + AvailableKingdoms.ToString(), System.Drawing.Color.DarkOrange);
                        UpdateHallwayOnItemRecieve?.Invoke();
                    }
                    else if (itemName == "Progressive Region")
                    {
                        ProgressiveRegions++;
                        this.logger.PrintMessage("Progressive Regions: " + ProgressiveRegions, System.Drawing.Color.DarkOrange);
                        UpdateHallwayOnItemRecieve?.Invoke();
                    }
                    else if (CLASSES.Contains(itemName))
                    {
                        AvailableClasses = AvailableClasses | (ClassFlags)Enum.Parse(typeof(ClassFlags), itemName);
                        this.logger.PrintMessage("Classes: " + AvailableClasses.ToString(), System.Drawing.Color.DarkOrange);
                    }
                    else if (ITEMSETS.Contains(itemName))
                    {
                        AddItemsFromItemset(itemName);
                        this.logger.PrintMessage("Items: " + String.Join(", ", AvailableItems), System.Drawing.Color.DarkOrange);
                    }
                    else if (ITEMS.TryGetValue(itemName, out long individualItem))
                    {
                        availableItems.Add(individualItem);
                        this.logger.PrintMessage("Items: " + String.Join(", ", AvailableItems), System.Drawing.Color.DarkOrange);
                    }
                    else if (itemName == "Treasuresphere")
                    {
                        AddChest?.Invoke();
                        AvailableTreasurespheres++;
                        this.logger.PrintMessage("Treasuresphers: " + AvailableTreasurespheres, System.Drawing.Color.DarkOrange);
                    }
                    else if (UPGRADES.Contains(itemName))
                    {
                        var enumName = itemName.Replace(" ", "");
                        if (this.UpgradeSanity == InventoryUtil.UpgradeSetting.Simple)
                        {
                            if (enumName.Contains("Emerald"))
                            {
                                AvailablePrimaryUpgrades = AvailablePrimaryUpgrades | PrimaryUpgradeFlags.PrimaryEmeraldGem;
                                AvailableSecondaryUpgrades = AvailableSecondaryUpgrades | SecondaryUpgradeFlags.SecondaryEmeraldGem;
                                AvailableSpecialUpgrades = AvailableSpecialUpgrades | SpecialUpgradeFlags.SpecialEmeraldGem;
                                AvailableDefensiveUpgrades = AvailableDefensiveUpgrades | DefensiveUpgradeFlags.DefensiveEmeraldGem;
                            }
                            else if (enumName.Contains("Garnet"))
                            {
                                AvailablePrimaryUpgrades = AvailablePrimaryUpgrades | PrimaryUpgradeFlags.PrimaryGarnetGem;
                                AvailableSecondaryUpgrades = AvailableSecondaryUpgrades | SecondaryUpgradeFlags.SecondaryGarnetGem;
                                AvailableSpecialUpgrades = AvailableSpecialUpgrades | SpecialUpgradeFlags.SpecialGarnetGem;
                                AvailableDefensiveUpgrades = AvailableDefensiveUpgrades | DefensiveUpgradeFlags.DefensiveGarnetGem;
                            }
                            else if (enumName.Contains("Ruby"))
                            {
                                AvailablePrimaryUpgrades = AvailablePrimaryUpgrades | PrimaryUpgradeFlags.PrimaryRubyGem;
                                AvailableSecondaryUpgrades = AvailableSecondaryUpgrades | SecondaryUpgradeFlags.SecondaryRubyGem;
                                AvailableSpecialUpgrades = AvailableSpecialUpgrades | SpecialUpgradeFlags.SpecialRubyGem;
                                AvailableDefensiveUpgrades = AvailableDefensiveUpgrades | DefensiveUpgradeFlags.DefensiveRubyGem;
                            }
                            else if (enumName.Contains("Sapphire"))
                            {
                                AvailablePrimaryUpgrades = AvailablePrimaryUpgrades | PrimaryUpgradeFlags.PrimarySapphireGem;
                                AvailableSecondaryUpgrades = AvailableSecondaryUpgrades | SecondaryUpgradeFlags.SecondarySapphireGem;
                                AvailableSpecialUpgrades = AvailableSpecialUpgrades | SpecialUpgradeFlags.SpecialSapphireGem;
                                AvailableDefensiveUpgrades = AvailableDefensiveUpgrades | DefensiveUpgradeFlags.DefensiveSapphireGem;
                            }
                            else if (enumName.Contains("Opal"))
                            {
                                AvailablePrimaryUpgrades = AvailablePrimaryUpgrades | PrimaryUpgradeFlags.PrimaryOpalGem;
                                AvailableSecondaryUpgrades = AvailableSecondaryUpgrades | SecondaryUpgradeFlags.SecondaryOpalGem;
                                AvailableSpecialUpgrades = AvailableSpecialUpgrades | SpecialUpgradeFlags.SpecialOpalGem;
                                AvailableDefensiveUpgrades = AvailableDefensiveUpgrades | DefensiveUpgradeFlags.DefensiveOpalGem;
                            }
                            this.logger.PrintMessage("Primaries: " + AvailablePrimaryUpgrades.ToString(), System.Drawing.Color.DarkOrange);
                            this.logger.PrintMessage("Secondaries: " + AvailableSecondaryUpgrades.ToString(), System.Drawing.Color.DarkOrange);
                            this.logger.PrintMessage("Specials: " + AvailableSpecialUpgrades.ToString(), System.Drawing.Color.DarkOrange);
                            this.logger.PrintMessage("Defensives: " + AvailableDefensiveUpgrades.ToString(), System.Drawing.Color.DarkOrange);
                        }
                        else if (enumName.Contains("Primary"))
                        {
                            AvailablePrimaryUpgrades = AvailablePrimaryUpgrades | (PrimaryUpgradeFlags)Enum.Parse(typeof(PrimaryUpgradeFlags), enumName);
                            this.logger.PrintMessage("Primaries: " + AvailablePrimaryUpgrades.ToString(), System.Drawing.Color.DarkOrange);
                        }
                        else if (enumName.Contains("Secondary"))
                        {
                            AvailableSecondaryUpgrades = AvailableSecondaryUpgrades | (SecondaryUpgradeFlags)Enum.Parse(typeof(SecondaryUpgradeFlags), enumName);
                            this.logger.PrintMessage("Secondaries: " + AvailableSecondaryUpgrades.ToString(), System.Drawing.Color.DarkOrange);
                        }
                        else if (enumName.Contains("Special"))
                        {
                            AvailableSpecialUpgrades = AvailableSpecialUpgrades | (SpecialUpgradeFlags)Enum.Parse(typeof(SpecialUpgradeFlags), enumName);
                            this.logger.PrintMessage("Specials: " + AvailableSpecialUpgrades.ToString(), System.Drawing.Color.DarkOrange);
                        }
                        else if (enumName.Contains("Defensive"))
                        {
                            AvailableDefensiveUpgrades = AvailableDefensiveUpgrades | (DefensiveUpgradeFlags)Enum.Parse(typeof(DefensiveUpgradeFlags), enumName);
                            this.logger.PrintMessage("Defensives: " + AvailableDefensiveUpgrades.ToString(), System.Drawing.Color.DarkOrange);
                        }
                    }
                    else if (POTIONS.Contains(itemName))
                    {
                        AvailablePotions.Add(itemName);
                        this.logger.PrintMessage("Potions: " + String.Join(", ", AvailablePotions), System.Drawing.Color.DarkOrange);
                    }
                    else if (itemName.Contains("Defeat"))
                    {
                        if (itemName.Contains("Shira"))
                        {
                            shira_victories.Add(itemName);
                            this.logger.PrintMessage("Shira Victories: " + String.Join(", ", shira_victories), System.Drawing.Color.DarkOrange);
                        }
                        else if (itemName.Contains("Witch"))
                        {
                            witch_victories.Add(itemName);
                            this.logger.PrintMessage("Witch Victories: " + String.Join(", ", witch_victories), System.Drawing.Color.DarkOrange);
                        }

                        if (CheckGoal())
                        {
                            SendGoal?.Invoke();
                        }
                    }
                }

            }
        }

        internal bool CheckGoal()
        {
            if (goal == GoalSetting.Shira)
            {
                if (shira_victories.Count >= shiraKills)
                {
                    return true;
                }
            }
            else if (goal == GoalSetting.Witch)
            {
                if (witch_victories.Count >= witchKills)
                {
                    return true;
                }
            }
            else if (goal == GoalSetting.Both)
            {
                if (shira_victories.Count >= shiraKills && witch_victories.Count >= witchKills)
                {
                    return true;
                }
            }
            return false;
        }

        // Get the notchname from a kingdoms full name
        internal static string KingdomNameToNotch(string name)
        {
            if (name == "Kingdom Outskirts")
            {
                return "hw_outskirts";
            }
            if (name == "Scholar's Nest")
            {
                return "hw_nest";
            }
            if (name == "King's Arsenal")
            {
                return "hw_arsenal";
            }
            if (name == "Emerald Lakeside")
            {
                return "hw_lakeside";
            }
            if (name == "Churchmouse Streets")
            {
                return "hw_streets";
            }
            if (name == "Red Darkhouse")
            {
                return "hw_lighthouse";
            }
            if (name == "Crack in the Geode")
            {
                return "hw_geode";
            }
            if (name == "Darkhouse Depths")
            {
                return "hw_depths";
            }
            if (name == "Atelier Aurum")
            {
                return "hw_aurum";
            }
            if (name == "Subterra Sanctum")
            {
                return "hw_sanct";
            }
            return "";
        }

        internal List<string> GetKingdomKingdomsAvailable(int n = 5)
        {
            var kingdoms = new List<string>();

            if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Kingdom_Outskirts) != 0)
            {
                kingdoms.Add("hw_outskirts");
            }
            else
            {
                return kingdoms;
            }

            // Do we make use of kingdom order at all
            if ((isKingdomSanity && useKingdomOrderWithKingdomSanity) || (!isKingdomSanity && isProgressive))
            {
                for (var i = 0; i < Math.Min(n, maxKingdoms); i++)
                {
                    bool added = false;

                    for (var j = 0; j < kingdomOrder[i].Count; j++)
                    {
                        if (kingdomOrder[i][j] == "hw_nest" && (AvailableKingdoms & InventoryUtil.KingdomFlags.Scholars_Nest) != 0)
                        {
                            kingdoms.Add("hw_nest");
                            added = true;
                        }
                        else if (kingdomOrder[i][j] == "hw_arsenal" && (AvailableKingdoms & InventoryUtil.KingdomFlags.Kings_Arsenal) != 0)
                        {
                            kingdoms.Add("hw_arsenal");
                            added = true;
                        }
                        else if (kingdomOrder[i][j] == "hw_lakeside" && (AvailableKingdoms & InventoryUtil.KingdomFlags.Emerald_Lakeside) != 0)
                        {
                            kingdoms.Add("hw_lakeside");
                            added = true;
                        }
                        else if (kingdomOrder[i][j] == "hw_streets" && (AvailableKingdoms & InventoryUtil.KingdomFlags.Churchmouse_Streets) != 0)
                        {
                            kingdoms.Add("hw_streets");
                            added = true;
                        }
                        else if (kingdomOrder[i][j] == "hw_lighthouse" && (AvailableKingdoms & InventoryUtil.KingdomFlags.Red_Darkhouse) != 0)
                        {
                            kingdoms.Add("hw_lighthouse");
                            added = true;
                        }
                    }

                    if (!added)
                    {
                        break;
                    }
                }
            }
            else
            {
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Scholars_Nest) != 0)
                {
                    kingdoms.Add("hw_nest");
                }
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Kings_Arsenal) != 0)
                {
                    kingdoms.Add("hw_arsenal");
                }
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Emerald_Lakeside) != 0)
                {
                    kingdoms.Add("hw_lakeside");
                }
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Churchmouse_Streets) != 0)
                {
                    kingdoms.Add("hw_streets");
                }
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Red_Darkhouse) != 0)
                {
                    kingdoms.Add("hw_lighthouse");
                }
            }

            kingdoms = GetProgressiveKingdomsAvailable(kingdoms);

            return kingdoms;
        }

        internal List<string> GetExtraKingdomsAvailable(int n = 3)
        {
            var kingdoms = new List<string>();

            if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Crack_in_the_Geode) != 0)
            {
                kingdoms.Add("hw_geode");
            }
            else
            {
                return kingdoms;
            }

            // Do we make use of kingdom order at all
            if ((isKingdomSanity && useKingdomOrderWithKingdomSanity) || (!isKingdomSanity && isProgressive))
            {
                for (var i = 0; i < Math.Min(n, maxKingdoms); i++)
                {
                    bool added = false;

                    for (var j = 0; j < kingdomOrder[i].Count; j++)
                    {
                        if (kingdomOrder[i][j] == "hw_depths" && (AvailableKingdoms & InventoryUtil.KingdomFlags.Darkhouse_Depths) != 0)
                        {
                            kingdoms.Add("hw_depths");
                            added = true;
                        }
                        else if (kingdomOrder[i][j] == "hw_aurum" && (AvailableKingdoms & InventoryUtil.KingdomFlags.Atelier_Aurum) != 0)
                        {
                            kingdoms.Add("hw_aurum");
                            added = true;
                        }
                        else if (kingdomOrder[i][j] == "hw_sanct" && (AvailableKingdoms & InventoryUtil.KingdomFlags.Subterra_Sanctum) != 0)
                        {
                            kingdoms.Add("hw_sanct");
                            added = true;
                        }
                    }

                    if (!added)
                    {
                        break;
                    }
                }
            }
            else
            {
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Darkhouse_Depths) != 0)
                {
                    kingdoms.Add("hw_depths");
                }
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Atelier_Aurum) != 0)
                {
                    kingdoms.Add("hw_aurum");
                }
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Subterra_Sanctum) != 0)
                {
                    kingdoms.Add("hw_sanct");
                }

            }

            kingdoms = GetProgressiveKingdomsAvailable(kingdoms);

            return kingdoms;
        }

        internal List<string> GetChaosKingdomsAvailable(int n = 8)
        {
            var kingdoms = new List<string>();

            if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Kingdom_Outskirts) != 0)
            {
                kingdoms.Add("hw_outskirts");
            }

            if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Crack_in_the_Geode) != 0)
            {
                kingdoms.Add("hw_geode");
            }

            if (kingdoms.Count == 0)
            {
                return kingdoms;
            }

            // Do we make use of kingdom order at all
            if ((isKingdomSanity && useKingdomOrderWithKingdomSanity) || (!isKingdomSanity && isProgressive))
            {
                for (var i = 0; i < Math.Min(n, maxKingdoms); i++)
                {
                    bool added = false;

                    for (var j = 0; j < kingdomOrder[i].Count; j++)
                    {
                        if (kingdomOrder[i][j] == "hw_nest" && (AvailableKingdoms & InventoryUtil.KingdomFlags.Scholars_Nest) != 0)
                        {
                            kingdoms.Add("hw_nest");
                            added = true;
                        }
                        else if (kingdomOrder[i][j] == "hw_arsenal" && (AvailableKingdoms & InventoryUtil.KingdomFlags.Kings_Arsenal) != 0)
                        {
                            kingdoms.Add("hw_arsenal");
                            added = true;
                        }
                        else if (kingdomOrder[i][j] == "hw_lakeside" && (AvailableKingdoms & InventoryUtil.KingdomFlags.Emerald_Lakeside) != 0)
                        {
                            kingdoms.Add("hw_lakeside");
                            added = true;
                        }
                        else if (kingdomOrder[i][j] == "hw_streets" && (AvailableKingdoms & InventoryUtil.KingdomFlags.Churchmouse_Streets) != 0)
                        {
                            kingdoms.Add("hw_streets");
                            added = true;
                        }
                        else if (kingdomOrder[i][j] == "hw_lighthouse" && (AvailableKingdoms & InventoryUtil.KingdomFlags.Red_Darkhouse) != 0)
                        {
                            kingdoms.Add("hw_lighthouse");
                            added = true;
                        }
                        else if (kingdomOrder[i][j] == "hw_depths" && (AvailableKingdoms & InventoryUtil.KingdomFlags.Darkhouse_Depths) != 0)
                        {
                            kingdoms.Add("hw_depths");
                            added = true;
                        }
                        else if (kingdomOrder[i][j] == "hw_aurum" && (AvailableKingdoms & InventoryUtil.KingdomFlags.Atelier_Aurum) != 0)
                        {
                            kingdoms.Add("hw_aurum");
                            added = true;
                        }
                        else if (kingdomOrder[i][j] == "hw_sanct" && (AvailableKingdoms & InventoryUtil.KingdomFlags.Subterra_Sanctum) != 0)
                        {
                            kingdoms.Add("hw_sanct");
                            added = true;
                        }
                    }

                    if (!added)
                    {
                        break;
                    }
                }
            }
            else
            {
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Scholars_Nest) != 0)
                {
                    kingdoms.Add("hw_nest");
                }
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Kings_Arsenal) != 0)
                {
                    kingdoms.Add("hw_arsenal");
                }
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Emerald_Lakeside) != 0)
                {
                    kingdoms.Add("hw_lakeside");
                }
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Churchmouse_Streets) != 0)
                {
                    kingdoms.Add("hw_streets");
                }
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Red_Darkhouse) != 0)
                {
                    kingdoms.Add("hw_lighthouse");
                }
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Darkhouse_Depths) != 0)
                {
                    kingdoms.Add("hw_depths");
                }
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Atelier_Aurum) != 0)
                {
                    kingdoms.Add("hw_aurum");
                }
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Subterra_Sanctum) != 0)
                {
                    kingdoms.Add("hw_sanct");
                }
            }

            kingdoms = GetProgressiveKingdomsAvailable(kingdoms);

            return kingdoms;
        }

        internal List<string> GetProgressiveKingdomsAvailable(List<string> kingdoms)
        {
            if (isProgressive && (!isKingdomSanity || useKingdomOrderWithKingdomSanity))
            {
                for (var i = ProgressiveRegions; i < maxKingdoms; i++)
                {
                    kingdoms = kingdoms.Except(kingdomOrder[i]).ToList();
                }
            }

            return kingdoms;
        }

        internal bool isClassAvailable(int pos)
        {
            switch (pos)
            {
                case 0:
                    return (AvailableClasses & InventoryUtil.ClassFlags.Wizard) != 0;
                case 1:
                    return (AvailableClasses & InventoryUtil.ClassFlags.Assassin) != 0;
                case 2:
                    return (AvailableClasses & InventoryUtil.ClassFlags.Heavyblade) != 0;
                case 3:
                    return (AvailableClasses & InventoryUtil.ClassFlags.Dancer) != 0;
                case 4:
                    return (AvailableClasses & InventoryUtil.ClassFlags.Druid) != 0;
                case 5:
                    return (AvailableClasses & InventoryUtil.ClassFlags.Spellsword) != 0;
                case 6:
                    return (AvailableClasses & InventoryUtil.ClassFlags.Sniper) != 0;
                case 7:
                    return (AvailableClasses & InventoryUtil.ClassFlags.Bruiser) != 0;
                case 8:
                    return (AvailableClasses & InventoryUtil.ClassFlags.Defender) != 0;
                case 9:
                    return (AvailableClasses & InventoryUtil.ClassFlags.Ancient) != 0;
                case 10:
                    return (AvailableClasses & InventoryUtil.ClassFlags.Hammermaid) != 0;
                case 11:
                    return (AvailableClasses & InventoryUtil.ClassFlags.Pyromancer) != 0;
                case 12:
                    return (AvailableClasses & InventoryUtil.ClassFlags.Grenadier) != 0;
                case 13:
                    return (AvailableClasses & InventoryUtil.ClassFlags.Shadow) != 0;
            }
            return false;
        }

        internal string GetClass(int pos)
        {
            if (CLASSES.Length <= pos)
            {
                return "";
            }
            return CLASSES[pos];
        }

        private void AddItemsFromItemset(string itemset)
        {
            switch (itemset)
            {
                case "Arcane Set":
                    availableItems.AddRange(ARCANE_SET);
                    break;
                case "Night Set":
                    availableItems.AddRange(NIGHT_SET);
                    break;
                case "Timespace Set":
                    availableItems.AddRange(TIMESPACE_SET);
                    break;
                case "Wind Set":
                    availableItems.AddRange(WIND_SET);
                    break;
                case "Bloodwolf Set":
                    availableItems.AddRange(BLOODWOLF_SET);
                    break;
                case "Assassin Set":
                    availableItems.AddRange(ASSASSIN_SET);
                    break;
                case "Rockdragon Set":
                    availableItems.AddRange(ROCKDRAGON_SET);
                    break;
                case "Flame Set":
                    availableItems.AddRange(FLAME_SET);
                    break;
                case "Gem Set":
                    availableItems.AddRange(GEM_SET);
                    break;
                case "Lightning Set":
                    availableItems.AddRange(LIGHTNING_SET);
                    break;
                case "Shrine Set":
                    availableItems.AddRange(SHRINE_SET);
                    break;
                case "Lucky Set":
                    availableItems.AddRange(LUCKY_SET);
                    break;
                case "Life Set":
                    availableItems.AddRange(LIFE_SET);
                    break;
                case "Poison Set":
                    availableItems.AddRange(POISON_SET);
                    break;
                case "Depth Set":
                    availableItems.AddRange(DEPTH_SET);
                    break;
                case "Darkbite Set":
                    availableItems.AddRange(DARKBITE_SET);
                    break;
                case "Timegem Set":
                    availableItems.AddRange(TIMEGEM_SET);
                    break;
                case "Youkai Set":
                    availableItems.AddRange(YOUKAI_SET);
                    break;
                case "Haunted Set":
                    availableItems.AddRange(HAUNTED_SET);
                    break;
                case "Gladiator Set":
                    availableItems.AddRange(GLADIATOR_SET);
                    break;
                case "Sparkblade Set":
                    availableItems.AddRange(SPARKBLADE_SET);
                    break;
                case "Swiftflight Set":
                    availableItems.AddRange(SWIFTFLIGHT_SET);
                    break;
                case "Sacredflame Set":
                    availableItems.AddRange(SACREDFLAME_SET);
                    break;
                case "Ruins Set":
                    availableItems.AddRange(RUINS_SET);
                    break;
                case "Lakeshrine Set":
                    availableItems.AddRange(LAKESHRINE_SET);
                    break;
            }
        }
    }
}