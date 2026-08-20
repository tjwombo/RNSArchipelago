using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;
using RnSArchipelago.Utils;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;
using static RnSArchipelago.Game.InventoryHandler;

namespace RnSArchipelago.Game
{
    internal unsafe class MapHandler
    {
        private readonly WeakReference<IRNSReloaded> rnsReloadedRef;
        private readonly ILogger logger;
        private readonly InventoryHandler inventoryHandler;
        private readonly RouteHandler routeHandler;

        internal IHook<ScriptDelegate>? fixChooseIconsHook;

        internal MapHandler(WeakReference<IRNSReloaded> rnsReloadedRef, ILogger logger, InventoryHandler inventoryHandler, RouteHandler routeHandler)
        {
            this.rnsReloadedRef = rnsReloadedRef;
            this.logger = logger;
            this.inventoryHandler = inventoryHandler;
            this.routeHandler = routeHandler;
        }

        internal static string GetSelectedKingdom(WeakReference<IRNSReloaded> rnsReloadedRef)
        {
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                HookUtil.FindElementInLayer("ItemExtra", "buttonAvailable", out var map);
                if (map != null)
                {
                    var instance2 = (CLayerInstanceElement*)map;
                    var instanceValue2 = new RValue(instance2->Instance);

                    var routeIcons = instanceValue2.Get("buttonAvailable");
                    return HookUtil.GetNumeric(rnsReloaded.ArrayGetLength(routeIcons)!.Value) switch
                    {
                        6 or 7 => HookUtil.GetNumeric(instanceValue2.Get("selectedPos")) switch
                        {
                            0 => "hw_keep",
                            1 => "hw_nest",
                            2 => "hw_arsenal",
                            3 => "hw_lighthouse",
                            4 => "hw_streets",
                            5 => "hw_lakeside",
                            _ => "",
                        },
                        4 => HookUtil.GetNumeric(instanceValue2.Get("selectedPos")) switch
                        {
                            0 => "hw_darkhall",
                            1 => "hw_sanct",
                            2 => "hw_depths",
                            3 => "hw_aurum",
                            _ => "",
                        },
                        2 => HookUtil.GetNumeric(instanceValue2.Get("selectedPos")) switch
                        {
                            0 => "hw_outskirts",
                            1 => "hw_geode",
                            _ => "",
                        },
                        _ => "",
                    };
                }
            }
            return "";
        }

