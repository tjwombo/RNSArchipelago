using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;

using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

using RnSArchipelago.Connection;
using RnSArchipelago.Utils;

using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;

namespace RnSArchipelago.Game
{
    internal unsafe class LocationHandler
    {
        private readonly WeakReference<IRNSReloaded> rnsReloadedRef;
        private readonly Random rand;
        private readonly ILogger logger;
        private readonly InventoryHandler inventoryHandler;
        private readonly ShopItemsHandler shopItemsHandler;
        private readonly ScoutHandler scoutHandler;
        private readonly Config.Config modConfig;
        private readonly ArchipelagoConnection conn;

        internal IHook<ScriptDelegate>? notchCompleteHook;
        internal IHook<ScriptDelegate>? chestOpenHook;

        internal IHook<ScriptDelegate>? setupItemsHook;
        internal IHook<ScriptDelegate>? enableModHook;
        internal IHook<ScriptDelegate>? itemAmtHook;
        internal IHook<ScriptDelegate>? itemGetHook;
        internal IHook<ScriptDelegate>? itemSetHook;
        internal IHook<ScriptDelegate>? itemSetDescriptionHook;
        //internal IHook<ScriptDelegate>? itemSetUpgradeDescriptionHook;
        internal IHook<ScriptDelegate>? takeItemHook;
        internal IHook<ScriptDelegate>? spawnTreasuresphereHook;
        internal IHook<ScriptDelegate>? spawnTreasuresphereOnStartNHook;
        internal IHook<ScriptDelegate>? readyCheckHook;

        private long baseItemId = -1;
        private int treasurespheresToSpawn = 0;

        internal static readonly string[] CHEST_POSITIONS = ["Top Left", "Bottom Left", "Middle", "Bottom Right", "Top Right"];
        internal static readonly string[] SHOP_POSITIONS = ["Full Heal Potion Slot", "Level Up Slot", "Potion 1 Slot", "Potion 2 Slot", "Potion 3 Slot",
                  "Primary Upgrade Slot", "Secondary Upgrade Slot", "Special Upgrade Slot", "Defensive Upgrade Slot"];

        internal LocationHandler(WeakReference<IRNSReloaded> rnsReloadedRef, Random rand, ILogger logger, InventoryHandler inventoryHandler, ShopItemsHandler shopItemsHandler, ScoutHandler scoutHandler, Config.Config modConfig, ArchipelagoConnection conn)
        {
            this.rnsReloadedRef = rnsReloadedRef;
            this.rand = rand;
            this.logger = logger;
            this.inventoryHandler = inventoryHandler;
            this.shopItemsHandler = shopItemsHandler;
            this.scoutHandler = scoutHandler;
            this.modConfig = modConfig;
            this.conn = conn;

            this.inventoryHandler.AddChest += AddChestToNotch;
            this.inventoryHandler.SendGoal += SendGoal;
        }

        // Send the location for completing an encounter
        internal RValue* SendNotchComplete(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.notchCompleteHook != null)
            {
                returnValue = this.notchCompleteHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call notch complete hook", System.Drawing.Color.Red);
            }

            if (this.inventoryHandler.isActive)
            {
                SendNotchLoctaion();
            }
            return returnValue;
        }

        // Send the location for opening a chest
        internal RValue* SendChestOpen(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.chestOpenHook != null)
            {
                returnValue = this.chestOpenHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call chest open hook", System.Drawing.Color.Red);
            }

            if (this.inventoryHandler.isActive)
            {
                SendNotchLoctaion();
            }

