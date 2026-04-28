# BlueGlassMihomoClient

一个基于 C# WPF 和 .NET 8 开发的 Windows 桌面 Mihomo (Clash) 客户端。

## 特点
- **苹果蓝白液态玻璃风格**：通透、现代、简约。
- **完全动态适配**：不硬编码任何规则、节点或代理组名。
- **文本级安全修改**：仅修改运行必选参数，不破坏 YAML 结构与注释。
- **高性能**：异步处理核心启动与 API 交互。

## 目录结构
- `core/`: 存放 `mihomo.exe`。
- `config/`: 运行时的 `config.yaml`。
- `logs/`: 应用日志与核心日志。

## 使用方法
1. 下载 `mihomo.exe` 并放入 `core/` 目录。
2. 运行 `BlueGlassMihomoClient.exe`。
3. 在“配置导入”页面导入您的 YAML 订阅或配置文件。
4. 在“运行状态”页面点击启动。
5. 在“节点选择”页面切换您喜欢的节点。

## 编译
```bash
dotnet restore
dotnet build
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```
