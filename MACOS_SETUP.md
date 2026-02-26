# 🍎 nicodemouse: macOS Setup Guide

To ensure `nicodemouse` functions correctly on macOS (especially for capturing and injecting mouse/keyboard input), you must follow these installation and permission steps.

## 💾 Installation

1.  **Download** the macOS build.
2.  **Open** the `.dmg` or setup program.
3.  **Drag** the `nicodemouse` icon into your **Applications** folder.
4.  Launch `nicodemouse` from your Applications folder.

---

## 🛡️ Critical System Permissions

macOS requires explicit user consent for apps to monitor input or access disk data. `nicodemouse` needs the following:

### 1. Accessibility
**Why?** This allows `nicodemouse` to read your mouse and keyboard input locally and "inject" it into the remote computer when in remote mode.

**Steps (macOS 13 Ventures & Later):**
1. Open **System Settings**.
2. Navigate to **Privacy & Security** > **Accessibility**.
3. Find `nicodemouse` in the list and toggle it **ON**.
4. If prompted, restart `nicodemouse`.

> [!WARNING]
> Do not uncheck this option while `nicodemouse` is actively controlling a remote machine, as it may temporarily lock your input handling. Exit the app before changing this setting.

### 2. Full Disk Access
**Why?** Required for seamless **Clipboard Synchronization** and future **File Transfer** features. This allows the app to securely write temporary data needed for these streams.

**Steps (macOS 13 Ventures & Later):**
1. Return to **Privacy & Security**.
2. Select **Full Disk Access**.
3. Find `nicodemouse` in the list and toggle it **ON**.

---

## 🔍 Troubleshooting Permissions

If the app still cannot capture input even though settings look correct:

1.  **Quit** `nicodemouse` completely.
2.  Go to the **Accessibility** settings.
3.  Select `nicodemouse` and click the **minus (-)** button to remove it entirely.
4.  Click the **plus (+)** button and manually add `nicodemouse` from your Applications folder.
5.  Restart your Mac.

### 💡 Expert Tip
If the system permission database becomes corrupted, you can reset all accessibility permissions using the Terminal:
```bash
sudo tccutil reset Accessibility
```
*Note: This will reset permissions for ALL apps.*

---
*Created with ❤️ by rodrigod3v*