            return returnValue;
        }

        // Update the archipelago items mod data
        internal RValue* SetupArchipelagoItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.setupItemsHook != null)
            {
                returnValue = this.setupItemsHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call setup items hook", System.Drawing.Color.Red);
            }

            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
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

            return returnValue;
        }

        // After applying mod settings in game, ensure the archipelago items mod is enabled
        internal RValue* EnableArchipelagoItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.enableModHook != null)
            {
                returnValue = this.enableModHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call enable mod hook", System.Drawing.Color.Red);
            }

            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
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

                            return returnValue;
                        }
                    }
                }
            }

            return returnValue;
        }

        // Set the amount of items in the chest to be 5
        internal RValue* SetAmountOfItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.itemAmtHook != null)
            {
                returnValue = this.itemAmtHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call item amount hook", System.Drawing.Color.Red);
            }
            if (this.inventoryHandler.isActive)
            {
                returnValue->Real = 5;
            }

            return returnValue;
        }

        // Get the item info of AP shop items
        internal void GetUnclaimedShopItems(int position, out ScoutedItemInfo? info, out long archipelagoItem, out bool useArchipelagoItem, out long id)
        {
            if (conn.session != null)
            {
                if (this.inventoryHandler.ShopSanity == InventoryHandler.ShopSetting.Global)
                {
                    id = conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, SHOP_POSITIONS[position]);
                    if (!conn.session.Locations.AllLocationsChecked.Contains(id))
                    {
                        info = scoutHandler.shopContents.Result[id];
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
                else if (this.inventoryHandler.ShopSanity == InventoryHandler.ShopSetting.Regional)
                {
                    id = conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, LocationUtil.GetBaseLocation() + " " + SHOP_POSITIONS[position]);
                    if (!conn.session.Locations.AllLocationsChecked.Contains(id))
                    {
                        info = scoutHandler.shopContents.Result[id];

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
            id = -1;
        }

        // Set the item inside the chest to the proper item
        internal RValue* SetItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (this.inventoryHandler.isActive)
                {
                    var location = LocationUtil.GetLocationType();

                    // If the item that is being created is a chest loot item
                    if (location == LocationUtil.LocationType.Chest)
                    {
                        // Set the item to the archipelago item
                        if (this.inventoryHandler.checksPerItemInChest)
                        {
                            for (var i = 0; i < 5; i++)
                            {
                                // Determine which slot item we are at, which should be the first -1
                                if (HookUtil.IsEqualToNumeric(rnsReloaded.ArrayGetEntry(rnsReloaded.ArrayGetEntry(rnsReloaded.ArrayGetEntry(rnsReloaded.FindValue(self, "slots"), 1), i), 1), -1))
                                {
                                    var locationId = GetChestPositionLocationId(SlotIdToChestPos(i));
                                    var info = scoutHandler.chestContents.Result[locationId];

                                    if (conn.session != null && MessageHandler.HintConfigIsOn(modConfig, info.Flags))
                                    {
                                        conn.session.Locations.ScoutLocationsAsync(HintCreationPolicy.CreateAndAnnounceOnce, locationId);
                                    }

                                    // If the location is checked
                                    if (conn.session != null && conn.session.Locations.AllLocationsChecked.Contains(locationId))
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
                        else if (this.inventoryHandler.ItemSanity != InventoryHandler.ItemSetting.None)
                        {
                            if (this.inventoryHandler.AvailableItems.Count == 0)
                            {
                                *argv[0] = new RValue(0);
                            }
                            else
                            {
                                var index = this.rand.Next(this.inventoryHandler.AvailableItems.Count);
                                *argv[0] = new RValue(this.inventoryHandler.AvailableItems[index]);
                            }
                        }
                    }
                    // If the item that is being created is a shop loot item
                    else if (location == LocationUtil.LocationType.Shop)
                    {
                        // Determine which slot item we are at, which should be the first -1
                        for (var i = 0; i < 9; i++)
                        {
                            if (HookUtil.IsEqualToNumeric(rnsReloaded.ArrayGetEntry(rnsReloaded.ArrayGetEntry(rnsReloaded.ArrayGetEntry(rnsReloaded.FindValue(self, "slots"), 2), i), 1), -1))
                            {
                                GetUnclaimedShopItems(i, out ScoutedItemInfo? info, out long archipelagoItem, out bool useArchipelagoItem, out long locationId);

                                if (conn.session != null && info != null && MessageHandler.HintConfigIsOn(modConfig, info.Flags))
                                {
                                    conn.session.Locations.ScoutLocationsAsync(HintCreationPolicy.CreateAndAnnounceOnce, locationId);
                                }

                                switch (i)
                                {
                                    case 0:
                                        this.shopItemsHandler.SetHpPotion(argv, archipelagoItem, useArchipelagoItem);
  
                                        break;
                                    case 1:
                                        this.shopItemsHandler.SetLevelPotion(argv, archipelagoItem, useArchipelagoItem);
 
                                        break;
                                    case 2:
                                    case 3:
                                    case 4:
                                        this.shopItemsHandler.SetPotion(argv, archipelagoItem, useArchipelagoItem);
    
                                        break;
                                    case 5:
                                        this.shopItemsHandler.SetPrimaryUpgrade(argv, archipelagoItem, useArchipelagoItem);

                                        break;
                                    case 6:
                                        this.shopItemsHandler.SetSecondaryUpgrade(argv, archipelagoItem, useArchipelagoItem);
  
                                        break;
                                    case 7:
                                        this.shopItemsHandler.SetSpecialUpgrade(argv, archipelagoItem, useArchipelagoItem);
  
                                        break;
                                    case 8:
                                        this.shopItemsHandler.SetDefensiveUpgrade(argv, archipelagoItem, useArchipelagoItem);

                                        break;
                                }

                                if (this.itemSetHook != null)
                                {
                                    returnValue = this.itemSetHook.OriginalFunction(self, other, returnValue, argc, argv);
                                }
                                else
                                {
                                    this.logger.PrintMessage("Unable to call item set hook", System.Drawing.Color.Red);
                                }

                                return returnValue;
                            }
                        }
                    }
                    else if (location == LocationUtil.LocationType.SpecialChest)
                    {
                        if (this.inventoryHandler.ItemSanity != InventoryHandler.ItemSetting.None)
                        {
                            if (this.inventoryHandler.AvailableItems.Count == 0)
                            {
                                *argv[0] = new RValue(0);
                            }
                            else
                            {
                                var index = rand.Next(this.inventoryHandler.AvailableItems.Count);
                                *argv[0] = new RValue(this.inventoryHandler.AvailableItems[index]);
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

            if (this.itemSetHook != null)
            {
                returnValue = this.itemSetHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call item set hook", System.Drawing.Color.Red);
            }

            return returnValue;
        }

        // Get the ingame item id for the first archipelago item
        internal RValue* GetItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (rnsReloaded.GetString(argv[0]).Contains("ArchipelagoItems"))
                {
                    baseItemId = HookUtil.GetNumeric(rnsReloaded.FindValue(self, "item_data_entry_max")) + 1;
                }
            }

            if (this.itemGetHook != null)
            {
                returnValue = this.itemGetHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call item get hook", System.Drawing.Color.Red);
            }

            return returnValue;
        }

        // Set the description for archipelago items to reflect their actual item
        internal RValue* SetItemsDescription(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.itemSetDescriptionHook != null)
            {
                returnValue = this.itemSetDescriptionHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call item set description hook", System.Drawing.Color.Red);
            }

            if (this.inventoryHandler.isActive)
            {

                //if an archipelago item, set the description to the real item
                if (HookUtil.IsEqualToNumeric(argv[0], baseItemId) || HookUtil.IsEqualToNumeric(argv[0], baseItemId + 1) || HookUtil.IsEqualToNumeric(argv[0], baseItemId + 2))
                {
                    ScoutedItemInfo? info = null;
                    //TODO: Look into making the better
                    var safeSelf = new RValue(self);

                    if (this.inventoryHandler.checksPerItemInChest && LocationUtil.GetLocationType() == LocationUtil.LocationType.Chest)
                    {
                        info = scoutHandler.chestContents.Result[GetChestPositionLocationId(SlotIdToChestPos((int)HookUtil.GetNumeric(safeSelf["slotId"])))];
                    }
                    else if (this.inventoryHandler.ShopSanity != InventoryHandler.ShopSetting.None && LocationUtil.GetLocationType() == LocationUtil.LocationType.Shop)
                    {
                        if (conn.session != null)
                        {
                            if (this.inventoryHandler.ShopSanity == InventoryHandler.ShopSetting.Global)
                            {
                                info = scoutHandler.shopContents.Result[conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, SHOP_POSITIONS[(int)HookUtil.GetNumeric(safeSelf["slotId"])])];
                            }
                            else if (this.inventoryHandler.ShopSanity == InventoryHandler.ShopSetting.Regional)
                            {
                                info = scoutHandler.shopContents.Result[conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, LocationUtil.GetBaseLocation() + " " + SHOP_POSITIONS[(int)HookUtil.GetNumeric(safeSelf["slotId"])])];
                            }
                        }
                    }

                    if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
                    {
                        if (info != null)
                        {
                            var player = info.Player.Slot == this.conn.messageHandler.slot ? "your" : info.Player.Name + "'s";

                            rnsReloaded.CreateString(returnValue, info.ItemDisplayName + " for " + player + " world");
                        }
                        else
                        {
                            rnsReloaded.CreateString(returnValue, "Unable to fetch archipelago item data");
                        }
                    }
                }
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
            var notchType = LocationUtil.GetLocationType();

            if (notchType == LocationUtil.LocationType.Shop)
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
                default: return "";
            }
        }

        // Get the archipelago location id for the current chest's item at chestPos
        private long GetChestPositionLocationId(string chestPos)
        {
            if (conn.session != null)
            {
                return conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, LocationUtil.GetBaseLocation() + chestPos);
            }
            return -1;
        }

        // Prevent 'fake' items from actually being taken
        internal RValue* TakeItem(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (this.inventoryHandler.isActive)
                {
                    var itemPos = HookUtil.GetNumeric(argv[2]);
                    CLayerElementBase* instance = null;

                    if (LocationUtil.GetLocationType() == LocationUtil.LocationType.Chest)
                    {
                        HookUtil.FindElementInLayer("LootInfo", "slotId", itemPos + "", out instance);
                    }
                    else if (LocationUtil.GetLocationType() == LocationUtil.LocationType.Shop)
                    {
                        HookUtil.FindElementInLayer("InventoryInfo", "slotId", itemPos + "", out instance);
                    }

                    if (instance != null)
                    {
                        var element = ((CLayerInstanceElement*)instance)->Instance;
                        var itemId = rnsReloaded.FindValue(element, "itemId");

                        // Take the item if its not an ap item
                        if (!HookUtil.IsEqualToNumeric(itemId, baseItemId) && !HookUtil.IsEqualToNumeric(itemId, baseItemId + 1) && !HookUtil.IsEqualToNumeric(itemId, baseItemId + 2))
                        {
                            if (this.takeItemHook != null)
                            {
                                returnValue = this.takeItemHook.OriginalFunction(self, other, returnValue, argc, argv);
                            }
                            else
                            {
                                this.logger.PrintMessage("Unable to call take item hook", System.Drawing.Color.Red);
                            }

                            return returnValue;
                        }

                        // Send the AP item
                        if (this.inventoryHandler.checksPerItemInChest && LocationUtil.GetLocationType() == LocationUtil.LocationType.Chest)
                        {
                            var locationPacket = new LocationChecksPacket { Locations = [GetChestPositionLocationId(SlotIdToChestPos((int)HookUtil.GetNumeric(rnsReloaded.FindValue(element, "slotId"))))] };
                            conn.session?.Socket.SendPacketAsync(locationPacket);
                        }
                        else if (this.inventoryHandler.ShopSanity != InventoryHandler.ShopSetting.None && LocationUtil.GetLocationType() == LocationUtil.LocationType.Shop)
                        {
                            if (conn.session != null)
                            {
                                if (this.inventoryHandler.ShopSanity == InventoryHandler.ShopSetting.Global)
                                {
                                    var locationPacket = new LocationChecksPacket { Locations = [conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, SHOP_POSITIONS[(int)HookUtil.GetNumeric(argv[2])])] };
                                    conn.session?.Socket.SendPacketAsync(locationPacket);
                                }
                                else if (this.inventoryHandler.ShopSanity == InventoryHandler.ShopSetting.Regional)
                                {
                                    var locationPacket = new LocationChecksPacket { Locations = [conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, LocationUtil.GetBaseLocation() + " " + SHOP_POSITIONS[(int)HookUtil.GetNumeric(argv[2])])] };
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

                        return returnValue;
                    }
                }
            }

            if (this.takeItemHook != null)
            {
                returnValue = this.takeItemHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call take item hook", System.Drawing.Color.Red);
            }

            return returnValue;
        }

        // Send the location for finishing a notch if there is a generic location for it, i.e. battle, chest, or boss (not shop)
        internal void SendNotchLoctaion()
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                var baseLocation = LocationUtil.GetBaseLocation();

                var character = HookUtil.GetClass();

                if (conn.session != null)
                {
                    long[] locations = [conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, baseLocation), conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, baseLocation + " - " + character)];
                    conn.session.Locations.CompleteLocationChecksAsync(locations);
                }
            }
        }

        // Make the next notch be an ingame only chest
        private void AddChestToNotch()
        {
            treasurespheresToSpawn++;
        }

        // On outskirts loading, besides loading into lobby, add the treasurespheres we have accumulated
        internal RValue* SpawnTreasuresphere(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.spawnTreasuresphereHook != null)
            {
                returnValue = this.spawnTreasuresphereHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call spawn trasuresphere hook", System.Drawing.Color.Red);
            }

            if (treasurespheresToSpawn > 0)
            {
                if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
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

                    treasurespheresToSpawn--;
                }
            }

            return returnValue;
        }

        // On outskirts loading, besides loading into lobby, add the treasurespheres we have accumulated
        internal RValue* SpawnTreasuresphereOnStart(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            this.rnsReloadedRef.TryGetTarget(out var rnsReloaded);

            // Perform normal action for menu / starting kingdom
            if (this.spawnTreasuresphereOnStartNHook != null && (!inventoryHandler.isActive || (rnsReloaded != null && HookUtil.IsEqualToNumeric(rnsReloaded.FindValue(self, "hallwayPos"), 0))))
            {
                returnValue = this.spawnTreasuresphereOnStartNHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            // Due to a bug on the 6th kingdom, manually call each kingdoms hallway gen and update the notch icons
            else if (this.spawnTreasuresphereOnStartNHook != null)
            {
                if (rnsReloaded != null)
                {
                    string hallkey = rnsReloaded.ArrayGetEntry(rnsReloaded.FindValue(self, "hallkey"), (int) HookUtil.GetNumeric(rnsReloaded.FindValue(self, "hallwayPos")))->ToString();

                    GenerateHallway(self, hallkey);
                    *rnsReloaded.FindValue(self, "stageNameRefresh") = new RValue(1);
                    UpdateNotchIcons(self, hallkey);
                    *rnsReloaded.ArrayGetEntry(rnsReloaded.FindValue(self, "hallsubimg"), (int)HookUtil.GetNumeric(rnsReloaded.FindValue(self, "hallwayPos"))) = new RValue(HookUtil.GetNumeric(rnsReloaded.FindValue(self, "stageKey"))); // Getting numberic here just in case
                }
            }
            else
            {
                this.logger.PrintMessage("Unable to call spawn trasuresphere on start n hook", System.Drawing.Color.Red);
            }

            if (rnsReloaded != null)
            {
                var kingdomName = rnsReloaded.FindValue(self, "stageName")->ToString();
                kingdomName = kingdomName.Replace(Environment.NewLine, " ");

                if (kingdomName.Equals("Kingdom Outskirts") || kingdomName.Equals("Crack in the Geode"))
                {
                    for (int i = 0; i < this.inventoryHandler.AvailableTreasurespheres; i++)
                    {
                        treasurespheresToSpawn++;
                    }
                }
            }

            return returnValue;
        }

        // Generate the notch data for the given hallway
        internal void GenerateHallway(CInstance* self, string hallkey)
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                switch (hallkey)
                {
                    case "hw_nest":
                        rnsReloaded.ExecuteScript("scr_hallwaygen_nest", self, null, []);
                        break;
                    case "hw_arsenal":
                        rnsReloaded.ExecuteScript("scr_hallwaygen_arsenal", self, null, []);
                        break;
                    case "hw_lakeside":
                        rnsReloaded.ExecuteScript("scr_hallwaygen_lakeside", self, null, []);
                        break;
                    case "hw_streets":
                        rnsReloaded.ExecuteScript("scr_hallwaygen_streets", self, null, []);
                        break;
                    case "hw_lighthouse":
                        rnsReloaded.ExecuteScript("scr_hallwaygen_lighthouse", self, null, []);
                        break;
                    case "hw_keep":
                        rnsReloaded.ExecuteScript("scr_hallwaygen_keep", self, null, []);
                        break;
                    case "hw_pinnacle":
                        rnsReloaded.ExecuteScript("scr_hallwaygen_pinnacle", self, null, []);
                        break;
                    case "hw_depths":
                        rnsReloaded.ExecuteScript("scr_hallwaygen_depths", self, null, []);
                        break;
                    case "hw_aurum":
                        rnsReloaded.ExecuteScript("scr_hallwaygen_aurum", self, null, []);
                        break;
                    case "hw_sanct":
                        rnsReloaded.ExecuteScript("scr_hallwaygen_sanct", self, null, []);
                        break;
                    case "hw_darkhall":
                        rnsReloaded.ExecuteScript("scr_hallwaygen_darkhall", self, null, []);
                        break;
                    case "hw_reflection":
                        rnsReloaded.ExecuteScript("scr_hallwaygen_reflection", self, null, []);
                        break;
                }
                
            }
        }

        // Set the notch icons for the given hallway
        internal void UpdateNotchIcons(CInstance* self, string hallkey)
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                var icons = rnsReloaded.FindValue(self, "xSubimg");
                switch (hallkey)
                {
                    case "hw_pinnacle":
                    case "hw_reflection":
                        *rnsReloaded.ArrayGetEntry(icons, 0) = new RValue(3);
                        *rnsReloaded.ArrayGetEntry(icons, 1) = new RValue(4);
                        *rnsReloaded.ArrayGetEntry(icons, 2) = new RValue(6);
                        break;
                    case "hw_nest":
                    case "hw_arsenal":
                    case "hw_lakeside":
                    case "hw_streets":
                    case "hw_lighthouse":
                    case "hw_keep":
                    case "hw_depths":
                    case "hw_aurum":
                    case "hw_sanct":
                    case "hw_darkhall":
                        *rnsReloaded.ArrayGetEntry(icons, 0) = new RValue(2);
                        *rnsReloaded.ArrayGetEntry(icons, 1) = new RValue(0);
                        *rnsReloaded.ArrayGetEntry(icons, 2) = new RValue(0);
                        *rnsReloaded.ArrayGetEntry(icons, 3) = new RValue(0);
                        *rnsReloaded.ArrayGetEntry(icons, 4) = new RValue(1);
                        *rnsReloaded.ArrayGetEntry(icons, 5) = new RValue(4);
                        break;
                    default: // Default to the starting kingdom
                        *rnsReloaded.ArrayGetEntry(icons, 0) = new RValue(3);
                        *rnsReloaded.ArrayGetEntry(icons, 1) = new RValue(0);
                        *rnsReloaded.ArrayGetEntry(icons, 2) = new RValue(1);
                        *rnsReloaded.ArrayGetEntry(icons, 3) = new RValue(0);
                        *rnsReloaded.ArrayGetEntry(icons, 4) = new RValue(1);
                        *rnsReloaded.ArrayGetEntry(icons, 5) = new RValue(0);
                        break;
                }
            }
        }

        // Remove the go to next screen loader if disconnected
        internal RValue* StopReadyCheck(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (!this.inventoryHandler.isActive && HookUtil.IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("obLobbyType"), 3))
                {
                    HookUtil.FindElementInLayer("PlayerField", "percent", out var fieldElement);
                    if (fieldElement != null)
                    {
                        var fieldInstance = new RValue(((CLayerInstanceElement*)fieldElement)->Instance);
                        *fieldInstance.Get("percent") = new(-0.1);
                    }
                }
            }

            if (this.readyCheckHook != null)
            {
                returnValue = this.readyCheckHook.OriginalFunction(self, other, returnValue, argc, argv);
            }

            return returnValue;
        }

        // Send the goal packet
        private void SendGoal()
        {
            conn.session?.SetGoalAchieved();
        }
    }
}