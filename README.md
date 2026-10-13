# Better UI

A code mod for the video game Spaceflight Simulator (SFS) that improves the in-game user interface (UI).
[![Demonstration of Better UI (Mod Menu)](https://www.youtube.com/watch?v=JpWrF3RdsOQ)](https://www.youtube.com/watch?v=JpWrF3RdsOQ)


## Features

### Menu Wallpaper Customisation
Replace the main menu background with a PNG, JPG, GIF, or MP4.

1. Main menu -> Change Wallpaper (under Settings)
   - For non-Windows users, you must manually set the wallpaper path in `BetterUI/wallpaper.txt`
2. Main menu -> Reset Wallpaper (optional)

Videos loop automatically.
![menu wallpaper example usage, a blue archive wallpaper found by Luna Tomato.png](./assets/menu_wallpaper.png)

### Better Mod Menu
- Filter mods by type (code mod, asset pack, texture pack, custom solar system)
  - filter settings are saved to disk
![filters example](./assets/filters.png)
- Detailed report when mods finished loading, with number of loaded mods for each type
![img.png](./assets/report.png)
- Requests confirmation to enter world/build scene if you try to enter before all mods are loaded
![img.png](./assets/confirmation.png)
- Planet pack icons
  - Simply add a new `"icon"` field to your ImportSettings.txt, and put an image file in the same folder as ImportSettings.txt and reference it in the file, including the extension. 
![planet pack icons example](./assets/planet_pack_icons_example.png)
## Future Roadmap
[ROADMAP.md](ROADMAP.md)

## Requirements and Dependencies
- SFS (PC Version)
  - v1.6.00.16 and newer

## Installation

[Code Mod Installation Guide | SFS Modding Guide](https://kojamori.github.io/SFS-Modding-Guide/getting-started/installation/code-mods)

## Social Media

### Forum Post

https://sfsforum.com/index.php?threads/bettermodmenu-filter-mods-by-type-and-more.20179/

### SFS Modding Guide
https://kojamori.github.io/SFS-Modding-Guide/

### Discord

[Join the discord server here!](https://discord.gg/QHEmcehAe9)

# Authorship & Credits

* <img src="assets/kojamori_pfp.png" width="100" style="vertical-align: middle;" alt="kojamori's avatar"></img>  **kojamori** - Lead developer and maintainer of the mod.


* <img src="assets/luna_tomato_pfp.png" width="100" style="vertical-align: middle;" alt="Luna Tomato's avatar"></img> **Luna Tomato** - Implemented the menu wallpaper customisation system (Integrated with the author's direct permission).
  * [GitHub Profile](https://github.com) | [YouTube Channel](https://www.youtube.com/channel/UCv2v5wErevqbt4HJkFUhe2A) | [Spaceflight Simulator Thailand Discord](https://discord.gg/7g6v8Z9k)

# License

See the [LICENSE](LICENSE) file for details.
