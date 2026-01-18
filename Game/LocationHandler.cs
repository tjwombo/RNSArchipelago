using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces.Internal;
using RnSArchipelago.Connection;
using RnSArchipelago.Utils;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;
using System.Diagnostics.CodeAnalysis;

namespace RnSArchipelago.Game
{
    internal unsafe class LocationHandler
    {
        private readonly WeakReference<IRNSReloaded>? rnsReloadedRef;
        private readonly ILoggerV1 logger;
        internal Config.Config? modConfig;

        internal IHook<ScriptDelegate>? notchCompleteHook;
        internal IHook<ScriptDelegate>? chestOpenHook;

        internal IHook<ScriptDelegate>? setupItemsHook;
        internal IHook<ScriptDelegate>? enableModHook;
        internal IHook<ScriptDelegate>? itemAmtHook;
        internal IHook<ScriptDelegate>? itemGetHook;
        internal IHook<ScriptDelegate>? itemScoutChestHook;
        internal IHook<ScriptDelegate>? itemScoutShopHook;
        internal IHook<ScriptDelegate>? itemSetHook;
        internal IHook<ScriptDelegate>? itemSetDescriptionHook;
        //internal IHook<ScriptDelegate>? itemSetUpgradeDescriptionHook;
        internal IHook<ScriptDelegate>? takeItemHook;
        internal IHook<ScriptDelegate>? spawnTreasuresphereOnStartNHook;

        internal ArchipelagoConnection conn = null!;
        private long baseItemId = -1;

        private Random rand = new Random();

        private static readonly string GAME = "Rabbit and Steel";
        private static readonly string[] STARTING_LOCATIONS = ["Starting Class", "Starting Kingdom", "Starting Primary", "Starting Secondary", "Starting Special", "Starting Defensive"];
        private static readonly string[] CHEST_POSITIONS = ["Top Left", "Bottom Left", "Middle", "Bottom Right", "Top Right"];
        private static readonly string[] SHOP_POSITIONS = ["Full Heal Potion Slot", "Level Up Slot", "Potion 1 Slot", "Potion 2 Slot", "Potion 3 Slot",
                  "Primary Upgrade Slot", "Secondary Upgrade Slot", "Special Upgrade Slot", "Defensive Upgrade Slot"];

        private Task<Dictionary<long, ScoutedItemInfo>> chestContents = null!;
        private Task<Dictionary<long, ScoutedItemInfo>> shopContents = null!;

        private bool IsReady(
            [MaybeNullWhen(false), NotNullWhen(true)] out IRNSReloaded rnsReloaded
        )
        {
            if (this.rnsReloadedRef != null && this.rnsReloadedRef.TryGetTarget(out rnsReloaded))
            {
                return rnsReloaded != null;
            }
            this.logger.PrintMessage("Unable to find rnsReloaded in LocationHandler", System.Drawing.Color.Red);
            rnsReloaded = null;
            return false;
        }

        internal LocationHandler(WeakReference<IRNSReloaded>? rnsReloadedRef, ILoggerV1 logger, Config.Config modConfig)
        {
            this.rnsReloadedRef = rnsReloadedRef;
            this.logger = logger;
            this.modConfig = modConfig;

            InventoryUtil.Instance.AddChest += AddChestToNotch;
            InventoryUtil.Instance.SendGoal += SendGoal;
        }

        private enum LocationType
        {
            Other,
            Start,
            Battle,
            Boss,
            Chest,
            SpecialChest,
            Shop
        }

        // Get the location type of the provided notch index, default is current notch
        private LocationType GetLocationType(int index = -1)
        {
            if (this.IsReady(out var rnsReloaded))
            {
                HookUtil.FindElementInLayer("RunMenu_Blocker", "xSubimg", out var element);
                var instance = ((CLayerInstanceElement*)element)->Instance;
                if (index == -1)
                {
                    index = (int)HookUtil.GetNumeric(rnsReloaded.FindValue(instance, "currentPos"));
                }
                var currentXImg = rnsReloaded.ArrayGetEntry(rnsReloaded.FindValue(instance, "xSubimg"), index);

                if (HookUtil.IsEqualToNumeric(currentXImg, 1))
                {
                    return LocationType.Chest;
                }
                else if (HookUtil.IsEqualToNumeric(currentXImg, 2))
                {
                    return LocationType.Shop;
                }
                else if (HookUtil.IsEqualToNumeric(currentXImg, 4))
                {
                    return LocationType.Boss;
                }
                else if (HookUtil.IsEqualToNumeric(currentXImg, 5))
                {
                    return LocationType.SpecialChest;
                }
                else if (HookUtil.IsEqualToNumeric(currentXImg, 0))
                {
                    return LocationType.Battle;
                }
            }
            return LocationType.Other;
        }

