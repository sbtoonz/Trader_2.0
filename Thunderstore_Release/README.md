
![](https://github.com/sbtoonz/Trader_2.0/raw/master/Images/Knarr.png)
****

This is a new trader you can find in Valheim. He is Haldors cousin...only by marriage though. 

You can find this trader randomly spawned in the meadows. Alternatively you can always spawn him using the prefab name
``Knarr``

Knarr has a fully server sync'd config and store configuration yaml file
This file also has a filewatcher for it built into the mod
that means that when you edit this file the store updates in game!


![](https://github.com/sbtoonz/Trader_2.0/raw/master/Images/ezgif.com-gif-maker%20(3).gif)
### Configuration

There is a yaml file that is generated on first launch of the mod. This file will live in your BepInEx/configs folder and becalled trader_config.yaml

You are required to put the items you wish the trader to sell as well as their cost and stack in this YAML file 

Example as shown:
```yaml
PrefabName:
  cost: 100
  stack: 10
  inventory count: 1

Acorns:
  cost: 1 
  stack: 1
  inventory count: 10000
```

where you see PrefabName you need to put the prefab the trader should sell. Yes this trader supports custom items.


## Known issues:
* None yet please report if any

## Join us on discord

<p align="center"><h2>For Questions or Comments please join the Odin Plus Team on Discord:</h2></p>

<p align="center"><a href="https://discord.gg/mbkPcvu9ax"><img src="https://i.imgur.com/Ji3u63C.png"></a></p>


### V0.0.1
* Initial release

### V0.0.2
* Added prefab for currency as a config option
* Fixed potential incompatibility when loading Objects into trader inventory

### V0.0.3
* Updated the materials on the store background to get cloned at runtime from other UI elements that way when its dark the panel will be dark etc
* Fixed reported errors on the menu patch


### V0.0.4
* Made it fail gracefully if you put a bad entry in the YML
* Made SellSFX play random instead of all at once
* TODO: Tie SFX into audio mixer global group


### V0.0.5
* Added yaml file watcher for server only, if file is updated on server it is live updated in game
* Setup custom value synchronization for the traders config entries clients get servers config values now

### V0.0.6
* Fix typo in README

### V0.0.7
* Take PR from @blaxxun-boop of CLLC that fixes filewatcher on *nix servers 


### V0.0.8
* Start framework for inventory of items Knarr can now hold a fixed count of things.

### V0.0.9
* Add GUI elements to indicate if there is an inventory count of the item Inventory count example YML included with mod

### V0.1.0
* Added dragNdrop to the store screen UI element. You can now move it around the screen if you please

### V0.1.1
* Fixed issue where if item stack sale made inventory go negative it was not removed from store

### V0.1.2 
* Added config option to remove randomly spawning Knarr from your world

### V0.1.3
* Changed the stores loading tactic to an async/await method. This makes stores with huge item counts loud instantly and no longer take up FPS when loading in items

### V0.1.4
* Added config option to log sales Knarr makes, this includes who purchased the item, what item was sold, and the currency volume spent on the item
* Added fix for randomly spawning Knarr (hopefully)
* Updated serversync binary