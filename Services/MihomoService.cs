using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BlueGlassMihomoClient.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

using System.IO.Compression;

namespace BlueGlassMihomoClient.Services;

public class MihomoService
{
    private Process? _process;
    private readonly HttpClient _httpClient = new();
    private const string ApiBase = "http://127.0.0.1:9090";

    public async Task<bool> Start()
    {
        string coreDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "core");
        string exePath = Path.Combine(coreDir, "mihomo.exe");
        
        if (!File.Exists(exePath))
        {
            LogService.LogApp("核心不存在，尝试自动下载...");
            var downloaded = await DownloadMihomo(coreDir);
            if (!downloaded) return false;
        }

        YamlConfigService.PatchConfigForRunning();

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"-f \"{YamlConfigService.ConfigPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(exePath)
            }
        };

        _process.OutputDataReceived += (s, e) => { if (e.Data != null) LogService.LogMihomo(e.Data); };
        _process.ErrorDataReceived += (s, e) => { if (e.Data != null) LogService.LogMihomo($"[ERROR] {e.Data}"); };

        try
        {
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            // 等待 API 可用
            for (int i = 0; i < 20; i++)
            {
                try
                {
                    var res = await _httpClient.GetAsync($"{ApiBase}/version");
                    if (res.IsSuccessStatusCode) return true;
                }
                catch { }
                await Task.Delay(500);
            }
            return false;
        }
        catch (Exception ex)
        {
            LogService.LogApp($"mihomo 启动异常: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> DownloadMihomo(string targetDir)
    {
        try
        {
            Directory.CreateDirectory(targetDir);
            // 这里使用 MetaCubeX 官方的最新构建地址（示例为 windows-amd64）
            string downloadUrl = "https://github.com/MetaCubeX/mihomo/releases/download/v1.18.3/mihomo-windows-amd64-v1.18.3.zip";
            string zipPath = Path.Combine(targetDir, "core.zip");

            LogService.LogApp("开始从 GitHub 下载 Mihomo 核心...");
            var bytes = await _httpClient.GetByteArrayAsync(downloadUrl);
            await File.WriteAllBytesAsync(zipPath, bytes);

            LogService.LogApp("下载完成，正在解压...");
            ZipFile.ExtractToDirectory(zipPath, targetDir, true);
            
            // 查找到解压出的 exe 并重命名
            var exeFile = Directory.GetFiles(targetDir, "*.exe").FirstOrDefault();
            if (exeFile != null)
            {
                string finalPath = Path.Combine(targetDir, "mihomo.exe");
                if (File.Exists(finalPath)) File.Delete(finalPath);
                File.Move(exeFile, finalPath);
            }

            File.Delete(zipPath);
            LogService.LogApp("核心准备就绪。");
            return true;
        }
        catch (Exception ex)
        {
            LogService.LogApp($"核心下载失败: {ex.Message}");
            return false;
        }
    }

    public void Stop()
    {
        try
        {
            if (_process != null && !_process.HasExited)
            {
                _process.Kill(true);
            }
        }
        catch { }
        _process = null;
    }

    public async Task<List<ProxyGroup>> GetProxyGroups()
    {
        try
        {
            var json = await _httpClient.GetStringAsync($"{ApiBase}/proxies");
            var data = JsonConvert.DeserializeObject<dynamic>(json);
            var groups = new List<ProxyGroup>();

            foreach (var proxy in data.proxies)
            {
                var type = (string)proxy.Value.type;
                if (type == "Selector" || type == "URLTest" || type == "Fallback" || type == "LoadBalance")
                {
                    var group = new ProxyGroup
                    {
                        Name = proxy.Name,
                        Type = type,
                        Now = (string)proxy.Value.now,
                        All = ((IEnumerable<dynamic>)proxy.Value.all).Select(x => (string)x).ToList()
                    };
                    groups.Add(group);
                }
            }
            return groups;
        }
        catch (Exception ex)
        {
            LogService.LogApp($"获取代理组失败: {ex.Message}");
            return new List<ProxyGroup>();
        }
    }

    public async Task<bool> SwitchProxy(string groupName, string proxyName)
    {
        try
        {
            var url = $"{ApiBase}/proxies/{Uri.EscapeDataString(groupName)}";
            var payload = JsonConvert.SerializeObject(new { name = proxyName });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var res = await _httpClient.PutAsync(url, content);
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            LogService.LogApp($"切换节点失败: {ex.Message}");
            return false;
        }
    }

    public async Task<int> GetDelay(string proxyName)
    {
        try
        {
            var url = $"{ApiBase}/proxies/{Uri.EscapeDataString(proxyName)}/delay?timeout=5000&url=https://www.gstatic.com/generate_204";
            var res = await _httpClient.GetAsync(url);
            if (res.IsSuccessStatusCode)
            {
                var json = await res.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<dynamic>(json);
                return (int)data.delay;
            }
            return -1;
        }
        catch { return -1; }
    }
}