        // Send the location for completing an encounter
        internal RValue* SendNotchComplete(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Original Function Send Locaiton Check", System.Drawing.Color.DarkOrange);
            }
            if (this.notchCompleteHook != null)
            {
                returnValue = this.notchCompleteHook.OriginalFunction(self, other, returnValue, argc, argv);
            } else
            {
                this.logger.PrintMessage("Unable to call notch complete hook", System.Drawing.Color.Red);
            }
            if (InventoryUtil.Instance.isActive)
            {
                if (modConfig?.ExtraDebugMessages ?? false)
                {
                    this.logger.PrintMessage("Sending Location Check", System.Drawing.Color.DarkOrange);
                }
                SendNotchLoctaion();
            }
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Return Send Location Check", System.Drawing.Color.DarkOrange);
            }
            return returnValue;
        }

        // Send the location for opening a chest
        internal RValue* SendChestOpen(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Original Function Send Chest Open Check", System.Drawing.Color.DarkOrange);
            }
            if (this.chestOpenHook != null)
            {
                returnValue = this.chestOpenHook.OriginalFunction(self, other, returnValue, argc, argv);
            } else
            {
                this.logger.PrintMessage("Unable to call chest open hook", System.Drawing.Color.Red);
            }
            if (InventoryUtil.Instance.isActive)
            {
                if (modConfig?.ExtraDebugMessages ?? false)
                {
                    this.logger.PrintMessage("Sending Chest Open Check", System.Drawing.Color.DarkOrange);
                }
                SendNotchLoctaion();
            }
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Return Send Chest Open Check", System.Drawing.Color.DarkOrange);
            }
            return returnValue;
        }

        // Update the archipelago items mod data
        internal RValue* SetupArchipelagoItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Original Function Set Up Item Mod", System.Drawing.Color.DarkOrange);
            }
            if (this.setupItemsHook != null)
            {
                returnValue = this.setupItemsHook.OriginalFunction(self, other, returnValue, argc, argv);
            } else
            {
                this.logger.PrintMessage("Unable to call setup items hook", System.Drawing.Color.Red);
            }
            if (this.IsReady(out var rnsReloaded))
            {
                if (modConfig?.ExtraDebugMessages ?? false)
                {
                    this.logger.PrintMessage("Set Up Item Mod", System.Drawing.Color.DarkOrange);
                }
                var modInfo = rnsReloaded.utils.GetGlobalVar("modInfo");
                var foundMod = false;
                var modInfoLength = rnsReloaded.ArrayGetLength(modInfo);
                if (modInfoLength.HasValue)
                {
                    for (var i = 0; i < HookUtil.GetNumeric(modInfoLength.Value); i++)
                    {
                        var entry = rnsReloaded.ArrayGetEntry(modInfo, i);
                        if (rnsReloaded.ArrayGetEntry(entry, 0)->ToString() == "ArchipelagoItems")
                        {
                            var name = new RValue();
                            rnsReloaded.CreateString(&name, "Archipelago Items");
                            *rnsReloaded.ArrayGetEntry(entry, 4) = name;

                            var tags = new RValue();
                            rnsReloaded.CreateString(&tags, "Loot Items,");
                            *rnsReloaded.ArrayGetEntry(entry, 5) = tags;

                            *rnsReloaded.ArrayGetEntry(entry, 8) = new(1); // Enabled
                            *rnsReloaded.ArrayGetEntry(entry, 10) = new(0); // 'Workshop'

                            foundMod = true;

                            break;
                        }
                    }
                }

                if (!foundMod)
                {
                    this.logger.PrintMessage("Unable to find archipelago items mod", System.Drawing.Color.Red);
                }
            }
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Return Set Up Item Mod", System.Drawing.Color.DarkOrange);
            }
            return returnValue;
        }

        // After applying mod settings in game, ensure the archipelago items mod is enabled
        internal RValue* EnableArchipelagoItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Original Function Item Mod Enable", System.Drawing.Color.DarkOrange);
            }
            if (this.enableModHook != null)
            {
                returnValue = this.enableModHook.OriginalFunction(self, other, returnValue, argc, argv);
            } else
            {
                this.logger.PrintMessage("Unable to call enable mod hook", System.Drawing.Color.Red);
            }

            if (this.IsReady(out var rnsReloaded))
            {
                if (modConfig?.ExtraDebugMessages ?? false)
                {
                    this.logger.PrintMessage("Ensure Item Mod Is Enabled", System.Drawing.Color.DarkOrange);
                }
                var modInfo = rnsReloaded.utils.GetGlobalVar("modInfo");
                var modInfoLength = rnsReloaded.ArrayGetLength(modInfo);
                if (modInfoLength.HasValue)
                {
                    for (var i = 0; i < HookUtil.GetNumeric(modInfoLength.Value); i++)
                    {
                        var entry = rnsReloaded.ArrayGetEntry(modInfo, i);
                        if (rnsReloaded.ArrayGetEntry(entry, 0)->ToString() == "ArchipelagoItems")
                        {
                            *rnsReloaded.ArrayGetEntry(entry, 8) = new(1); // Enabled
                            if (modConfig?.ExtraDebugMessages ?? false)
                            {
                                this.logger.PrintMessage("Before Return Item Mod Enable", System.Drawing.Color.DarkOrange);
                            }
                            return returnValue;
                        }
                    }
                }
            }
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Return Item Mod Enable", System.Drawing.Color.DarkOrange);
            }
            return returnValue;
        }

        // Scout the network items in the chest ahead of time so once we need the results the task has finished
        internal RValue* ScoutChestItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (InventoryUtil.Instance.isActive)
            {
                if (modConfig?.ExtraDebugMessages ?? false)
                {
                    this.logger.PrintMessage("Scouting Chest Items", System.Drawing.Color.DarkOrange);
                }
                GetArchipelagoChestItemInfo();
            }
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Original Function Scout Chest", System.Drawing.Color.DarkOrange);
            }
            if (this.itemScoutChestHook != null)
            {
                returnValue = this.itemScoutChestHook.OriginalFunction(self, other, returnValue, argc, argv);
            } else
            {
                this.logger.PrintMessage("Unable to call item scout chest hook", System.Drawing.Color.Red);
            }
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Return Scout Chest", System.Drawing.Color.DarkOrange);
            }
            return returnValue;
        }

        // Scout the network items in the shop ahead of time so once we need the results the task has finished
        internal RValue* ScoutShopItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.IsReady(out var rnsReloaded))
            {
                if (InventoryUtil.Instance.isActive)
                {
                    if (modConfig?.ExtraDebugMessages ?? false)
                    {
                        this.logger.PrintMessage("Scouting Chest Items", System.Drawing.Color.DarkOrange);
                    }
                    GetArchipelagoShopItemInfo();

                    if (modConfig?.ExtraDebugMessages ?? false)
                    {
                        this.logger.PrintMessage("Before Original Function Scout Chest Item", System.Drawing.Color.DarkOrange);
                    }
                    if (this.itemScoutShopHook != null)
                    {
                        returnValue = this.itemScoutShopHook.OriginalFunction(self, other, returnValue, argc, argv);
                    } else
                    {
                        this.logger.PrintMessage("Unable to call item scout shop hook", System.Drawing.Color.Red);
                    }

                    var instance = new RValue(self);
                    long? id = -1;
                    for (var j = 0; j < 9; j++)
                    {
                        id = conn.session?.Locations.GetLocationIdFromName(GAME, SHOP_POSITIONS[j]);

                        // TODO: RE-TURN THIS ON WHEN THE AP ITEM HAS BEEN BOUGHT
                        // if the item is an archipelago item, disable the purchase condition, mainly applies to hp and upgrades
                        if (id.HasValue && conn.session != null && !conn.session.Locations.AllLocationsChecked.Contains(id.Value))
                        {
                            *rnsReloaded.ArrayGetEntry(instance["storeSlotHeal"], j) = new RValue(0);
                            *rnsReloaded.ArrayGetEntry(instance["storeSlotUpgrade"], j) = new RValue(0);
                        }
                    }
                    if (modConfig?.ExtraDebugMessages ?? false)
                    {
                        this.logger.PrintMessage("Before Return Scout Shop", System.Drawing.Color.DarkOrange);
                    }
                    return returnValue;
                }
            }
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Original Function Scout Shop", System.Drawing.Color.DarkOrange);
            }
            if (this.itemScoutShopHook != null)
            {
                returnValue = this.itemScoutShopHook.OriginalFunction(self, other, returnValue, argc, argv);
            } else
            {
                this.logger.PrintMessage("Unable to call item scout shop hook", System.Drawing.Color.Red); ;
            }
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Return Scout Shop", System.Drawing.Color.DarkOrange);
            }
            return returnValue;
        }

        //TODO: Actually determine what amount it should be
        // Set the amount of items in the chest to corrospond to the amount it should be
        internal RValue* SetAmountOfItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Original Function Set Chest Amount", System.Drawing.Color.DarkOrange);
            }
            if (this.itemAmtHook != null)
            {
                returnValue = this.itemAmtHook.OriginalFunction(self, other, returnValue, argc, argv);
            } else
            {
                this.logger.PrintMessage("Unable to call item amount hook", System.Drawing.Color.Red);
            }
            if (InventoryUtil.Instance.isActive)
            {
                if (modConfig?.ExtraDebugMessages ?? false)
                {
                    this.logger.PrintMessage("Setting Chest Amount", System.Drawing.Color.DarkOrange);
                }
                returnValue->Real = 5;
            }
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Return Set Chest Amount", System.Drawing.Color.DarkOrange);
            }
            return returnValue;
        }

        internal void GetUnclaimedShopItems(int position, out ScoutedItemInfo? info, out long archipelagoItem, out bool useArchipelagoItem)
        {
            if (conn.session != null)
            {
                if (InventoryUtil.Instance.ShopSanity == InventoryUtil.ShopSetting.Global)
                {
                    long id = conn.session.Locations.GetLocationIdFromName(GAME, SHOP_POSITIONS[position]);
                    if (!conn.session.Locations.AllLocationsChecked.Contains(id))
                    {
                        info = shopContents.Result[id];
                        if (info.Flags.HasFlag(ItemFlags.Advancement))
                        {
                            archipelagoItem = baseItemId + 1;
                            useArchipelagoItem = true;
                        }
                        else
                        {
                            archipelagoItem = baseItemId;
                            useArchipelagoItem = true;
                        }
                        return;
                    }
                }
                else if (InventoryUtil.Instance.ShopSanity == InventoryUtil.ShopSetting.Regional)
                {
                    long id = conn.session.Locations.GetLocationIdFromName(GAME, GetBaseLocation() + " " + SHOP_POSITIONS[position]);
                    if (!conn.session.Locations.AllLocationsChecked.Contains(id))
                    {
                        info = shopContents.Result[id];

                        if (info.Flags.HasFlag(ItemFlags.Advancement))
                        {
                            archipelagoItem = baseItemId + 1;
                            useArchipelagoItem = true;
                        }
                        else
                        {
                            archipelagoItem = baseItemId;
                            useArchipelagoItem = true;
                        }
                        return;
                    }
                }
            }

            info = null;
            archipelagoItem = baseItemId;
            useArchipelagoItem = false;
        }

        // Set the item inside the chest to the proper item
        internal RValue* SetItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.IsReady(out var rnsReloaded))
            {
                if (InventoryUtil.Instance.isActive)
                {
                    if (modConfig?.ExtraDebugMessages ?? false)
                    {
                        this.logger.PrintMessage("Set Items", System.Drawing.Color.DarkOrange);
                    }
                    var location = GetLocationType();
                    // If the item that is being created is a chest loot item
                    if (location == LocationType.Chest)
                    {
                        // Set the item to the archipelago item
                        if (InventoryUtil.Instance.checksPerItemInChest)
                        {
                            for (var i = 0; i < 5; i++)
                            {
                                // Determine which slot item we are at, which should be the first -1
                                if (HookUtil.IsEqualToNumeric(rnsReloaded.ArrayGetEntry(rnsReloaded.ArrayGetEntry(rnsReloaded.ArrayGetEntry(rnsReloaded.FindValue(self, "slots"), 1), i), 1), -1))
                                {
                                    var info = chestContents.Result[GetChestPositionLocationId(SlotIdToChestPos(i))];

                                    // If the location is checked
                                    if (conn.session != null && conn.session.Locations.AllLocationsChecked.Contains(GetChestPositionLocationId(SlotIdToChestPos(i))))
                                    {
                                        *argv[0] = new RValue(baseItemId + 2);
                                    }
                                    // If the item is progression
                                    else if (info.Flags.HasFlag(ItemFlags.Advancement))
                                    {
                                        *argv[0] = new RValue(baseItemId + 1);
                                    }
                                    else
                                    {
                                        *argv[0] = new RValue(baseItemId);
                                    }
                                    break;
                                }
                            }
                        }
                        else if (InventoryUtil.Instance.shuffleItemsets)
                        {
                            if (InventoryUtil.Instance.AvailableItems.Count == 0)
                            {
                                *argv[0] = new RValue(0);
                            }
                            else
                            {
                                var index = rand.Next(InventoryUtil.Instance.AvailableItems.Count);
                                *argv[0] = new RValue(InventoryUtil.Instance.AvailableItems[index]);
                            }
                        }
                    }
                    // If the item that is being created is a shop loot item
                    else if (location == LocationType.Shop)
                    {
                        // Determine which slot item we are at, which should be the first -1
                        for (var i = 0; i < 9; i++)
                        {
                            if (HookUtil.IsEqualToNumeric(rnsReloaded.ArrayGetEntry(rnsReloaded.ArrayGetEntry(rnsReloaded.ArrayGetEntry(rnsReloaded.FindValue(self, "slots"), 2), i), 1), -1))
                            {
                                GetUnclaimedShopItems(i, out ScoutedItemInfo? info, out long archipelagoItem, out bool useArchipelagoItem);

                                switch (i)
                                {
                                    case 0:
                                        ShopItemsUtil.SetHpPotion(argv, archipelagoItem, useArchipelagoItem);
                                        if (this.itemSetHook != null)
                                        {
                                            returnValue = this.itemSetHook.OriginalFunction(self, other, returnValue, argc, argv);
                                        } else
                                        {
                                            this.logger.PrintMessage("Unable to call item set hook", System.Drawing.Color.Red);
                                        }
                                        break;
                                    case 1:
                                        ShopItemsUtil.SetLevelPotion(argv, archipelagoItem, useArchipelagoItem);
                                        if (this.itemSetHook != null)
                                        {
                                            returnValue = this.itemSetHook.OriginalFunction(self, other, returnValue, argc, argv);
                                        }
                                        else
                                        {
                                            this.logger.PrintMessage("Unable to call item set hook", System.Drawing.Color.Red);
                                        }
                                        break;
                                    case 2:
                                    case 3:
                                    case 4:
                                        ShopItemsUtil.SetPotion(argv, archipelagoItem, useArchipelagoItem);
                                        if (this.itemSetHook != null)
                                        {
                                            returnValue = this.itemSetHook.OriginalFunction(self, other, returnValue, argc, argv);
                                        }
                                        else
                                        {
                                            this.logger.PrintMessage("Unable to call item set hook", System.Drawing.Color.Red);
                                        }
                                        break;
                                    case 5:
                                        ShopItemsUtil.SetPrimaryUpgrade(argv, archipelagoItem, useArchipelagoItem);
                                        if (this.itemSetHook != null)
                                        {
                                            returnValue = this.itemSetHook.OriginalFunction(self, other, returnValue, argc, argv);
                                        }
                                        else
                                        {
                                            this.logger.PrintMessage("Unable to call item set hook", System.Drawing.Color.Red);
                                        }
                                        break;
                                    case 6:
                                        ShopItemsUtil.SetSecondaryUpgrade(argv, archipelagoItem, useArchipelagoItem);
                                        if (this.itemSetHook != null)
                                        {
                                            returnValue = this.itemSetHook.OriginalFunction(self, other, returnValue, argc, argv);
                                        }
                                        else
                                        {
                                            this.logger.PrintMessage("Unable to call item set hook", System.Drawing.Color.Red);
                                        }
                                        break;
                                    case 7:
                                        ShopItemsUtil.SetSpecialUpgrade(argv, archipelagoItem, useArchipelagoItem);
                                        if (this.itemSetHook != null)
                                        {
                                            returnValue = this.itemSetHook.OriginalFunction(self, other, returnValue, argc, argv);
                                        }
                                        else
                                        {
                                            this.logger.PrintMessage("Unable to call item set hook", System.Drawing.Color.Red);
                                        }
                                        break;
                                    case 8:
                                        ShopItemsUtil.SetDefensiveUpgrade(argv, archipelagoItem, useArchipelagoItem);
                                        if (this.itemSetHook != null)
                                        {
                                            returnValue = this.itemSetHook.OriginalFunction(self, other, returnValue, argc, argv);
                                        }
                                        else
                                        {
                                            this.logger.PrintMessage("Unable to call item set hook", System.Drawing.Color.Red);
                                        }
                                        break;
                                }

                                return returnValue;
                            }
                        }
                    }
                    else if (location == LocationType.SpecialChest)
                    {
                        if (InventoryUtil.Instance.shuffleItemsets)
                        {
                            if (InventoryUtil.Instance.AvailableItems.Count == 0)
                            {
                                *argv[0] = new RValue(0);
                            }
                            else
                            {
                                var index = rand.Next(InventoryUtil.Instance.AvailableItems.Count);
                                *argv[0] = new RValue(InventoryUtil.Instance.AvailableItems[index]);
                            }

                            // TODO: Trying to force the icon to show when its a chest after the intro room, but its not working
                            //HookUtil.FindElementInLayer(rnsReloaded, "RunMenu_Blocker", "xSubimg", out var element);
                            //var instance = ((CLayerInstanceElement*)element)->Instance;
                            /*rnsReloaded.FindValue(instance, "yScale")->Real = 1;
                            rnsReloaded.FindValue(instance, "yScale")->Real = 1;*/

                            this.logger.PrintMessage(HookUtil.PrintHook("mod", self, returnValue, argc, argv), System.Drawing.Color.DarkOrange);
                        }
                    }
                }
            }

            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Original Function Set Item", System.Drawing.Color.DarkOrange);
            }
            if (this.itemSetHook != null)
            {
                returnValue = this.itemSetHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call item set hook", System.Drawing.Color.Red);
            }
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Return Set Item", System.Drawing.Color.DarkOrange);
            }
            return returnValue;
        }

        // Get the ingame item id for the first archipelago item
        internal RValue* GetItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.IsReady(out var rnsReloaded))
            {
                if (modConfig?.ExtraDebugMessages ?? false)
                {
                    this.logger.PrintMessage("Get AP Item Mod", System.Drawing.Color.DarkOrange);
                }
                if (rnsReloaded.GetString(argv[0]).Contains("ArchipelagoItems"))
                {
                    baseItemId = HookUtil.GetNumeric(rnsReloaded.FindValue(self, "item_data_entry_max")) + 1;
                }
            }

            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Original Function Get AP Item Mod", System.Drawing.Color.DarkOrange);
            }
            if (this.itemGetHook != null)
            {
                returnValue = this.itemGetHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call item get hook", System.Drawing.Color.Red);
            }
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Return Get AP Item Mod", System.Drawing.Color.DarkOrange);
            }
            return returnValue;
        }

        // Set the description for archipelago items to reflect their actual item
        internal RValue* SetItemsDescription(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Original Function Item Descriptions", System.Drawing.Color.DarkOrange);
            }
            if (this.itemSetDescriptionHook != null)
            {
                returnValue = this.itemSetDescriptionHook.OriginalFunction(self, other, returnValue, argc, argv);
            } else
            {
                this.logger.PrintMessage("Unable to call item set description hook", System.Drawing.Color.Red);
            }

            if (InventoryUtil.Instance.isActive)
            {
                if (modConfig?.ExtraDebugMessages ?? false)
                {
                    this.logger.PrintMessage("Set Item Descriptions", System.Drawing.Color.DarkOrange);
                }
                //if an archipelago item, set the description to the real item
                if (HookUtil.IsEqualToNumeric(argv[0], baseItemId) || HookUtil.IsEqualToNumeric(argv[0], baseItemId + 1) || HookUtil.IsEqualToNumeric(argv[0], baseItemId + 2))
                {
                    ScoutedItemInfo? info = null;
                    //TODO: Look into making the better
                    var safeSelf = new RValue(self);

                    if (InventoryUtil.Instance.checksPerItemInChest && GetLocationType() == LocationType.Chest)
                    {
                        info = chestContents.Result[GetChestPositionLocationId(SlotIdToChestPos((int)HookUtil.GetNumeric(safeSelf["slotId"])))];
                    }
                    else if (InventoryUtil.Instance.ShopSanity != InventoryUtil.ShopSetting.None && GetLocationType() == LocationType.Shop)
                    {
                        if (conn.session != null)
                        {
                            if (InventoryUtil.Instance.ShopSanity == InventoryUtil.ShopSetting.Global)
                            {
                                info = shopContents.Result[conn.session.Locations.GetLocationIdFromName(GAME, SHOP_POSITIONS[(int)HookUtil.GetNumeric(safeSelf["slotId"])])];
                            }
                            else if (InventoryUtil.Instance.ShopSanity == InventoryUtil.ShopSetting.Regional)
                            {
                                info = shopContents.Result[conn.session.Locations.GetLocationIdFromName(GAME, GetBaseLocation() + " " + SHOP_POSITIONS[(int)HookUtil.GetNumeric(safeSelf["slotId"])])];
                            }
                        }
                    }

                    if (this.IsReady(out var rnsReloaded))
                    {
                        if (info != null)
                        {
                            var player = info.Player.Slot == MessageHandler.Instance.slot ? "your" : info.Player.Name + "'s";

                            rnsReloaded.CreateString(returnValue, info.ItemDisplayName + " for " + player + " world");
                        }
                        else
                        {
                            rnsReloaded.CreateString(returnValue, "Unable to fetch archipelago item data");
                        }
                    }
                }
            }

            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Return Item Descriptions", System.Drawing.Color.DarkOrange);
            }

            return returnValue;
        }

        // Set the description for archipelago items to reflect their actual item
        internal RValue* SetUpgradeDescription(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            /*if (this.itemSetUpgradeDescriptionHook != null)
            {
                returnValue = this.itemSetUpgradeDescriptionHook.OriginalFunction(self, other, returnValue, argc, argv);
            } else
            {
                this.logger.PrintMessage("Unable to call item set upgrade description hook", System.Drawing.Color.Red);
            }*/

            // TODO: probably should make a function that tells me what notch room type we are in
            var notchType = GetLocationType();

            if (notchType == LocationType.Shop)
            {
                //this.logger.PrintMessage(HookUtil.PrintHook(rnsReloaded, "shop", self, returnValue, argc, argv), System.Drawing.Color.DarkOrange);

                /*HookUtil.FindElementInLayer(rnsReloaded, "InventoryInfo", "itemId", out var layer);
                if (layer != null)
                {
                    var element = layer->Layer->Elements.First;

                    while (element != null)
                    {
                        var instance = (CInstance*)element;
                        var shopSlot = HookUtil.GetNumeric(rnsReloaded.FindValue(instance, "slotId"));
                        var a = new RValue(instance);
                        this.logger.PrintMessage(shopSlot + "", System.Drawing.Color.DarkOrange);
                        //this.logger.PrintMessage(shopSlot + " " + HookUtil.GetNumeric(rnsReloaded.FindValue(instance, "slotId")) + " " + Marshal.PtrToStringAnsi((nint)element->Layer->Name), System.Drawing.Color.DarkOrange);
                        if (shopSlot >= 5 && shopSlot <= 8)
                        {
                            rnsReloaded.FindValue(instance, "itemId")->Real = 94;

                        }
                        // if 5 <= slotId <= 8

                        // itemId = 94


                        element = element->Next;
                        return returnValue;
                    }
                }*/
                //var a = new RValue(self);
                //this.logger.PrintMessage(a.ToString(), System.Drawing.Color.DarkOrange);
                /*this.logger.PrintMessage(HookUtil.GetNumeric(rnsReloaded.FindValue(self, "slotId")) + "", System.Drawing.Color.DarkOrange);
                var shopSlot = (int)HookUtil.GetNumeric(rnsReloaded.FindValue(self, "slotId"));
                if (shopSlot >= 5 && shopSlot <= 8)
                {
                    rnsReloaded.FindValue(self, "itemId")->Real = 94;

                }*/
            }
            //this.logger.PrintMessage(HookUtil.PrintHook(rnsReloaded, "uh", self, returnValue, argc, argv), System.Drawing.Color.DarkOrange);
            var a = new RValue(self);
            this.logger.PrintMessage(a.ToString(), System.Drawing.Color.DarkOrange);
            /*if (this.itemSetUpgradeDescriptionHook != null)
            {
                returnValue = this.itemSetUpgradeDescriptionHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call item set upgrade description hook", System.Drawing.Color.Red);
            }*/
            return returnValue;
        }

        // Convert the ingame slotId to the archipelago chest location name suffix
        private string SlotIdToChestPos(int slotId)
        {
            switch (slotId)
            {
                case 0: return " Top Left";
                case 1: return " Bottom Left";
                case 2: return " Middle";
                case 3: return " Bottom Right";
                case 4: return " Top Right";
            }
            return "";
        }

        // Get the archipelago location id for the current chest's item at chestPos
        private long GetChestPositionLocationId(string chestPos)
        {
            if (conn.session != null)
            {
                return conn.session.Locations.GetLocationIdFromName(GAME, GetBaseLocation() + chestPos);
            }
            return -1;
        }

        // Scout all the items in the current chest
        private void GetArchipelagoChestItemInfo()
        {
            if (conn.session != null)
            {
                var locations = CHEST_POSITIONS.Select(x => conn.session.Locations.GetLocationIdFromName(GAME, GetBaseLocation() + " " + x)).ToArray();

                chestContents = conn.session.Locations.ScoutLocationsAsync(HintCreationPolicy.CreateAndAnnounceOnce, locations);
            }
        }

        // Scout all the items in the current shop
        private void GetArchipelagoShopItemInfo()
        {
            long[] locations = [];

            if (conn.session != null)
            {
                if (InventoryUtil.Instance.ShopSanity == InventoryUtil.ShopSetting.Global)
                {
                    locations = SHOP_POSITIONS.Select(x => conn.session.Locations.GetLocationIdFromName(GAME, x)).ToArray();
                }
                else if (InventoryUtil.Instance.ShopSanity == InventoryUtil.ShopSetting.Regional)
                {
                    locations = SHOP_POSITIONS.Select(x => conn.session.Locations.GetLocationIdFromName(GAME, GetBaseLocation() + " " + x)).ToArray();
                }

                shopContents = conn.session.Locations.ScoutLocationsAsync(HintCreationPolicy.CreateAndAnnounceOnce, locations);
                this.logger.PrintMessage("shop pos id: " + string.Join(", ", shopContents.Result.Select(pair => $"{pair.Key} => {pair.Value.ItemName}\n")), System.Drawing.Color.DarkOrange);
            }
        }

        // Prevent 'fake' items from actually being taken
        internal RValue* TakeItem(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.IsReady(out var rnsReloaded))
            {
                if (InventoryUtil.Instance.isActive)
                {
                    if (modConfig?.ExtraDebugMessages ?? false)
                    {
                        this.logger.PrintMessage("Prepare Take Item", System.Drawing.Color.DarkOrange);
                    }
                    var itemPos = HookUtil.GetNumeric(argv[2]);
                    CLayerElementBase* instance = null;
                    if (GetLocationType() == LocationType.Chest)
                    {
                        HookUtil.FindElementInLayer("LootInfo", "slotId", itemPos + "", out instance);
                    }
                    else if (GetLocationType() == LocationType.Shop)
                    {
                        HookUtil.FindElementInLayer("InventoryInfo", "slotId", itemPos + "", out instance);
                    }
                    if (instance != null)
                    {
                        var element = ((CLayerInstanceElement*)instance)->Instance;
                        var itemId = rnsReloaded.FindValue(element, "itemId");

                        if (!HookUtil.IsEqualToNumeric(itemId, baseItemId) && !HookUtil.IsEqualToNumeric(itemId, baseItemId + 1) && !HookUtil.IsEqualToNumeric(itemId, baseItemId + 2))
                        {
                            if (modConfig?.ExtraDebugMessages ?? false)
                            {
                                this.logger.PrintMessage("Before Original Function Take Item 1", System.Drawing.Color.DarkOrange);
                            }
                            if (this.takeItemHook != null)
                            {
                                returnValue = this.takeItemHook.OriginalFunction(self, other, returnValue, argc, argv);
                            } else
                            {
                                this.logger.PrintMessage("Unable to call take item hook", System.Drawing.Color.Red);
                            }
                            if (modConfig?.ExtraDebugMessages ?? false)
                            {
                                this.logger.PrintMessage("Return From Take Item 1", System.Drawing.Color.DarkOrange);
                            }
                            return returnValue;
                        }

                        this.logger.PrintMessage(HookUtil.PrintHook("take", self, returnValue, argc, argv), System.Drawing.Color.DarkOrange);

                        if (InventoryUtil.Instance.checksPerItemInChest && GetLocationType() == LocationType.Chest)
                        {
                            var locationPacket = new LocationChecksPacket { Locations = [GetChestPositionLocationId(SlotIdToChestPos((int)HookUtil.GetNumeric(rnsReloaded.FindValue(element, "slotId"))))] };
                            conn.session?.Socket.SendPacketAsync(locationPacket);
                        }
                        else if (InventoryUtil.Instance.ShopSanity != InventoryUtil.ShopSetting.None && GetLocationType() == LocationType.Shop)
                        {
                            if (conn.session != null)
                            {
                                if (InventoryUtil.Instance.ShopSanity == InventoryUtil.ShopSetting.Global)
                                {
                                    var locationPacket = new LocationChecksPacket { Locations = [conn.session.Locations.GetLocationIdFromName(GAME, SHOP_POSITIONS[(int)HookUtil.GetNumeric(argv[2])])] };
                                    conn.session?.Socket.SendPacketAsync(locationPacket);
                                }
                                else if (InventoryUtil.Instance.ShopSanity == InventoryUtil.ShopSetting.Regional)
                                {
                                    var locationPacket = new LocationChecksPacket { Locations = [conn.session.Locations.GetLocationIdFromName(GAME, GetBaseLocation() + " " + SHOP_POSITIONS[(int)HookUtil.GetNumeric(argv[2])])] };
                                    conn.session?.Socket.SendPacketAsync(locationPacket);
                                }
                            }

                            var slots = new RValue(self);

                            // Set the item cache to -1, so we repopulate it
                            *rnsReloaded.ArrayGetEntry(rnsReloaded.ArrayGetEntry(rnsReloaded.ArrayGetEntry(slots["slots"], 2), (int)HookUtil.GetNumeric(argv[2])), 1) = new RValue(-1);

                            rnsReloaded.ExecuteScript("scr_itemsys_populate_store", self, other, [new RValue(0)]);

                            // TODO: Fix the width of item names
                            // TODO: Subtract user gold, and set price of AP items
                        }

                    }
                    else
                    {
                        if (modConfig?.ExtraDebugMessages ?? false)
                        {
                            this.logger.PrintMessage("Before Original Function Take Item 2", System.Drawing.Color.DarkOrange);
                        }
                        if (this.takeItemHook != null)
                        {
                            returnValue = this.takeItemHook.OriginalFunction(self, other, returnValue, argc, argv);
                        }
                        else
                        {
                            this.logger.PrintMessage("Unable to call take item hook", System.Drawing.Color.Red);
                        }
                    }
                }
                else
                {
                    if (modConfig?.ExtraDebugMessages ?? false)
                    {
                        this.logger.PrintMessage("Before Original Function Take Item 3", System.Drawing.Color.DarkOrange);
                    }
                    if (this.takeItemHook != null)
                    {
                        returnValue = this.takeItemHook.OriginalFunction(self, other, returnValue, argc, argv);
                    }
                    else
                    {
                        this.logger.PrintMessage("Unable to call take item hook", System.Drawing.Color.Red);
                    }
                }
            } else
            {
                if (modConfig?.ExtraDebugMessages ?? false)
                {
                    this.logger.PrintMessage("Before Original Function Take Item 4", System.Drawing.Color.DarkOrange);
                }
                if (this.takeItemHook != null)
                {
                    returnValue = this.takeItemHook.OriginalFunction(self, other, returnValue, argc, argv);
                }
                else
                {
                    this.logger.PrintMessage("Unable to call take item hook", System.Drawing.Color.Red);
                }
            }

            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Return From Take Item 2", System.Drawing.Color.DarkOrange);
            }

            return returnValue;
        }

        // Send the location for finishing a notch if there is a generic location for it, i.e. battle, chest, or boss (not shop)
        internal void SendNotchLoctaion()
        {
            if (this.IsReady(out var rnsReloaded))
            {
                var baseLocation = GetBaseLocation();

                HookUtil.FindElementInLayer("Ally", "allyId", out var instance);
                var element = ((CLayerInstanceElement*)instance)->Instance;
                var characterId = (int)HookUtil.GetNumeric(rnsReloaded.FindValue(element, "allyId"));
                var character = InventoryUtil.Instance.GetClass(characterId);

                if (conn.session != null)
                {
                    long[] locations = [conn.session.Locations.GetLocationIdFromName(GAME, baseLocation), conn.session.Locations.GetLocationIdFromName(GAME, baseLocation + " - " + character)];
                    var locationPacket = new LocationChecksPacket { Locations = locations };
                    //conn.session.Socket.SendPacket(locationPacket);
                    conn.session.Locations.CompleteLocationChecksAsync(locations);
                }
            }
        }

        // Get the base location name, ex. Kingdom Outskirts Chest 1
        private string GetBaseLocation()
        {
            if (this.IsReady(out var rnsReloaded))
            {
                HookUtil.FindElementInLayer("RunMenu_Blocker", "currentPos", out var instance);
                var element = ((CLayerInstanceElement*)instance)->Instance;

                var kingdomName = rnsReloaded.FindValue(element, "stageName")->ToString();
                kingdomName = kingdomName.Replace(Environment.NewLine, " ");

                var notchName = GetNotchName(element);
                if (notchName.Contains("Chest") && !kingdomName.Equals("Kingdom Outskirts"))
                {
                    notchName = " Chest";
                }
                else if (kingdomName.Equals("Moonlit Pinnacle"))
                {
                    return "Shira";
                }
                return kingdomName + notchName;
            }
            return "";
        }

        // Get the name of the location for the notch based on its image and number of occurence
        private string GetNotchName(CInstance* element)
        {
            if (this.IsReady(out var rnsReloaded))
            {
                var notchPos = (int)HookUtil.GetNumeric(rnsReloaded.FindValue(element, "currentPos"));
                var notchType = GetLocationType();

                if (notchType == LocationType.Boss)
                {
                    return " Boss";
                }

                int count = 1;

                var kingdomName = rnsReloaded.FindValue(element, "stageName")->ToString();
                kingdomName = kingdomName.Replace(Environment.NewLine, " ");


                for (var i = kingdomName.Equals("Kingdom Outskirts") ? 1 : 0; i < notchPos; i++)
                {
                    if (GetLocationType(i) == notchType)
                    {
                        count++;
                    }
                }

                if (notchType == LocationType.Chest)
                {
                    return " Chest " + count;
                }

                if (notchType == LocationType.Battle)
                {
                    return " Battle " + count;
                }

                if (notchType == LocationType.Shop)
                {
                    return " Shop";
                }
            }

            return "";
        }

        // Make the next notch be an ingame only chest
        private void AddChestToNotch()
        {
            if (this.IsReady(out var rnsReloaded))
            {
                HookUtil.FindElementInLayer("RunMenu_Blocker", "xSubimg", out var element);
                var instance = ((CLayerInstanceElement*)element)->Instance;

                var currentPos = HookUtil.GetNumeric(rnsReloaded.FindValue(instance, "currentPos"));
                if (currentPos == -1)
                {
                    currentPos = 0;
                }
                var notches = rnsReloaded.FindValue(instance, "notches");
                var notch = HookUtil.CreateRArray([5, "", 0, 0]);

                // Actually increase things
                rnsReloaded.ExecuteCodeFunction("array_insert", instance, null, [*notches, new RValue(currentPos + 1), notch]);
                rnsReloaded.FindValue(instance, "notchNumber")->Real = HookUtil.GetNumeric(rnsReloaded.FindValue(instance, "notchNumber")) + 1;
                rnsReloaded.ExecuteCodeFunction("array_insert", instance, null, [*rnsReloaded.FindValue(instance, "xSubimg"), new RValue(currentPos + 1), new(5)]);
                
            }
        }

        // On outskirts loading, besides loading into lobby, add the treasurespheres we have accumulated
        internal RValue* SpawnTreasuresphereOnStart(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Original Function Try Spawn Start Treasuresphere", System.Drawing.Color.DarkOrange);
            }
            if (this.spawnTreasuresphereOnStartNHook != null)
            {
                returnValue = this.spawnTreasuresphereOnStartNHook.OriginalFunction(self, other, returnValue, argc, argv);
            } else
            {
                this.logger.PrintMessage("Unable to call spawn trasuresphere on start n hook", System.Drawing.Color.Red);
            }


            if (this.IsReady(out var rnsReloaded))
            {
                if (modConfig?.ExtraDebugMessages ?? false)
                {
                    this.logger.PrintMessage("Try Spawn Start Treasuresphere", System.Drawing.Color.DarkOrange);
                }
                var kingdomName = rnsReloaded.FindValue(self, "stageName")->ToString();
                kingdomName = kingdomName.Replace(Environment.NewLine, " ");

                if (kingdomName.Equals("Kingdom Outskirts"))
                {
                    for (int i = 0; i < InventoryUtil.Instance.AvailableTreasurespheres; i++)
                    {
                        AddChestToNotch();
                    }
                }
            }

            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Return From Spawn Start Treasuresphere", System.Drawing.Color.DarkOrange);
            }

            return returnValue;
        }

        private void SendGoal()
        {
            conn.session?.SetGoalAchieved();
        }
    }
}
