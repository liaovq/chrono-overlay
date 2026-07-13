[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ExecutablePath
)

$resolvedPath = (Resolve-Path -LiteralPath $ExecutablePath).Path

if (-not ('ChronoOverlay.ManifestResourceReader' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace ChronoOverlay
{
    public static class ManifestResourceReader
    {
        private const uint LoadLibraryAsDataFile = 0x00000002;
        private static readonly IntPtr ManifestResourceType = new IntPtr(24);
        private static readonly IntPtr ManifestResourceId = new IntPtr(1);

        public static string Read(string executablePath)
        {
            IntPtr module = LoadLibraryEx(executablePath, IntPtr.Zero, LoadLibraryAsDataFile);
            if (module == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to load executable resources.");
            }

            try
            {
                IntPtr resource = FindResource(module, ManifestResourceId, ManifestResourceType);
                if (resource == IntPtr.Zero)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Embedded manifest resource was not found.");
                }

                uint size = SizeofResource(module, resource);
                IntPtr loaded = LoadResource(module, resource);
                IntPtr bytes = LockResource(loaded);
                if (size == 0 || loaded == IntPtr.Zero || bytes == IntPtr.Zero)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Embedded manifest resource could not be read.");
                }

                int length = checked((int)size);
                byte[] buffer = new byte[length];
                Marshal.Copy(bytes, buffer, 0, length);
                return Encoding.UTF8.GetString(buffer).TrimStart('\uFEFF', '\0');
            }
            finally
            {
                FreeLibrary(module);
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryEx(string fileName, IntPtr file, uint flags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr FindResource(IntPtr module, IntPtr name, IntPtr type);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint SizeofResource(IntPtr module, IntPtr resource);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadResource(IntPtr module, IntPtr resource);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LockResource(IntPtr resource);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr module);
    }
}
'@
}

$manifest = [ChronoOverlay.ManifestResourceReader]::Read($resolvedPath)
if ($manifest -notmatch '<dpiAware[^>]*>\s*true/pm\s*</dpiAware>') {
    throw 'The published executable manifest does not contain dpiAware=true/pm.'
}

if ($manifest -notmatch '<dpiAwareness[^>]*>\s*PerMonitorV2\s*</dpiAwareness>') {
    throw 'The published executable manifest does not contain dpiAwareness=PerMonitorV2.'
}

Write-Output "Embedded manifest verified: true/pm and PerMonitorV2 ($resolvedPath)."
