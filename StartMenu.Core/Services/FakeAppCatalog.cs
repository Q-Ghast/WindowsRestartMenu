using StartMenu.Core.Models;

namespace StartMenu.Core.Services;

public sealed class FakeAppCatalog
{
    private static readonly string[] PinnedOrder =
    {
        "vscode", "blend", "visualstudio", "edge", "outlook", "wechat", "intellij", "store",
        "settings", "photos", "calculator", "clock", "notepad", "everything", "powertoys", "palette",
        "codex", "codex-manager", "cc-switch", "terminal", "fastgithub", "chrome", "obs", "modrinth",
        "libreoffice", "pcl", "qidong", "mihoyo", "steam", "thispc", "recycle", "deepseek"
    };

    private static readonly (string Id, string Name, string Category, string Glyph, string Color)[] Definition =
    {
        ("vscode", "VS Code", "dev", "\uE943", "#007ACC"),
        ("blend", "Blend for Visual Studio", "dev", "\uE943", "#7E4CC4"),
        ("visualstudio", "Visual Studio", "dev", "\uE943", "#5C2D91"),
        ("edge", "Microsoft Edge", "productivity", "\uE774", "#0A84FF"),
        ("outlook", "Outlook (PWA)", "productivity", "\uE715", "#0F6CBD"),
        ("wechat", "微信", "other", "\uE8BD", "#07C160"),
        ("intellij", "IntelliJ IDEA 2026.2.3", "dev", "\uE943", "#FE315D"),
        ("store", "Microsoft Store", "other", "\uE719", "#0067B8"),
        ("settings", "设置", "utilities", "\uE713", "#5C5C5C"),
        ("photos", "照片", "utilities", "\uE91B", "#0078D4"),
        ("calculator", "计算器", "utilities", "\uE8EF", "#0078D4"),
        ("clock", "时钟", "utilities", "\uE823", "#0078D4"),
        ("notepad", "记事本", "utilities", "\uE70F", "#0078D4"),
        ("everything", "Everything", "utilities", "\uE721", "#F5A623"),
        ("powertoys", "PowerToys (Preview)", "utilities", "\uE713", "#0078D4"),
        ("palette", "命令面板", "utilities", "\uE756", "#2B2B2B"),
        ("codex", "Codex++", "dev", "\uE943", "#10A37F"),
        ("codex-manager", "Codex++ 管理工具", "dev", "\uE713", "#10A37F"),
        ("cc-switch", "CC Switch", "dev", "\uE8AB", "#FF6B35"),
        ("terminal", "终端", "dev", "\uE756", "#0C0C0C"),
        ("fastgithub", "FastGithub.UI", "dev", "\uE774", "#24292F"),
        ("chrome", "Google Chrome", "other", "\uE774", "#4285F4"),
        ("obs", "OBS Studio", "utilities", "\uE714", "#302E31"),
        ("modrinth", "Modrinth App", "other", "\uE7FC", "#1BD96A"),
        ("libreoffice", "LibreOffice", "productivity", "\uE8A5", "#18A303"),
        ("pcl", "Plain Craft Launcher", "other", "\uE7FC", "#2D89EF"),
        ("qidong", "启动异环", "other", "\uE7FC", "#FF4D6D"),
        ("mihoyo", "miHoYo Launcher", "other", "\uE7FC", "#4A90E2"),
        ("steam", "Steam", "other", "\uE7FC", "#171A21"),
        ("thispc", "此电脑", "utilities", "\uE7F4", "#0078D4"),
        ("recycle", "回收站", "utilities", "\uE74D", "#6B6B6B"),
        ("deepseek", "DeepSeek Harness", "other", "\uE8BD", "#4D6BFE"),
        ("word", "Word", "productivity", "\uE8A5", "#2B579A"),
        ("excel", "Excel", "productivity", "\uE8A5", "#217346"),
        ("powerpoint", "PowerPoint", "productivity", "\uE8A5", "#D24726"),
        ("onenote", "OneNote", "productivity", "\uE8A5", "#80397B"),
        ("calendar", "日历", "productivity", "\uE787", "#0078D4"),
        ("mail", "邮件", "productivity", "\uE715", "#0078D4"),
        ("teams", "Teams", "other", "\uE8BD", "#6264A7"),
        ("discord", "Discord", "other", "\uE8BD", "#5865F2"),
        ("qq", "QQ", "other", "\uE8BD", "#12B7F5"),
        ("minecraft", "Minecraft", "other", "\uE7FC", "#62B47A"),
        ("epic", "Epic Games", "other", "\uE7FC", "#313131"),
        ("firefox", "Firefox", "other", "\uE774", "#FF7139"),
        ("sevenzip", "7-Zip", "utilities", "\uE8A5", "#000000"),
        ("snipping", "截图工具", "utilities", "\uE722", "#0078D4"),
        ("taskmgr", "任务管理器", "utilities", "\uE7F4", "#0078D4"),
        ("controlpanel", "控制面板", "utilities", "\uE713", "#0078D4")
    };

    public FakeAppCatalog()
    {
        AllApps = Definition
            .Select(item => new AppEntry(item.Id, item.Name, item.Category, item.Glyph, item.Color))
            .ToArray();

        var byId = AllApps.ToDictionary(app => app.Id);
        PinnedApps = PinnedOrder.Select(id => byId[id]).ToArray();

        Categories = new[]
        {
            new CategoryGroup("productivity", "高效工作", AppsIn("productivity")),
            new CategoryGroup("dev", "开发人员工具", AppsIn("dev")),
            new CategoryGroup("other", "其他", AppsIn("other")),
            new CategoryGroup("utilities", "实用程序与工具", AppsIn("utilities"))
        };
    }

    public IReadOnlyList<AppEntry> AllApps { get; }

    public IReadOnlyList<AppEntry> PinnedApps { get; }

    public IReadOnlyList<CategoryGroup> Categories { get; }

    private IReadOnlyList<AppEntry> AppsIn(string categoryId)
    {
        return AllApps.Where(app => app.CategoryId == categoryId).ToArray();
    }
}
