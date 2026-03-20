using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Wauncher.Utils
{
    public class SteamNative
    {
        // 64-bit steam_api calls
        [DllImport("platform/steam_api64", EntryPoint = "SteamAPI_InitFlat", CallingConvention = CallingConvention.Cdecl)]
        private unsafe static extern int SteamAPI64_InitFlat(byte* steamErrMsg);
        [DllImport("platform/steam_api64", EntryPoint = "SteamAPI_GetSteamInstallPath", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SteamAPI64_GetSteamInstallPath();
        [DllImport("platform/steam_api64", EntryPoint = "SteamAPI_Shutdown", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SteamAPI64_Shutdown();

        // 32-bit steam_api calls
        [DllImport("platform/steam_api", EntryPoint = "SteamAPI_InitFlat", CallingConvention = CallingConvention.Cdecl)]
        private unsafe static extern int SteamAPI_InitFlat(byte* steamErrMsg);
        [DllImport("platform/steam_api", EntryPoint = "SteamAPI_GetSteamInstallPath", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SteamAPI_GetSteamInstallPath();
        [DllImport("platform/steam_api", EntryPoint = "SteamAPI_Shutdown", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SteamAPI_Shutdown();

        private static string? _steamPath = null;

        public static string? GetSteamInstallPath()
        {
            // If was already found return it right away.
            if (_steamPath != null)
                return _steamPath;

            // If it wasn't found before by registry, try using Steamworks.
            // (Steamworks.NET doesn't give access to native methods)
            string steamDll = Environment.Is64BitProcess ? "steam_api64.dll" : "steam_api.dll";
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(steamDll)
                ?? throw new Exception($"{steamDll} wasn't found in the binary!");

            // If the needed steam_api(64).dll doesn't exist in the folder, unpack it from the binary.
            var outputPath = Path.Combine(AppContext.BaseDirectory, "platform", steamDll);
            if (!File.Exists(outputPath))
            {
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "platform"));
                using (var file = File.Create(outputPath))
                    stream.CopyTo(file);
            }

            // Make sure the steam_appid.txt exists, because if it doesn't steam throws an error.
            if (!File.Exists("steam_appid.txt"))
                File.WriteAllText("steam_appid.txt", "730");

            unsafe
            {
                byte* errMsg = stackalloc byte[1024];
                int result = Environment.Is64BitProcess ? SteamAPI64_InitFlat(errMsg) : SteamAPI_InitFlat(errMsg);
                if (result > 0)
                {
                    ConsoleManager.ShowError($"Steamworks couldn't initialize to find Steam. (Error ({result}) {Marshal.PtrToStringUTF8((IntPtr)errMsg)})");
                    return null;
                }
                _steamPath = Marshal.PtrToStringUTF8(Environment.Is64BitProcess ? SteamAPI64_GetSteamInstallPath() : SteamAPI_GetSteamInstallPath());
                if (Environment.Is64BitProcess) SteamAPI64_Shutdown(); else SteamAPI_Shutdown();
            }

            return _steamPath;
        }
    }
}
