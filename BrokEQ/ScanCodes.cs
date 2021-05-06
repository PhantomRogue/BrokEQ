using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BrokEQ
{

    public static class KeySetup
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct KeyboardInput
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MouseInput
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct HardwareInput
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }
        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)] public MouseInput mi;
            [FieldOffset(0)] public KeyboardInput ki;
            [FieldOffset(0)] public HardwareInput hi;
        }
        public struct Input
        {
            public int type;
            public InputUnion u;
        }

        [Flags]
        public enum InputType
        {
            Mouse = 0,
            Keyboard = 1,
            Hardware = 2
        }

        [Flags]
        public enum KeyEventF
        {
            KeyDown = 0x0000,
            ExtendedKey = 0x0001,
            KeyUp = 0x0002,
            Unicode = 0x0004,
            Scancode = 0x0008
        }

        [Flags]
        public enum MouseEventF
        {
            Absolute = 0x8000,
            HWheel = 0x01000,
            Move = 0x0001,
            MoveNoCoalesce = 0x2000,
            LeftDown = 0x0002,
            LeftUp = 0x0004,
            RightDown = 0x0008,
            RightUp = 0x0010,
            MiddleDown = 0x0020,
            MiddleUp = 0x0040,
            VirtualDesk = 0x4000,
            Wheel = 0x0800,
            XDown = 0x0080,
            XUp = 0x0100
        }
    }

    public static class ScanCodes
    {
        public enum Scancode
        {

            escape = 0x01,
            d1 = 0x02,
            d2 = 0x03,
            d3 = 0x04,
            d4 = 0x05,
            d5 = 0x06,
            d6 = 0x07,
            d7 = 0x08,
            d8 = 0x09,
            d9 = 0x0A,
            d10 = 0x0B,
            minus = 0x0C,
            equals = 0x0D,
            backspace = 0x0E,
            tab = 0x0F,
            q = 0x10,
            w = 0x11,
            e = 0x12,
            r = 0x13,
            t = 0x14,
            y = 0x15,
            u = 0x16,
            i = 0x17,
            o = 0x18,
            p = 0x19,
            bracketLeft = 0x1A,
            bracketRight = 0x1B,
            enter = 0x1C,
            controlLeft = 0x1D,
            a = 0x1E,
            s = 0x1F,
            d = 0x20,
            f = 0x21,
            g = 0x22,
            h = 0x23,
            j = 0x24,
            k = 0x25,
            l = 0x26,
            semicolon = 0x27,
            apostrophe = 0x28,
            grave = 0x29,
            shiftLeft = 0x2A,
            backslash = 0x2B,
            z = 0x2C,
            x = 0x2D,
            c = 0x2E,
            v = 0x2F,
            b = 0x30,
            n = 0x31,
            m = 0x32,
            comma = 0x33,
            preiod = 0x34,
            slash = 0x35,
            shiftright = 0x36,
            numpad_multiply = 0x37,
            altLeft = 0x38,
            space = 0x39,
            capsLock = 0x3A,
            f1 = 0x3B,
            f2 = 0x3C,
            f3 = 0x3D,
            f4 = 0x3E,
            f5 = 0x3F,
            f6 = 0x40,
            f7 = 0x41,
            f8 = 0x42,
            f9 = 0x43,
            f10 = 0x44,
            numLock = 0x45,
            scrollLock = 0x46,
            numpad_7 = 0x47,
            numpad_8 = 0x48,
            numpad_9 = 0x49,
            numpad_minus = 0x4A,
            numpad_4 = 0x4B,
            numpad_5 = 0x4C,
            numpad_6 = 0x4D,
            numpad_plus = 0x4E,
            numpad_1 = 0x4F,
            numpad_2 = 0x50,
            numpad_3 = 0x51,
            numpad_0 = 0x52,
            numpad_period = 0x53,
            alt_printScreen = 0x54, /* Alt + print screen. MapVirtualKeyEx( VK_SNAPSHOT, MAPVK_VK_TO_VEX, 0 ) returns scancode 0x54. */
            bracketAngle = 0x56, /* Key between the left shift and Z. */
            f11 = 0x57,
            f12 = 0x58,
            oem_1 = 0x5a, /* VK_OEM_WSCTRL */
            oem_2 = 0x5b, /* VK_OEM_FINISH */
            oem_3 = 0x5c, /* VK_OEM_JUMP */
            eraseEOF = 0x5d,
            oem_4 = 0x5e, /* VK_OEM_BACKTAB */
            oem_5 = 0x5f, /* VK_OEM_AUTO */
            zoom = 0x62,
            help = 0x63,
            f13 = 0x64,
            f14 = 0x65,
            f15 = 0x66,
            f16 = 0x67,
            f17 = 0x68,
            f18 = 0x69,
            f19 = 0x6a,
            f20 = 0x6b,
            f21 = 0x6c,
            f22 = 0x6d,
            f23 = 0x6e,
            oem_6 = 0x6f, /* VK_OEM_PA3 */
            katakana = 0x70,
            oem_7 = 0x71, /* VK_OEM_RESET */
            f24 = 0x76,
            sbcschar = 0x77,
            convert = 0x79,
            nonconvert = 0x7B, /* VK_OEM_PA1 */

            media_previous = 0xE010,
            media_next = 0xE019,
            numpad_enter = 0xE01C,
            controlRight = 0xE01D,
            volume_mute = 0xE020,
            launch_app2 = 0xE021,
            media_play = 0xE022,
            media_stop = 0xE024,
            volume_down = 0xE02E,
            volume_up = 0xE030,
            browser_home = 0xE032,
            numpad_divide = 0xE035,
            printScreen = 0xE037,
            /*
            printScreen:
            - make: 0xE02A 0xE037
            - break: 0xE0B7 0xE0AA
            - MapVirtualKeyEx( VK_SNAPSHOT, MAPVK_VK_TO_VEX, 0 ) returns scancode 0x54;
            - There is no VK_KEYDOWN with VK_SNAPSHOT.
            */
            altRight = 0xE038,
            cancel = 0xE046, /* CTRL + Pause */
            home = 0xE047,
            arrowUp = 0xE048,
            pageUp = 0xE049,
            arrowLeft = 0xE04B,
            arrowRight = 0xE04D,
            end = 0xE04F,
            arrowDown = 0xE050,
            pageDown = 0xE051,
            insert = 0xE052,
            delete = 0xE053,
            metaLeft = 0xE05B,
            metaRight = 0xE05C,
            application = 0xE05D,
            power = 0xE05E,
            sleep = 0xE05F,
            wake = 0xE063,
            browser_search = 0xE065,
            browser_favorites = 0xE066,
            browser_refresh = 0xE067,
            browser_stop = 0xE068,
            browser_forward = 0xE069,
            browser_back = 0xE06A,
            launch_app1 = 0xE06B,
            launch_email = 0xE06C,
            launch_media = 0xE06D,

            pause = 0xE11D45,
            /*
            pause:
            - make: 0xE11D 45 0xE19D C5
            - make in raw input: 0xE11D 0x45
            - break: none
            - No repeat when you hold the key down
            - There are no break so I don't know how the key down/up is expected to work. Raw input sends "keydown" and "keyup" messages, and it appears that the keyup message is sent directly after the keydown message (you can't hold the key down) so depending on when GetMessage or PeekMessage will return messages, you may get both a keydown and keyup message "at the same time". If you use VK messages most of the time you only get keydown messages, but some times you get keyup messages too.
            - when pressed at the same time as one or both control keys, generates a 0xE046 (cancel) and the string for that scancode is "break".
            */
        };
    }


    public static class GenericKeyStrokes
    {

    }
}
