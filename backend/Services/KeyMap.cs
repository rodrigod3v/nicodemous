using SharpHook.Native;
using SharpHook.Data;
using System.Collections.Generic;

namespace Nicodemous.Backend.Services;

/// <summary>
/// Maps between SharpHook KeyCode and Nicodemous KeyID (a stable, platform-agnostic ID).
/// Inspired by Input Leap's key mapping approach (protocol_types.h / KeyID).
/// KeyID values 0x0020-0x00FF are Unicode codepoints for printable chars.
/// Values 0xEF00-0xEFFF are used for special/function keys.
/// Modifier masks are separate (see KeyModifiers).
/// </summary>
public static class KeyMap
{
    // Modifier bit masks — sent alongside key events
    public const ushort ModShift   = 0x0001;
    public const ushort ModControl = 0x0002;
    public const ushort ModAlt     = 0x0004;
    public const ushort ModMeta    = 0x0008; // Win / Cmd

    // Special key IDs (0xEF00 range, mirrors X11 Keysym convention used by Input Leap)
    public const ushort KeyBackspace = 0xEF08;
    public const ushort KeyTab       = 0xEF09;
    public const ushort KeyReturn    = 0xEF0D;
    public const ushort KeyEscape    = 0xEF1B;
    public const ushort KeyDelete    = 0xEFFF;
    public const ushort KeyHome      = 0xEF50;
    public const ushort KeyLeft      = 0xEF51;
    public const ushort KeyUp        = 0xEF52;
    public const ushort KeyRight     = 0xEF53;
    public const ushort KeyDown      = 0xEF54;
    public const ushort KeyPageUp    = 0xEF55;
    public const ushort KeyPageDown  = 0xEF56;
    public const ushort KeyEnd       = 0xEF57;
    public const ushort KeyInsert    = 0xEF63;
    public const ushort KeyF1        = 0xEFBE;
    public const ushort KeyF2        = 0xEFBF;
    public const ushort KeyF3        = 0xEFC0;
    public const ushort KeyF4        = 0xEFC1;
    public const ushort KeyF5        = 0xEFC2;
    public const ushort KeyF6        = 0xEFC3;
    public const ushort KeyF7        = 0xEFC4;
    public const ushort KeyF8        = 0xEFC5;
    public const ushort KeyF9        = 0xEFC6;
    public const ushort KeyF10       = 0xEFC7;
    public const ushort KeyF11       = 0xEFC8;
    public const ushort KeyF12       = 0xEFC9;
    public const ushort KeyCapsLock  = 0xEFE5;
    public const ushort KeyShiftL    = 0xEFE1;
    public const ushort KeyShiftR    = 0xEFE2;
    public const ushort KeyControlL  = 0xEFE3;
    public const ushort KeyControlR  = 0xEFE4;
    public const ushort KeyAltL      = 0xEFE9;
    public const ushort KeyAltR      = 0xEFEA;
    public const ushort KeyMetaL     = 0xEFEB;
    public const ushort KeyMetaR     = 0xEFEC;
    public const ushort KeyNumLock   = 0xEFAF;
    public const ushort KeyScrollLock = 0xEFF4;
    public const ushort KeyPrintScr  = 0xEF61;

