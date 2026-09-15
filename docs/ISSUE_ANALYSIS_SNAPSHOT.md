# Open Issues Analysis Snapshot

*Snapshot taken on: September 16, 2026*

This document ranks the currently open issues in the `emoacht/Monitorian` repository based on their community impact, determined by the sum of comments and user reactions.

| Rank | Issue | Title | Comments | Reactions | Total Impact Score |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **1** | [#115](https://github.com/emoacht/Monitorian/issues/115) | Restore brightness of external display which is reset to 100% after restart/resume | 24 | 4 | **28** |
| **2** | [#190](https://github.com/emoacht/Monitorian/issues/190) | Incremental UI Refresh | 21 | 0 | **21** |
| **3** | [#637](https://github.com/emoacht/Monitorian/issues/637) | Mouse wheel over notification area icon | 11 | 2 | **13** |
| **4** | [#762](https://github.com/emoacht/Monitorian/issues/762) | Brightness levels restore themselves to the original value | 9 | 0 | **9** |
| **5** | [#655](https://github.com/emoacht/Monitorian/issues/655) | Availability of command-line options to get/set brightness, contrast or input | 4 | 4 | **8** |
| **6** | [#756](https://github.com/emoacht/Monitorian/issues/756) | HDR Monitors don't adjust brightness independently. | 8 | 0 | **8** |
| **7** | [#427](https://github.com/emoacht/Monitorian/issues/427) | Redefine unison function | 7 | 0 | **7** |
| **8** | [#563](https://github.com/emoacht/Monitorian/issues/563) | Conditional / Time / Key commands | 4 | 0 | **4** |
| **9** | [#426](https://github.com/emoacht/Monitorian/issues/426) | Inability to get auto-brightness by ambient light sensor on some devices (Surface Pro 8) | 1 | 1 | **2** |
| **10** | [#705](https://github.com/emoacht/Monitorian/issues/705) | SDR content brightness under HDR | 1 | 1 | **2** |

## Top 3 Issues Deep Dive

### 1. #115: Restore brightness of external display which is reset to 100% after restart/resume
**Context:** Monitors occasionally reset to 100% brightness (or default values) after a PC wakes from sleep or screen timeout.
**Current State:** The author added an experimental `/restore hard` command-line argument. Users report it works for sleep, but fails when the screen simply times out (Display Off) and turns back on.

### 2. #190: Incremental UI Refresh
**Context:** An umbrella issue for modernizing the UI away from Metro design towards Windows 11 aesthetics, and exposing hidden CLI features to the UI.
**Current State:** The author has incrementally added features (like rounded corners and new sliders). The biggest remaining gap requested in the issue description is: "Additional settings: Some features have been already implemented but not shown in the menu. Making them more accessible would be helpful for the majority of users."

### 3. #637: Mouse wheel over notification area icon
**Context:** Users want to scroll over the tray icon to change brightness (similar to volume control).
**Current State:** The author added this experimentally via the `/iconwheel` command-line argument. A recent popular request in the comments asks to show the brightness percentage amount when scrolling, similar to Windows 11's volume OSD.
