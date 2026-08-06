
# Rabbit and Steel Archipelago

Adds support for [Archipelago](https://archipelago.gg/) to Rabbit and Steel. Currently a Work in Progress mod.

## APWorld

The APWorld can be found in the releases tab [here](https://github.com/tjwombo/RNSArchipelago/releases)

## Bug Reports

If you found any bugs, please report it in the Archipelago discord server in the Rabbit and Steel channel [here](https://discord.com/channels/731205301247803413/1241105661425877194)

## Installation

**Note Reloaded-II can have some trouble downloading/updating, so it is recommended to make sure you are using the correct version before starting a multiworld**

This mod uses [R2Reloaded](https://github.com/Reloaded-Project/Reloaded-II)

You can install the mod thorugh Reloaded by adding Rabbit and Steel as an application by clicking the + button and then
1. Download the mod by clicking the ⚙+ button
2. Filter All Sources to RabbitSteel.exe
3. Search "RnS Reloaded" and install it

Alternatively, it can be downloaded manually by 
1. Downloading the .7z file in releases
2. Remove the version number from the name so that its RnSArchipelago.7z
3. Dragging it (while still zipped) onto the Reloaded 2 "Configure Mods" screen, which is reached by clicking on the Rabbit and Steel Icon

Then on the "Configure Mods" screen make sure the mod is active, box should be red and have a + instead of -
- RNSReloaded should have a filled in red box

And then hit "Launch Application"

## Connecting to Archipelago
After launching the game through reloaded, a new lobby tab "ARCHIPELAGO" should appear. You must create the lobby under this tab in order to connect to Archipelago

You can enter your archipelago address, player name, and password by clicking on the edit lobby settings while on the AP tab.
- This is prefilled with the data in the mod conifg and automaitically updates the mod config with your most recent settings after leaving an AP lobby
- **Note if the game closes before returning to this lobby setting screen, the new info may not get saved correctly, and the original lobby settings may not get restored correctly.**

If you get disconnected mid run, the "ONWARD" loading area will not appear.
You can reconnect by pausing the game and hit the reconnect button at the top to try reconnecting
- Upon reconnection, the loading area should reappear.
- Any checks obtained while disconnected will not be sent. This will eventually be updated.

## Dependencies
This mod requires two dependencies, reloaded.sharedlib.hooks and RNSReloaded (along with their dependencies) which should be installed automatically regardless of which instalation method you choose.
- If RNSReloaded does not appear in the mods list you can download it manually at [RNSReloaded](https://packages.sewer56.moe/packages/rnsreloaded).
  1. Download package on the right side
  2. Unzip the .nupkg file
  3. Move the files in \contentFiles\any\Sewer56.Update to a folder called RNSReloaded in your reloaded2 mods folder

## Configuration

If you right click the mod in R2Reloaded, there is a Configure button.
There are various settings you can set to fine-tune archipelago settings.
Sending commands to the server is not currently supported.

If for some reason, the AP item mod isn't installed automatically, you will need to turn on the "Skip ArchipelagoItem Folder Creation" configuration and install it manually.
- With the config on, anytime the AP item mod has an update, you will need to update it yourself

To do so, download the ArchipelagoItems.zip file in the releases tab and extract it into your Rabbit and Steel mods folder like so steamapps\common\Rabbit and Steel\mods\ArchipelagoItems

## Kudos

- Everyone who helped make RNSReloaded
- Everyone in the Archipelago discord for all the amazing work they do
- Everyone who helped in testing and bugfixing
- RavingMagicMan and Straybard for helping me organize my mess of notes and testing