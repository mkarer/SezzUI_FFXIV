<div id="top"></div>


<!-- PROJECT SHIELDS -->
[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]
[![LinkedIn][linkedin-shield]][linkedin-url]
[![Ko-Fi][kofi-shield]][kofi-url]



<!-- PROJECT LOGO -->
<br />
<div align="center">
  <a href="https://github.com/mkarer/SezzUI_FFXIV">
    <img src="Assets/docs/logo.png" alt="Logo" id="logo" width="300" height="300" srcset="Assets/docs/logo@2x.png 2x, Assets/docs/logo.png 1x">
  </a>

  <h3 align="center">SezzUI for Final Fantasy XIV</h3>

  <p align="center">
    Minimalistic UI Additions & Tweaks
  </p>

  <p align="center">
    <a href="https://github.com/mkarer/SezzUI_FFXIV"><strong>Explore the docs »</strong></a>
    <br />
	<sub><sup>(Spoiler alert: There is no documentation!)</sup></sub>
    <br />
    <a href="https://github.com/mkarer/SezzUI_FFXIV/issues">Report Bug</a>
    ·
    <a href="https://github.com/mkarer/SezzUI_FFXIV/issues">Request Feature</a>
  </p>
</div>



<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li>
      <a href="#about-the-project">About The Project</a>
    </li>
    <li><a href="#features">Features</a></li>
    <li><a href="#faq">FAQ</a></li>
    <li>
      <a href="#getting-started">Getting Started</a>
      <ul>
        <li><a href="#prerequisites">Prerequisites</a></li>
        <li><a href="#installation">Installation</a></li>
      </ul>
    </li>
    <li><a href="#usage">Usage</a></li>
    <li><a href="#contributing">Contributing</a></li>
    <li><a href="#contact">Contact</a></li>
    <li><a href="#license">License</a></li>
    <li><a href="#acknowledgments">Acknowledgments</a></li>
    <li><a href="#disclaimer">Disclaimer</a></li>
  </ol>
</details>



<!-- ABOUT THE PROJECT -->

## About The Project

This is aimed to be a port of my World of Wacraft UI that I've been using since 2008, you can find that one at [https://github.com/mkarer/ElvUI_SezzUI](https://github.com/mkarer/ElvUI_SezzUI/) soon&trade; (aka whenever I find time to clean up the code).

SezzUI won't give you any unfair advantages. Neither does it automate the game without direct interaction, nor will it ever do. Everything this plugin offers is either a clientside UI improvement or can be already achieved by the default client (just not that convenient).

**<span style="color:red">WARNING: At the current state of the plugin I would strongly advice against using it.</span>**

While it should be stable I cannot guarantee that it won't crash. I'm using this project to get into C#, which means major changes and rewrites are expected to happen every now and then as I learn. Any suggestions on improving the code are highly appreciated.

<p align="right">(<a href="#top">back to top</a>)</p>



<!-- Features -->
## Features

* UI Improvements

	* Define areas on the screen that can be used to show/hide default UI elements on mouseover (action bars, main menu, etc.).
	* Hide action bar lock.
	* Action bar paging - swap the main action bar page while holding down a modifier key.
	* Action bar button row reordering, inverts row order.