        private void SetRouteIconInfo(int index, CLayerElementBase* element, int activated, string desc, string title)
        {
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                var instance = (CLayerInstanceElement*)element;
                var instanceValue = new RValue(instance->Instance);

                HookUtil.ModifyElementVariable(element, "buttonAvailable", HookUtil.ModificationType.ModifyArray, [new(index), new(activated)]);

                RValue description = new();
                rnsReloaded.CreateString(&description, desc);
                *(instanceValue.Get("buttonStr")->Get(index)) = description;

                RValue titleValue = new();
                rnsReloaded.CreateString(&titleValue, title);
                *(instanceValue.Get("buttonTitle")->Get(index)) = titleValue;
            }
        }


        // Toggle the kingdom icons on the route selection screen to only display runnable kingdoms + the pale keep for a random one
        internal void ModifyRouteKingdomIcons(RValue* buttons, int buttonCount, CLayerElementBase* element)
        {
            if (buttonCount >= 6)
            {
                routeHandler.lastVisitedRunType = "kingdom";
                List<string> kingdoms = KingdomUtil.GetRunnableKingdoms(ref routeHandler.lastVisitedRunType);

                if ((inventoryHandler.AvailableKingdoms & KingdomFlags.The_Pale_Keep) != 0)
                {
                    SetRouteIconInfo(0, element, 1, "The Pale Keep will be visited if possible.", "The Pale Keep");
                }
                else
                {
                    SetRouteIconInfo(0, element, 0, "A route will be taken through at random.", "Random");
                }

                if (kingdoms.Contains("hw_nest"))
                {
                    SetRouteIconInfo(1, element, 1, "The Scholar's Nest will be visited first.", "Scholar's Nest");
                }
                else
                {
                    SetRouteIconInfo(1, element, 0, "A route will be taken through at random.", "Random");
                }

                if (kingdoms.Contains("hw_arsenal"))
                {
                    SetRouteIconInfo(2, element, 1, "The King's Arsenal will be visited first.", "King's Arsenal");
                }
                else
                {
                    SetRouteIconInfo(2, element, 0, "A route will be taken through at random.", "Random");
                }

                if (kingdoms.Contains("hw_lighthouse"))
                {
                    SetRouteIconInfo(3, element, 1, "The Red Darkhouse will be visited first.", "Red Darkhouse");
                }
                else
                {
                    SetRouteIconInfo(3, element, 0, "A route will be taken through at random.", "Random");
                }

                if (kingdoms.Contains("hw_streets"))
                {
                    SetRouteIconInfo(4, element, 1, "The Churchmouse Streets will be visited first.", "Churchmouse Streets");
                }
                else
                {
                    SetRouteIconInfo(4, element, 0, "A route will be taken through at random.", "Random");
                }

                if (kingdoms.Contains("hw_lakeside"))
                {
                    SetRouteIconInfo(5, element, 1, "The Emerald Lakeside will be visited first.", "Emerald Lakeside");
                }
                else
                {
                    SetRouteIconInfo(5, element, 0, "A route will be taken through at random.", "Random");
                }

                // Always disallow the extras
                *(buttons->Get(6)) = new(0);
                *(buttons->Get(7)) = new(0);
            }
            else if (buttonCount == 4)
            {
                routeHandler.lastVisitedRunType = "extra";
                List<string> kingdoms = KingdomUtil.GetRunnableKingdoms(ref routeHandler.lastVisitedRunType);

                if ((inventoryHandler.AvailableKingdoms & KingdomFlags.Looping_Hallway) != 0)
                {
                    SetRouteIconInfo(0, element, 1, "The Looping Hallway will be visited if possible.", "Looping Hallway");
                }
                else
                {
                    SetRouteIconInfo(0, element, 0, "A route will be taken through at random.", "Random");
                }

                if (kingdoms.Contains("hw_sanct"))
                {
                    SetRouteIconInfo(1, element, 1, "The Subterra Sanctum will be visited first.", "Subterra Sanctum");
                }
                else
                {
                    SetRouteIconInfo(1, element, 0, "A route will be taken through at random.", "Random");
                }

                if (kingdoms.Contains("hw_depths"))
                {
                    SetRouteIconInfo(2, element, 1, "The Darkhouse Depths will be visited first.", "Darkhouse Depths");
                }
                else
                {
                    SetRouteIconInfo(2, element, 0, "A route will be taken through at random.", "Random");
                }

                if (kingdoms.Contains("hw_aurum"))
                {
                    SetRouteIconInfo(3, element, 1, "The Atelier Aurum will be visited first.", "Atelier Aurum");
                }
                else
                {
                    SetRouteIconInfo(3, element, 0, "A route will be taken through at random.", "Random");
                }
            }
            else if (buttonCount == 2)
            {
                if ((inventoryHandler.AvailableKingdoms & KingdomFlags.Kingdom_Outskirts) != 0)
                {
                    SetRouteIconInfo(0, element, 1, "The Kingdom Outskirts will be visited.", "Kingdom Outskirts");
                } else
                {
                    SetRouteIconInfo(0, element, 0, "A route will be taken through at random.", "Random");
                }

                if ((inventoryHandler.AvailableKingdoms & KingdomFlags.Crack_in_the_Geode) != 0)
                {
                    SetRouteIconInfo(1, element, 1, "The Crack In The Geode will be visited.", "Crack In The Geode");
                }
                else
                {
                    SetRouteIconInfo(1, element, 0, "A route will be taken through at random.", "Random");
                }
            }
        }

        // If we are on the route selection screen, update it to match the available kingdoms
        internal RValue* ModifyRouteIcons(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (this.fixChooseIconsHook != null)
                {
                    returnValue = this.fixChooseIconsHook.OriginalFunction(self, other, returnValue, argc, argv);
                }
                else
                {
                    this.logger.PrintMessage("Unable to call fix choose icons hook", System.Drawing.Color.Red);
                }
                if (this.inventoryHandler.isActive)
                {
                    HookUtil.FindElementInLayer("ItemExtra", "buttonAvailable", out var element);

                    if (element == null)
                    {
                        return returnValue;
                    }

                    var instance = (CLayerInstanceElement*)element;
                    var instanceValue = new RValue(instance->Instance);

                    var routeIcons = instanceValue.Get("buttonAvailable");
                    var buttonCount = rnsReloaded.ArrayGetLength(routeIcons);

                    if (routeIcons != null && routeIcons->ToString() != "unset" && buttonCount.HasValue)
                    {
                        ModifyRouteKingdomIcons(routeIcons, (int)HookUtil.GetNumeric(buttonCount.Value), element);
                        returnValue = routeIcons->Get((int)HookUtil.GetNumeric(buttonCount.Value) - 1);
                    }
                }
                return returnValue;
            }
            else
            {
                if (this.fixChooseIconsHook != null)
                {
                    returnValue = this.fixChooseIconsHook.OriginalFunction(self, other, returnValue, argc, argv);
                }
                else
                {
                    this.logger.PrintMessage("Unable to call fix choose icons hook", System.Drawing.Color.Red);
                }
            }

            return returnValue;
        }
    }
}
