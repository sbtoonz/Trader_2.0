
![](https://github.com/sbtoonz/Trader_2.0/raw/master/Images/Knarr.png)
****

This is a new trader you can find in Valheim. He is Haldors cousin...only by marriage though. 

You can find this trader randomly spawned in the meadows. Alternatively you can always spawn him using the prefab name
``Knarr``

Knarr has a fully server sync'd config and store configuration yaml file
This file also has a filewatcher for it built into the mod
that means that when you edit this file the store updates in game!


![](https://cdn.discordapp.com/attachments/1081545225287249930/1085701970825842798/ezgif.com-video-to-gif.gif)
### Configuration

There is a yaml file that is generated on first launch of the mod. This file will live in your BepInEx/configs folder and becalled trader_config.yaml

You are required to put the items you wish the trader to sell as well as their cost and stack in this YAML file 

Example as shown:
```yaml
Wood:
  cost: 10
  stack: 10
  inventory count: 100
  purchase price: 5
LeatherScraps:
  cost: 10
  stack: 5
  inventory count: -1
  purchase price: 50
```

where you see PrefabName you need to put the prefab the trader should sell. Yes this trader supports custom items.


## Known issues:
* None yet please report if any

## Join us on discord

<p align="center"><h2>For Questions or Comments find knarr related stuff here:</h2></p>
<a href="https://discord.gg/4znsrzzuXY"> HoremVerse Modding - Knarrs Corner </a>

<details>
<summary>Old Release Notes</summary>

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

### V0.1.5
* Finally fixed randomly spawning Knarr from throwing obscene volume of errors What Happened? When using the method I wanted the game attempted to add 2x network components to Knarr and it caused issues this is now fixed

### V0.1.6
* Fixed UI Window being able to be dragged out of the game screen
* Added terminal command to clean up Knarr from the world if you have previous bad spawns of him. To enter this command bring up your terminal by hitting f5 and type `remove knarr` if you are an admin the command will wipe knarr from that map

### V0.1.7
* Fixed random stacking bug 
* Added terminal command to find knarr spawns in your world `find knarr` in your console to locate random spawn and player spawned versions of knarr

### V0.1.8
* Fixed bug in random spawning Knarr
* Fixed terminal command for locating knarr to lock to admin

### V0.1.9
* Fixed Auga incompatibility with newest release

### V0.2.0
* Added more graceful failure message if you have bad prefab in config for currency
* Altered prefab resolution tactic to work with ZnetScene.instance instead of ObjectDB. All things must have an ItemDrop that Knarr vends
* Added configuration option to only allow knarr to vend the items a player knows recipes for

### V0.2.1
* Gave some more graceful failures for when issues present themselves 
* UI Will no longer open if your store is empty
* Fixed location not being used from config for store screen 

### V0.2.2
* Fixed up RPC for Find Knarr to work on servers.
* Added a repair icon to the store window You can set a lucky number in the config. When pressing the repair button a virtual dice will roll. If this dice hits your lucky number repairs will go through


### V0.2.3
* Fixed some annoying stack bug that was introduced in recent update 
* updated ServerSync to newest master 
* Knarr is now using the in game 'creature' shader which should give him a better feeling look in game

### V0.2.4
* Fix bug caught by Horem (interferes with Jewelcrafting and OdinsQOL)
* Changed the way the PlayerBase effectArea was made so you cant interact with knarr from super far now 

### V0.2.5
* Knarr can now buy things from the player!, This has a new line entry your your trader_config.yaml file that is called 'purchase price' if this value is set to 0 the item will not show in the list of things knarr can buy from the player

### V0.2.6
* Added options to hide repair tab
* Added option to hide sell tab

### V0.2.7
* ServerSync Update

### V0.2.8
* ServerSync Update

### V0.2.9
* Mistlands Update

### V0.3.0
* Avoid package deprecation 

### V0.3.1
* Add configuration option to turn off knarr's audio cue's when walking up to him

### V0.3.2
* Alpha Gamepad support is here for knarr now for those who care.

### V0.3.3
* Remove some artifact rotation controlling code from Knarr
* Fix server join issues with knarr. During refactor of ShaderReplacer code I did introduce a bug which prevented knarr from being started on some server instances properly. This should now be resolved.



### V0.3.4
* Updated Knarr to use TextMesh pro ... cuz I have been waiting since valheim launched to get this in game
* Major performance upgrades when using huge item lists with knarr (techy terms are object pooling)
* Updated to be compat with latest game version

### V0.3.5
* Fixed a whoopsy I missed in last push. Tstore makes it hard to just reup the same mod so here is a new version


### V0.3.6
* Added the ability to split up the items sold to knarr when you have more than 1 item in your inventory.

### V0.3.7
* Fixed item sync with servers not happening in situations
* Increased knarrs range of spawn (for older worlds using genloc)
* Fixed SOME FPS loss reported while knarr exists in world


### V0.3.8
* Address reports of lag

### V0.3.9
* Fixed a NRE (red text spam) report when using knarr on servers and disvovering his location

### V0.4.0
* Fixed sync issue with player inventory items for sale when YML wasnt given to player prior to join, Added some error handling around this please open a git issue if anything persists

### V0.4.1
* Fixed github reported issue, where if player sold items via split panel wrong volume was being given to knarr this is now fixed


### V0.4.2
* Fix github reported issue, where if Knarr was installed through r2/thunderstore he would fail to resolve his config file this was due to some old amateur code I wrote.


### V0.4.3
* Update Dependencies for latest release version. Update Assets to match unity version

</details>

### V0.4.4
* Fix shader issue with knarrs tabs after recent game patch
