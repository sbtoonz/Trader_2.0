
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

Acorns:
  cost: 1 
  stack: 1
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
