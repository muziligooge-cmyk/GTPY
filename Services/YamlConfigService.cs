using System;
using System.IO;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using System.Collections.Generic;

namespace BlueGlassMihomoClient.Services;

public static class YamlConfigService
{
    public static string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "config.yaml");

    public static (bool success, string message) ValidateAndSave(string sourcePath)
    {
        try
        {
            string content = File.ReadAllText(sourcePath);
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();
            
            var yaml = deserializer.Deserialize<Dictionary<object, object>>(content);

            if (!yaml.ContainsKey("proxies") || !yaml.ContainsKey("proxy-groups") || !yaml.ContainsKey("rules"))
            {
                return (false, "配置文件不完整，请检查 YAML。");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath, content);
            LogService.LogApp($"成功导入配置文件: {sourcePath}");
            return (true, "配置导入成功。");
        }
        catch (Exception ex)
        {
            LogService.LogApp($"YAML 验证失败: {ex.Message}");
            return (false, $"解析失败: {ex.Message}");
        }
    }

    public static void PatchConfigForRunning()
    {
        if (!File.Exists(ConfigPath)) return;

        string content = File.ReadAllText(ConfigPath);

        // 安全修改顶层字段
        var patches = new Dictionary<string, string>
        {
            { "mode", "rule" },
            { "mixed-port", "7890" },
            { "external-controller", "127.0.0.1:9090" },
            { "allow-lan", "false" },
            { "log-level", "info" }
        };

        foreach (var patch in patches)
        {
            // 匹配行首的 key: value，支持可选空格
            string pattern = $@"^{patch.Key}:\s*.*$";
            string replacement = $"{patch.Key}: {patch.Value}";

            if (Regex.IsMatch(content, pattern, RegexOptions.Multiline))
            {
                content = Regex.Replace(content, pattern, replacement, RegexOptions.Multiline);
            }
            else
            {
                // 如果不存在，插入到顶部
                content = $"{patch.Key}: {patch.Value}\n" + content;
            }
        }

        File.WriteAllText(ConfigPath, content);
        LogService.LogApp("已完成 YAML 运行参数安全修补");
    }
}
