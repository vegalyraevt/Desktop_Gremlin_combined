# Desktop Gremlin - Combined Edition

<img width="925" height="436" alt="image" src="https://github.com/user-attachments/assets/7f3f1631-b2d2-4b8e-adc4-b287dd81d784" />

> **A fork of [KurtVelasco/Desktop_Gremlin](https://github.com/KurtVelasco/Desktop_Gremlin)** - All characters combined into one application with additional features.

---

## ✨ What's Different in This Version?

### 🎯 All Characters in One Application
Instead of downloading separate executables for each character, this version includes **all characters** in a single application.

### 🔄 Character Selector
- **Right-click** the taskbar icon to open the character selector
- Switch between any character without restarting the application
- Each character shows their available features (Idle, Running, Walking, Emotes, Dancing, Companion support, etc.)

### 💃 Dance Animations from Jukebox
Dance animations for several Uma Musume characters were pulled from the original creator's **Jukebox Mascot** program and integrated into this combined version. Characters with dance animations will automatically dance when music is detected.

### 🎵 Audio Detection
- Uses the **Windows Core Audio API** (`IAudioMeterInformation`) to detect audio levels
- Uses **Windows Media Session API** (`GlobalSystemMediaTransportControlsSessionManager`) to detect what song is playing
- Detects when you're playing Uma Musume music from Spotify, YouTube, or other media players
- **Special interactions** occur when Uma songs are detected
- Umas with dance animations will automatically dance when hearing music

### 👥 Companion System
Some characters can spawn a companion that follows them around! Companions are smaller versions of other characters.

### 🖼️ Icon Selection
Each character has multiple icon variations in the `Icons/` folder. Icons are used for:
- The taskbar/system tray icon
- Victory screen displays (random icon selected)

Available icons include regular sprites and plush variants for most characters.

---

## 🐴 Character List & Features

| Character | Movement | Animations | Companion | Notes |
|-----------|----------|------------|-----------|-------|
| **Meisho Doto** | Full 8-direction | Idle, Walk, Run, Emotes, Intro/Outro, Sleep | Opera | Main character with full feature set |
| **Manhattan Cafe** | Full 8-direction | Idle, Walk, Run, Emotes, Intro/Outro, Sleep | Agnes Tachyon | New in v3.1! Full feature set |
| **Agnes Tachyon** | Full 8-direction | Idle, Walk, Run, Emotes, Dance, Sleep | Manhattan Cafe | Has dance animation |
| **Gold Ship** | Full 8-direction | Idle, Walk, Run, Intro/Outro | — | Walk-focused character |
| **Oguri Cap** | Full 8-direction | Idle, Run, Dance, Intro/Outro | Opera | Has dance animation |
| **Rice Shower** | Full 8-direction | Idle, Walk, Run, Emotes, Dance, Sleep | — | Has dance animation |
| **Opera** | Full 8-direction | Idle, Run, Intro/Outro, Effects | Doto or Oguri (random) | Can spawn different companions |
| **Mambo** | Full 8-direction | Idle, Walk, Run, Emotes, Intro/Outro, Sleep | Agnes Tachyon | Full feature set |
| **Mambo Farmer** | Stationary | Click reactions, Emotes | — | Doesn't move, reacts to clicks only |
| **Bakushin** | Dance only | Dance, Intro | — | Dance-only character |
| **Pasa** | Dance only | Dance, Intro | — | Dance-only character |
| **Teio** | Dance only | Dance, Intro | — | Dance-only character |
| **Exusiai** (Arknights) | Left/Right only | Walk, Intro/Outro, Sleep | — | Moves along taskbar area |
| **Exusiai Gun** | Left/Right only | Firing, Reload, Emotes, Effects | — | Has ammo system & keyboard controls |
| **Koyuki** (Blue Archive) | Full 8-direction | Idle, Run, Emotes, Intro/Outro, Sleep | — | No walk animations |

---

## ⚙️ Settings Menu

Access via **right-click menu → Settings**. All settings are saved automatically.

### Dance Settings
| Setting | Description |
|---------|-------------|
| **Auto Dance** | Enable/disable automatic dancing when music is detected |
| **Auto Stop** | Automatically stop dancing when music stops |
| **Music Notes** | Show floating music notes while dancing |
| **Dynamic Chance** | Dance chance increases the longer music plays |
| **Dance Chance %** | Base probability to start dancing (default: 30%) |
| **Stop Chance %** | Probability to stop dancing each check (default: 40%) |
| **Check Interval** | How often to check for music (seconds) |
| **Dance Cooldown** | Minimum time between dances (seconds) |
| **Uma Easter Egg** | Enable special interactions for Uma songs |

### Audio Detection Settings
| Setting | Description |
|---------|-------------|
| **Use Media Session** | Detect song info from Windows Media Session |
| **Use Volume Detection** | Detect audio using system volume levels |
| **Audio Threshold %** | Minimum audio level to trigger detection |
| **Audio Sensitivity** | Multiplier for audio detection sensitivity |
| **Sound Volume** | Volume for character sound effects (0-100%) |

### Behavior Settings
| Setting | Description |
|---------|-------------|
| **Allow Randomness** | Enable random idle behaviors |
| **Follow Cursor** | Gremlin follows your mouse cursor |
| **Min/Max Interval** | Time range for random behaviors |
| **Enable Companion** | Spawn a companion character (if supported) |

---

## 🖼️ Icons

The `Icons/` folder contains `.ico` files for each character with multiple variants:

| Character | Available Icons |
|-----------|----------------|
| Manhattan Cafe | cafe1, cafe2, cafe3, cafe_plush |
| Agnes Tachyon | Tach1, Tach2, Tach3, Tach_plush |
| Gold Ship | gold1, gold2, gold3, gold4, Gold_plush1, Gold_plush2 |
| Oguri Cap | Oguri, Oguri_plush1, Oguri_plush2 |
| Rice Shower | Rice, Rice_plush |
| Meisho Doto | Doto, Doto_plush |
| Mambo | Mambo, Mambo2, mambo_plush, mambo_plush2 |
| Opera | opera, Opera_Plush |
| Exusiai | Exusiai |
| Koyuki | Koyuki |

---

## 📥 Download

| Version | Download |
|---------|----------|
| Combined Edition (Windows) | [See Releases](https://github.com/vegalyraevt/Desktop_Gremlin_combined/releases) |

> ⚠️ **Note:** This combined version is **Windows only**. There is no Linux version of this fork.
> For Linux, see the [original Linux port](https://github.com/iluvgirlswithglasses/linux-desktop-gremlin) by [@iluvgirlswithglasses](https://github.com/iluvgirlswithglasses) (Python, original characters only).

---

## 🎮 Controls & Interactions

| Action | What Happens |
|--------|-------------|
| **Left-click + Drag** | Pick up and move the gremlin |
| **Hover** | Gremlin reacts to your cursor |
| **Double-click** | Trigger emote animation |
| **Right-click taskbar icon** | Open context menu (Character Select, Settings, Debug, Exit) |

---

## ⚙️ Configuration (config.txt)

```ini
SPRITE_SCALE = 1.0          # Size of the gremlin (0.1 = tiny, 2.0 = large)
FOLLOW_RADIUS = 200         # How close cursor must be to trigger follow
FORCE_FAKE_TRANSPARENT = true   # Fix for translucent/opaque backgrounds
ALLOW_COLOR_HOTSPOT = true      # Show interaction hotspots (for debugging)
SHOW_TASKBAR = true             # Show icon in Windows taskbar
```

<img width="527" height="385" alt="Hotspot visualization" src="https://github.com/user-attachments/assets/45434679-7b5b-49c1-9055-a753252e2e86" />

---

## 🛠️ Troubleshooting

| Problem | Solution |
|---------|----------|
| Gremlin too big or off-screen | Lower `SPRITE_SCALE` in config.txt (try 0.5 or 0.1) |
| Gremlin not following mouse | Lower `FOLLOW_RADIUS` in config.txt |
| No animation while dragging | Windows Settings → Performance Options → Enable "Animate controls and elements inside windows" |
| Transparency not working | Settings → Personalization → Colors → Transparency Effects → **On** |
| Sprites look wrong | Delete config.txt and restart (a fresh one will be created) |
| Character not switching | Make sure the character folder exists in SpriteSheet/Gremlins/ |
| Music not detected | Check Settings → enable "Use Media Session" and "Use Volume Detection" |

---

## 📁 Folder Structure

```
DesktopGremlin/
├── DesktopGremlin.exe      # Main application
├── config.txt              # User settings
├── Icons/                  # Character icons (.ico files)
│   ├── Doto.ico
│   ├── Doto_plush.ico
│   └── ...
├── Sounds/                 # Character voice lines & SFX
│   ├── Doto/
│   │   ├── intro.wav
│   │   ├── grab.wav
│   │   └── ...
│   ├── Agnes Tachyon/
│   └── ...
└── SpriteSheet/
    ├── Gremlins/           # Character sprites
    │   ├── Doto/
    │   │   ├── config.txt
    │   │   ├── Actions/
    │   │   ├── Walk/
    │   │   └── Run/
    │   ├── Agnes Tachyon/
    │   │   ├── config.txt
    │   │   ├── dance.png    # Dance animation sprite sheet
    │   │   └── ...
    │   └── ...
    └── System/             # UI sprites (music notes, etc.)
```

---

## 🙏 Credits

- **Original Creator:** [KurtVelasco](https://github.com/KurtVelasco) - All sprite work, original codebase, character designs, and Jukebox Mascot dance sprites
- **Uma Musume Sprites:** Extracted via [UmaViewer](https://github.com/katboi01/UmaViewer)
- **Combined Edition:** [vegalyraevt](https://github.com/vegalyraevt) - Multi-character support, audio detection, companion system, settings UI

---

## ❓ FAQ

| Question | Answer |
|----------|--------|
| Can I add my own characters? | Yes! Create a folder in `SpriteSheet/Gremlins/` with the required sprites and a config.txt. You may need to add padding in the code for proper sprite sizing. |
| Does this work on Mac? | No, Windows only (WPF application) |
| Does this work on Linux? | No, this fork is Windows only. See the original Linux port linked above. |
| Why does my antivirus flag it? | False positive - the app uses Windows APIs to detect playing media. You can review the source code. |
| Where are the Uma Musume sprites from? | UmaViewer - a tool for extracting game assets |
| Where are the dance animations from? | The original creator's Jukebox Mascot program |

---

## 📜 License

This project is a fork of [Desktop_Gremlin](https://github.com/KurtVelasco/Desktop_Gremlin). Please support the original creator!