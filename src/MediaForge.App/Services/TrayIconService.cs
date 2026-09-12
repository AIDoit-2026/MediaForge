using System.Runtime.InteropServices;

namespace MediaForge.App.Services;

/// <summary>Owns the unpackaged application's notification-area icon and menu.</summary>
public sealed class TrayIconService : IDisposable
{
    public const uint CallbackMessage = 0x8001;

    private const uint NimAdd = 0;
    private const uint NimModify = 1;
    private const uint NimDelete = 2;
    private const uint NimSetVersion = 4;
    private const uint NifMessage = 0x00000001;
    private const uint NifIcon = 0x00000002;
    private const uint NifTip = 0x00000004;
    private const uint WmLButtonDblClk = 0x0203;
    private const uint WmRButtonUp = 0x0205;
    private const uint WmContextMenu = 0x007B;
    private const uint TpmRetCmd = 0x0100;
    private const uint ImageIcon = 1;
    private const uint LrLoadFromFile = 0x00000010;
    private const uint LrDefaultSize = 0x00000040;
    private const int OpenCommand = 1001;
    private const int ExitCommand = 1002;

    private readonly nint _windowHandle;
    private readonly Action _showWindow;
    private readonly Func<Task> _exitAsync;
    private readonly nint _iconHandle;
    private readonly bool _ownsIcon;
    private string _openLabel = "Open window";
    private string _exitLabel = "Exit application";
    private string _tooltip = "MediaForge";
    private bool _disposed;
    private bool _registered;

    public bool IsRegistered => _registered;
    public int RegistrationError { get; private set; }

    public TrayIconService(nint windowHandle, string iconPath, Action showWindow, Func<Task> exitAsync)
    {
        ArgumentNullException.ThrowIfNull(showWindow);
        ArgumentNullException.ThrowIfNull(exitAsync);

        _windowHandle = windowHandle;
        _showWindow = showWindow;
        _exitAsync = exitAsync;
        _iconHandle = LoadImage(
            nint.Zero,
            iconPath,
            ImageIcon,
            0,
            0,
            LrLoadFromFile | LrDefaultSize);
        _ownsIcon = _iconHandle != 0;
        if (_iconHandle == 0)
        {
            _iconHandle = LoadIcon(nint.Zero, new IntPtr(32512)); // IDI_APPLICATION
        }

        _registered = AddOrUpdate(NimAdd);
        RegistrationError = _registered ? 0 : Marshal.GetLastWin32Error();
        if (_registered)
        {
            var versionData = new NotifyIconData
            {
                Size = (uint)Marshal.SizeOf<NotifyIconData>(),
                WindowHandle = _windowHandle,
                Id = 1,
                VersionOrTimeout = 4,
                Info = string.Empty,
                InfoTitle = string.Empty
            };
            Shell_NotifyIcon(NimSetVersion, ref versionData);
        }
    }

    public void UpdateStrings(string openLabel, string exitLabel, string tooltip)
    {
        if (_disposed)
        {
            return;
        }

        _openLabel = openLabel;
        _exitLabel = exitLabel;
        _tooltip = tooltip;
        if (_registered)
        {
            AddOrUpdate(NimModify);
        }
    }

    public bool TryHandleMessage(uint message, nint lParam)
    {
        if (_disposed || message != CallbackMessage)
        {
            return false;
        }

        // NOTIFYICON_VERSION_4 packs the mouse message into LOWORD(lParam)
        // and the icon identifier into HIWORD(lParam).
        var mouseMessage = unchecked((uint)lParam.ToInt64()) & 0xFFFF;
        if (mouseMessage == WmLButtonDblClk)
        {
            _showWindow();
        }
        else if (mouseMessage is WmRButtonUp or WmContextMenu)
        {
            ShowMenu();
        }

        return true;
    }

    private bool AddOrUpdate(uint operation)
    {
        var data = new NotifyIconData
        {
            Size = (uint)Marshal.SizeOf<NotifyIconData>(),
            WindowHandle = _windowHandle,
            Id = 1,
            Flags = NifMessage | NifIcon | NifTip,
            CallbackMessage = CallbackMessage,
            IconHandle = _iconHandle,
            Tip = _tooltip.Length > 127 ? _tooltip[..127] : _tooltip,
            Info = string.Empty,
            InfoTitle = string.Empty
        };
        return Shell_NotifyIcon(operation, ref data);
    }

    private void ShowMenu()
    {
        var menu = CreatePopupMenu();
        if (menu == 0)
        {
            return;
        }

        try
        {
            AppendMenu(menu, 0, (nuint)OpenCommand, _openLabel);
            AppendMenu(menu, 0, (nuint)ExitCommand, _exitLabel);
            GetCursorPos(out var point);
            SetForegroundWindow(_windowHandle);
            var command = TrackPopupMenuEx(
                menu,
                TpmRetCmd,
                point.X,
                point.Y,
                _windowHandle,
                nint.Zero);
            PostMessage(_windowHandle, 0x0000, nint.Zero, nint.Zero);
            if (command == OpenCommand)
            {
                _showWindow();
            }
            else if (command == ExitCommand)
            {
                _ = _exitAsync();
            }
        }
        finally
        {
            DestroyMenu(menu);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        var data = new NotifyIconData
        {
            Size = (uint)Marshal.SizeOf<NotifyIconData>(),
            WindowHandle = _windowHandle,
            Id = 1,
            Info = string.Empty,
            InfoTitle = string.Empty
        };
        Shell_NotifyIcon(NimDelete, ref data);
        if (_ownsIcon && _iconHandle != 0)
        {
            DestroyIcon(_iconHandle);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public uint Size;
        public nint WindowHandle;
        public uint Id;
        public uint Flags;
        public uint CallbackMessage;
        public nint IconHandle;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Tip;
        public uint State;
        public uint StateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Info;
        public uint VersionOrTimeout;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string InfoTitle;
        public uint InfoFlags;
        public Guid GuidItem;
        public nint BalloonIconHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Shell_NotifyIcon(uint message, ref NotifyIconData data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint LoadImage(nint instance, string name, uint imageType, int width, int height, uint loadFlags);

    [DllImport("user32.dll")]
    private static extern nint LoadIcon(nint instance, nint name);

    [DllImport("user32.dll")]
    private static extern nint CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AppendMenu(nint menu, uint flags, nuint id, string newItem);

    [DllImport("user32.dll")]
    private static extern int TrackPopupMenuEx(nint menu, uint flags, int x, int y, nint owner, nint parameters);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyMenu(nint menu);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint windowHandle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(nint windowHandle, uint message, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(nint iconHandle);
}