* Job HUD

	* Timers and/or status bars for buffs and debuffs.
	* Cooldowns, proccs, etc.
	* Alerts similar to World of Warcraft's SpellActivationOverlay/WeakAuras.
		* Images are not included, as I currently use those from Blizzard, you better disable the alerts unless you like placeholder rectangles.
	* Super minimalistic, totally not configurable yet (use [XIVAuras](https://github.com/lichie567/XIVAuras)).

* Cooldown Bars

	* Bars. For cooldowns.
	* Pulse animation when a cooldown finishes (similar to Doom_CooldownPulse).
	* Also, there's no configuration yet for the list of enabled spells.

* Plugin Menu

	* Buttons that execute a command when clicked. Similar to [QoLBar](https://github.com/UnknownX7/QoLBar), but no additional features or logic besides the (optional) coloring based on another plugins property. They are pretty though.


<p align="right">(<a href="#top">back to top</a>)</p>



<!-- FAQ -->
## FAQ

See the [open issues](https://github.com/mkarer/SezzUI_FFXIV/issues) for a full list of proposed features (and known issues) *after* reading the whole FAQ.

### 1. How do I.../I want to.../Why doesn't... ?

I told you not to use this plugin, didn't I? ;) As already stated before this is a work in progress, If you think something isn't working like \*I\* wanted it to work and it isn't totally obvious feel free to [report a bug](https://github.com/mkarer/SezzUI_FFXIV/issues) *after* reading about known issues first.

### 2. Feature Requests

My main goal is to get all the features in that are important to me personally. Requesting features is fine, but don't expect anything. (For reference: My World of Warcraft UI still hasn't got settings for all modules and exists for more than a decade now.)

### 3. Known Issues

* Profiles/Exporting/Importing: The current implemenation of resetting configurations causes issues with how I setup the plugin and I haven't gotten time to change this. Switching to another profile/resetting a profile and reloading the plugin afterwards might be fine, but I don't know - I don't use profiles.

* Missing/Useless/Nonworking configuration items: Yep.

### 4. See #1


<p align="right">(<a href="#top">back to top</a>)</p>



<!-- GETTING STARTED -->
## Getting Started

SezzUI is a plugin for Dalamud, in order to use it you need to install XIVLauncher first. Dalamud should be enabled by default (if it is not you can enable it in the launcher settings).

XIVLauncher is available at https://goatcorp.github.io/, the source code is on GitHub: https://github.com/goatcorp/FFXIVQuickLauncher

### Prerequisites

* Read the <a href="#faq">FAQ</a>
* Install [XIVLauncher](https://goatcorp.github.io/)

### Installation

1. Open Dalamud settings after launching the game by typing `/xlsettings` in chat (or using the fancy new menu at the top left of your screen when you're at the character selection).

2. Go to the "Experimental" tab and add the following URL to "Custom Plugin Repositories" (don't forget to click the small plus button on the right side):
   ```
   https://ffxiv.sezz.at/repo.json
   ```

3. Optional: Enable "Plugin Testing Builds" to try the lastest build (if there is one).

4. Save and Close.

5. Open Dalamud's plugin installer using `/xlplugins` - you should now be able to install and update SezzUI from there (it should be listed in the UI category).

<p align="right">(<a href="#top">back to top</a>)</p>



<!-- USAGE EXAMPLES -->
## Usage

The command `/sezzui` opens the configuration window. The first thing you want do is to disable everything, because most of the default configuration is how I use the plugin.

Now go ahead and read the <a href="#faq#">FAQ</a> (seriously).

_For more information, please refer to the [Documentation](https://github.com/mkarer/SezzUI_FFXIV)._

<p align="right">(<a href="#top">back to top</a>)</p>



<!-- CONTRIBUTING -->
## Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

If you have a suggestion that would make this better, please fork the repo and create a pull request. You can also simply open an issue with the tag "enhancement".
Don't forget to give the project a star! Thanks again!

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

<p align="right">(<a href="#top">back to top</a>)</p>



<!-- CONTACT -->
## Contact

Martin Karer - [@sezz](https://twitter.com/sezz) - [https://sezz.at](https://sezz.at) - Discord: Sezz#3415

Please refrain from trying to contact me directly via anything else than email unless you don't expect an answer within the next 12 months. I'm sure you can figure out the address if your enquiry is of major importance.

<p align="right">(<a href="#top">back to top</a>)</p>



<!-- LICENSE -->
## License

Distributed under the GNU AGPL-3.0 License. See `LICENSE` for more information.

<p align="right">(<a href="#top">back to top</a>)</p>



<!-- ACKNOWLEDGMENTS -->
## Acknowledgments

* [FFXIVQuickLauncher](https://github.com/goatcorp/FFXIVQuickLauncher)
* [Dalamud](https://github.com/goatcorp/Dalamud)
* [DelvUI](https://github.com/DelvUI/DelvUI)
* [Flaticon](https://www.flaticon.com)

Most of the base framework and configuration code is taken from DelvUI which saved me a tremendous amount of time to get things going. Additional credits, detailed image sources, etc. are listed in the plugin.

<p align="right">(<a href="#top">back to top</a>)</p>



<!-- DISCLAIMER -->
## Disclaimer

FINAL FANTASY is a registered trademark of Square Enix Holdings Co., Ltd. FINAL FANTASY XIV © 2010-2022 SQUARE ENIX CO., LTD. All Rights Reserved. I'm not affiliated with SQUARE ENIX CO., LTD. in any way.



<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->
[contributors-shield]: https://img.shields.io/github/contributors/mkarer/SezzUI_FFXIV.svg?style=flat-square
[contributors-url]: https://github.com/mkarer/SezzUI_FFXIV/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/mkarer/SezzUI_FFXIV.svg?style=flat-square
[forks-url]: https://github.com/mkarer/SezzUI_FFXIV/network/members
[stars-shield]: https://img.shields.io/github/stars/mkarer/SezzUI_FFXIV.svg?style=flat-square
[stars-url]: https://github.com/mkarer/SezzUI_FFXIV/stargazers
[issues-shield]: https://img.shields.io/github/issues/mkarer/SezzUI_FFXIV.svg?style=flat-square
[issues-url]: https://github.com/mkarer/SezzUI_FFXIV/issues
[license-shield]: https://img.shields.io/github/license/mkarer/SezzUI_FFXIV.svg?style=flat-square
[license-url]: https://github.com/mkarer/SezzUI_FFXIV/blob/master/LICENSE.txt
[linkedin-shield]: https://img.shields.io/badge/-LinkedIn-black.svg?style=flat-square&logo=linkedin&colorB=555
[linkedin-url]: https://linkedin.com/in/sezz
[kofi-shield]: https://img.shields.io/badge/Ko--Fi-Donateblack.svg?style=flat-square&logo=kofi&colorB=555
[kofi-url]: https://ko-fi.com/sezzat
