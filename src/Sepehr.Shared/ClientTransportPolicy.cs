using System;

namespace Sepehr.Shared
{
    /// <summary>
    /// ClientWebSocket در .NET Framework فقط از Windows 8 (نسخه 6.2) به بعد کار می‌کند.
    /// روی Windows 7 (نسخه 6.1) کلاینت SignalR باید فقط SSE و LongPolling را فعال کند.
    /// نسخه سیستم‌عامل باید از Environment.OSVersion.Version خوانده شود.
    /// </summary>
    public static class ClientTransportPolicy
    {
        private static readonly Version FirstWebSocketVersion = new Version(6, 2);

        public static bool CanUseWebSockets(Version windowsVersion)
        {
            if (windowsVersion == null) throw new ArgumentNullException(nameof(windowsVersion));
            return windowsVersion >= FirstWebSocketVersion;
        }
    }
}
