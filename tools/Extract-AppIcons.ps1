param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\StartMenu.App\Assets\AppIcons'),
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase
Add-Type -AssemblyName System.Xaml

$extractorSource = @'
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

public static class ShellIconExtractor
{
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void SHCreateItemFromParsingName(
        string path, IntPtr pbc, ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out object shellItem);

    [ComImport]
    [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemImageFactory
    {
        [PreserveSig]
        int GetImage(Size size, uint flags, out IntPtr hBitmap);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Size
    {
        public int cx;
        public int cy;
    }

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    public static string Save(string path, string output, int size)
    {
        Guid iid = new Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b");
        object item;
        SHCreateItemFromParsingName(path, IntPtr.Zero, ref iid, out item);
        IShellItemImageFactory factory = (IShellItemImageFactory)item;

        IntPtr hBitmap;
        int hr = factory.GetImage(
            new Size { cx = size, cy = size },
            0x00000004u | 0x00000001u | 0x00000100u,
            out hBitmap);

        if (hr != 0 || hBitmap == IntPtr.Zero)
        {
            return string.Format("HRESULT 0x{0:X8}", hr);
        }

        try
        {
            BitmapSource source = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            using (FileStream stream = File.Create(output))
            {
                encoder.Save(stream);
            }
            return string.Format("{0}x{1}", source.PixelWidth, source.PixelHeight);
        }
        finally
        {
            DeleteObject(hBitmap);
        }
    }
}
'@

$references = @(
    [System.Windows.Media.Imaging.BitmapSource].Assembly.Location,
    [System.Windows.Rect].Assembly.Location,
    [System.Windows.Interop.Imaging].Assembly.Location,
    [System.Xaml.XamlReader].Assembly.Location
) | Select-Object -Unique

Add-Type -TypeDefinition $extractorSource -ReferencedAssemblies $references

$shortcutMap = @{}
$shortcutShell = New-Object -ComObject WScript.Shell
$startMenuDirectories = @(
    (Join-Path $env:ProgramData 'Microsoft\Windows\Start Menu\Programs'),
    (Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs')
)

foreach ($directory in $startMenuDirectories)
{
    if (-not (Test-Path -LiteralPath $directory))
    {
        continue
    }

    Get-ChildItem -LiteralPath $directory -Recurse -Filter '*.lnk' -ErrorAction SilentlyContinue |
        ForEach-Object {
            if (-not $shortcutMap.ContainsKey($_.BaseName))
            {
                $shortcutMap[$_.BaseName] = $_.FullName
            }
        }
}

$startAppMap = @{}
Get-StartApps | ForEach-Object {
    if (-not $startAppMap.ContainsKey($_.Name))
    {
        $startAppMap[$_.Name] = $_.AppID
    }
}

$mapping = [ordered]@{
    vscode        = @{ Aliases = @('Visual Studio Code') }
    blend         = @{ Aliases = @('Blend for Visual Studio') }
    visualstudio  = @{ Aliases = @('Visual Studio') }
    edge          = @{ Aliases = @('Microsoft Edge') }
    outlook       = @{ Aliases = @('Outlook (PWA)') }
    wechat        = @{ Aliases = @('微信') }
    intellij      = @{ Aliases = @('IntelliJ IDEA 2026.2.3', 'IntelliJ IDEA 2026.1.4') }
    store         = @{ Aliases = @('Microsoft Store') }
    settings      = @{ Aliases = @('设置') }
    photos        = @{ Aliases = @('照片') }
    calculator    = @{ Aliases = @('计算器') }
    clock         = @{ Aliases = @('时钟') }
    notepad       = @{ Aliases = @('记事本') }
    everything    = @{ Path = 'D:\others\Tools Application\Everything\Everything\Everything.exe' }
    powertoys     = @{ Path = 'C:\Users\Xiane\AppData\Local\PowerToys\PowerToys.exe' }
    palette       = @{ Aliases = @('命令面板') }
    codex         = @{ Aliases = @('Codex++') }
    'codex-manager' = @{ Aliases = @('Codex++ 管理工具') }
    'cc-switch'   = @{ Aliases = @('CC Switch') }
    terminal      = @{ Aliases = @('终端') }
    fastgithub    = @{ Aliases = @('FastGithub.UI') }
    chrome        = @{ Aliases = @('Google Chrome') }
    obs           = @{ Aliases = @('OBS Studio') }
    modrinth      = @{ Aliases = @('Modrinth App') }
    libreoffice   = @{ Path = 'D:\others\Office\program\soffice.exe' }
    pcl           = @{ Aliases = @('Plain Craft Launcher') }
    qidong        = @{ Aliases = @('启动异环') }
    mihoyo        = @{ Aliases = @('miHoYo Launcher') }
    steam         = @{ Aliases = @('Steam') }
    thispc        = @{ Path = '::{20D04FE0-3AEA-1069-A2D8-08002B30309D}' }
    recycle       = @{ Path = '::{645FF040-5081-101B-9F08-00AA002F954E}' }
    deepseek      = @{ Aliases = @('DeepSeek Harness') }
    word          = @{ Aliases = @('Word') }
    excel         = @{ Aliases = @('Excel') }
    powerpoint    = @{ Aliases = @('PowerPoint') }
    onenote       = @{ Aliases = @('OneNote') }
    calendar      = @{ Aliases = @('日历') }
    mail          = @{ Aliases = @('邮件') }
    teams         = @{ Aliases = @('Microsoft Teams') }
    sevenzip      = @{ Aliases = @('7-Zip File Manager') }
    snipping      = @{ Aliases = @('截图工具') }
    taskmgr       = @{ Aliases = @('任务管理器', 'Task Manager') }
    controlpanel  = @{ Aliases = @('控制面板', 'Control Panel') }
}

function Resolve-ShellItem
{
    param([hashtable]$Spec)

    if ($Spec.ContainsKey('Path'))
    {
        return $Spec.Path
    }

    foreach ($alias in $Spec.Aliases)
    {
        if ($shortcutMap.ContainsKey($alias))
        {
            return $shortcutMap[$alias]
        }

        if ($startAppMap.ContainsKey($alias))
        {
            $appId = $startAppMap[$alias]
            if ($appId -match '^[A-Za-z]:\\' -or $appId -match '^\\\\')
            {
                return $appId
            }

            return "shell:AppsFolder\$appId"
        }
    }

    return $null
}

if (-not (Test-Path -LiteralPath $OutputDirectory))
{
    New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
}

$extracted = @()
$skipIds = @('powertoys', 'libreoffice') # shell/exe icons are generic; keep the color+glyph fallback
$missing = @()

foreach ($id in $mapping.Keys)
{
    if ($skipIds -contains $id)
    {
        continue
    }

    $source = Resolve-ShellItem -Spec $mapping[$id]
    if (-not $source)
    {
        $missing += $id
        Write-Warning "$id : no matching Start menu entry"
        continue
    }

    $target = Join-Path $OutputDirectory "$id.png"
    if ((Test-Path -LiteralPath $target) -and -not $Force)
    {
        Write-Host "$id : exists, skipped"
        continue
    }

    try
    {
        $size = [ShellIconExtractor]::Save($source, $target, 256)
        $extracted += [pscustomobject]@{ Id = $id; Source = $source; Size = $size; Output = $target }
        Write-Host "$id : $size"
    }
    catch
    {
        $missing += $id
        Write-Warning "$id : $($_.Exception.Message)"
    }
}

Write-Host ''
Write-Host "Extracted: $($extracted.Count)"
Write-Host "Missing:   $($missing.Count)"
if ($missing.Count -gt 0)
{
    Write-Host ("Missing ids: " + ($missing -join ', '))
}
