using System.Text;
using System.Text.RegularExpressions;

namespace WukongBench;

/// <summary>Бэкап, правка и восстановление GameUserSettings.ini.</summary>
public sealed class SettingsFile(string path)
{
    private readonly string _backupPath = path + ".wukongbench.bak";

    /// <summary>Если прошлый запуск упал и оставил бэкап, сначала вернуть оригинал.</summary>
    public void RecoverAfterCrash()
    {
        if (File.Exists(_backupPath))
        {
            Console.WriteLine("Найден бэкап от прошлого запуска, восстанавливаю конфиг.");
            Restore();
        }
    }

    public void Backup()
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Не найден конфиг: {path}. Запустите бенчмарк вручную хотя бы раз.");
        File.Copy(path, _backupPath, overwrite: true);
    }

    public void Restore()
    {
        if (!File.Exists(_backupPath)) return;
        File.Copy(_backupPath, path, overwrite: true);
        File.Delete(_backupPath);
    }

    public void Apply(BenchmarkProfile profile)
    {
        string text;
        Encoding encoding;
        using (var reader = new StreamReader(path, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true))
        {
            text = reader.ReadToEnd();
            encoding = reader.CurrentEncoding; // UE может писать ini в UTF-16, сохраняем как было
        }

        var lines = text.Replace("\r\n", "\n").Split('\n').ToList();

        foreach (var (key, value) in profile.UiSettings)
            SetUiValue(lines, key, value);
        foreach (var ((section, key), value) in profile.IniValues)
            SetIniValue(lines, section, key, value);

        File.WriteAllText(path, string.Join("\r\n", lines), encoding);
    }

    // UISettingData=(("Key", "Value"),("Key2", "Value2"),...)
    private static void SetUiValue(List<string> lines, string key, string value)
    {
        var index = lines.FindIndex(l => l.StartsWith("UISettingData="));
        if (index < 0) throw new InvalidOperationException("В конфиге нет строки UISettingData.");

        var pattern = new Regex($"\\(\"{Regex.Escape(key)}\", \"[^\"]*\"\\)");
        if (!pattern.IsMatch(lines[index]))
            throw new InvalidOperationException($"В UISettingData нет ключа {key}.");

        lines[index] = pattern.Replace(lines[index], $"(\"{key}\", \"{value}\")");
    }

    private static void SetIniValue(List<string> lines, string section, string key, string value)
    {
        var header = lines.FindIndex(l => l.Trim() == $"[{section}]");
        if (header < 0) throw new InvalidOperationException($"В конфиге нет секции [{section}].");

        for (var i = header + 1; i < lines.Count && !lines[i].StartsWith('['); i++)
        {
            if (lines[i].StartsWith(key + "="))
            {
                lines[i] = $"{key}={value}";
                return;
            }
        }
        lines.Insert(header + 1, $"{key}={value}");
    }
}