    // Map: SharpHook KeyCode → Nicodemous KeyID
    public static readonly Dictionary<KeyCode, ushort> KeyCodeToId = new()
    {
        // --- Letters (Unicode codepoints for lowercase) ---
        { KeyCode.VcA, 0x0061 }, { KeyCode.VcB, 0x0062 }, { KeyCode.VcC, 0x0063 },
        { KeyCode.VcD, 0x0064 }, { KeyCode.VcE, 0x0065 }, { KeyCode.VcF, 0x0066 },
        { KeyCode.VcG, 0x0067 }, { KeyCode.VcH, 0x0068 }, { KeyCode.VcI, 0x0069 },
        { KeyCode.VcJ, 0x006A }, { KeyCode.VcK, 0x006B }, { KeyCode.VcL, 0x006C },
        { KeyCode.VcM, 0x006D }, { KeyCode.VcN, 0x006E }, { KeyCode.VcO, 0x006F },
        { KeyCode.VcP, 0x0070 }, { KeyCode.VcQ, 0x0071 }, { KeyCode.VcR, 0x0072 },
        { KeyCode.VcS, 0x0073 }, { KeyCode.VcT, 0x0074 }, { KeyCode.VcU, 0x0075 },
        { KeyCode.VcV, 0x0076 }, { KeyCode.VcW, 0x0077 }, { KeyCode.VcX, 0x0078 },
        { KeyCode.VcY, 0x0079 }, { KeyCode.VcZ, 0x007A },

        // --- Number row ---
        { KeyCode.Vc0, 0x0030 }, { KeyCode.Vc1, 0x0031 }, { KeyCode.Vc2, 0x0032 },
        { KeyCode.Vc3, 0x0033 }, { KeyCode.Vc4, 0x0034 }, { KeyCode.Vc5, 0x0035 },
        { KeyCode.Vc6, 0x0036 }, { KeyCode.Vc7, 0x0037 }, { KeyCode.Vc8, 0x0038 },
        { KeyCode.Vc9, 0x0039 },

        // --- Punctuation ---
        { KeyCode.VcMinus,        0x002D }, // -
        { KeyCode.VcEquals,       0x003D }, // =
        { KeyCode.VcOpenBracket,  0x005B }, // [
        { KeyCode.VcCloseBracket, 0x005D }, // ]
        { KeyCode.VcBackslash,    0x005C }, // \
        { KeyCode.VcSemicolon,    0x003B }, // ;
        { KeyCode.VcQuote,        0x0027 }, // '
        { KeyCode.VcBackQuote,    0x0060 }, // `
        { KeyCode.VcComma,        0x002C }, // ,
        { KeyCode.VcPeriod,       0x002E }, // .
        { KeyCode.VcSlash,        0x002F }, // /
        { KeyCode.VcSpace,        0x0020 }, // Space

        // --- Control keys ---
        { KeyCode.VcBackspace, KeyBackspace },
        { KeyCode.VcTab,       KeyTab       },
        { KeyCode.VcEnter,     KeyReturn    },
        { KeyCode.VcEscape,    KeyEscape    },
        { KeyCode.VcDelete,    KeyDelete    },
        { KeyCode.VcInsert,    KeyInsert    },
        { KeyCode.VcHome,      KeyHome      },
        { KeyCode.VcEnd,       KeyEnd       },
        { KeyCode.VcPageUp,    KeyPageUp    },
        { KeyCode.VcPageDown,  KeyPageDown  },

        // --- Arrow keys ---
        { KeyCode.VcLeft,  KeyLeft  },
        { KeyCode.VcUp,    KeyUp    },
        { KeyCode.VcRight, KeyRight },
        { KeyCode.VcDown,  KeyDown  },

        // --- Function keys ---
        { KeyCode.VcF1,  KeyF1  }, { KeyCode.VcF2,  KeyF2  }, { KeyCode.VcF3,  KeyF3  },
        { KeyCode.VcF4,  KeyF4  }, { KeyCode.VcF5,  KeyF5  }, { KeyCode.VcF6,  KeyF6  },
        { KeyCode.VcF7,  KeyF7  }, { KeyCode.VcF8,  KeyF8  }, { KeyCode.VcF9,  KeyF9  },
        { KeyCode.VcF10, KeyF10 }, { KeyCode.VcF11, KeyF11 }, { KeyCode.VcF12, KeyF12 },

        // --- Modifier keys ---
        { KeyCode.VcLeftShift,   KeyShiftL   }, { KeyCode.VcRightShift,   KeyShiftR   },
        { KeyCode.VcLeftControl, KeyControlL }, { KeyCode.VcRightControl, KeyControlR },
        { KeyCode.VcLeftAlt,     KeyAltL     }, { KeyCode.VcRightAlt,     KeyAltR     },
        { KeyCode.VcLeftMeta,    KeyMetaL    }, { KeyCode.VcRightMeta,    KeyMetaR    },

        // --- Lock keys ---
        { KeyCode.VcCapsLock,   KeyCapsLock   },
        { KeyCode.VcNumLock,    KeyNumLock    },
        { KeyCode.VcScrollLock, KeyScrollLock },
        { KeyCode.VcPrintScreen,KeyPrintScr   },

        // --- Numpad ---
        { KeyCode.VcNumPad0, 0x0030 }, { KeyCode.VcNumPad1, 0x0031 },
        { KeyCode.VcNumPad2, 0x0032 }, { KeyCode.VcNumPad3, 0x0033 },
        { KeyCode.VcNumPad4, 0x0034 }, { KeyCode.VcNumPad5, 0x0035 },
        { KeyCode.VcNumPad6, 0x0036 }, { KeyCode.VcNumPad7, 0x0037 },
        { KeyCode.VcNumPad8, 0x0038 }, { KeyCode.VcNumPad9, 0x0039 },
        { KeyCode.VcNumPadDivide,   0x002F },
        { KeyCode.VcNumPadMultiply, 0x002A },
        { KeyCode.VcNumPadSubtract, 0x002D },
        { KeyCode.VcNumPadAdd,      0x002B },
        { KeyCode.VcNumPadEnter,    KeyReturn },
        { KeyCode.VcNumPadDecimal,  0x002E  },
    };

    // Reverse map: KeyID → SharpHook KeyCode (for injection on receiver side)
    public static readonly Dictionary<ushort, KeyCode> IdToKeyCode;

    // These modifier keycodes should be treated as modifier-only; don't add to main modifier mask
    public static readonly HashSet<KeyCode> ModifierKeyCodes = new()
    {
        KeyCode.VcLeftShift,   KeyCode.VcRightShift,
        KeyCode.VcLeftControl, KeyCode.VcRightControl,
        KeyCode.VcLeftAlt,     KeyCode.VcRightAlt,
        KeyCode.VcLeftMeta,    KeyCode.VcRightMeta,
        KeyCode.VcCapsLock,    KeyCode.VcNumLock, KeyCode.VcScrollLock
    };

    // Currently-held modifier keys (updated by InputService)
    public static ushort GetModifierMask(
        bool shiftDown, bool ctrlDown, bool altDown, bool metaDown)
    {
        ushort mask = 0;
        if (shiftDown) mask |= ModShift;
        if (ctrlDown)  mask |= ModControl;
        if (altDown)   mask |= ModAlt;
        if (metaDown)  mask |= ModMeta;
        return mask;
    }

    static KeyMap()
    {
        IdToKeyCode = new Dictionary<ushort, KeyCode>();
        foreach (var kv in KeyCodeToId)
        {
            // Don't overwrite — prefer the first (canonical) mapping
            IdToKeyCode.TryAdd(kv.Value, kv.Key);
        }
    }
}
