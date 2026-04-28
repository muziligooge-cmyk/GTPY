using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BlueGlassMihomoClient.Services;
using BlueGlassMihomoClient.Models;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;

namespace BlueGlassMihomoClient;

public partial class MainWindow : Window
{
    private readonly MihomoService _mihomo = new();
    private AppStatus _status = AppStatus.Stopped;
    private string _currentConfigName = "未导入";

    public MainWindow()
    {
        InitializeComponent();
        CheckConfigStatus();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        NavigateHome(null, null);
    }

    private void CheckConfigStatus()
    {
        if (File.Exists(YamlConfigService.ConfigPath))
        {
            _currentConfigName = "config.yaml";
        }
    }

    private void OnMinimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    
    private void OnClose(object sender, RoutedEventArgs e) 
    {
        _mihomo.Stop();
        SystemProxyService.UnsetProxy();
        Application.Current.Shutdown();
    }

    private string GetStatusText() => _status switch 
    {
        AppStatus.Running => "已开启",
        AppStatus.Starting => "正在启动...",
        AppStatus.Failed => "启动失败",
        _ => "未开启"
    };

    private void NavigateHome(object sender, RoutedEventArgs e)
    {
        var panel = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        
        var statusLabel = new TextBlock 
        { 
            Text = GetStatusText(), 
            FontSize = 36, 
            FontWeight = FontWeight.FromOpenTypeWeight(700),
            Foreground = _status == AppStatus.Running ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48BB78")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D3748")),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0,0,0,10)
        };

        var configLabel = new TextBlock 
        { 
            Text = $"配置文件: {_currentConfigName}", 
            FontSize = 14, 
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#718096")),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0,0,0,40)
        };

        var startBtn = new Button 
        { 
            Content = _status == AppStatus.Running ? "停止服务" : "启动核心", 
            Style = (Style)FindResource("CapsuleButton"),
            Width = 180,
            IsEnabled = File.Exists(YamlConfigService.ConfigPath)
        };
        
        startBtn.Click += async (s, ex) => {
            if (_status == AppStatus.Running) await StopProxy(); else await StartProxy();
            NavigateHome(null, null);
        };

        var restartBtn = new Button 
        { 
            Content = "立即重启", 
            Style = (Style)FindResource("CapsuleButton"),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A0AEC0")),
            Width = 180,
            Margin = new Thickness(0,15,0,0),
            Visibility = _status == AppStatus.Running ? Visibility.Visible : Visibility.Collapsed
        };
        restartBtn.Click += async (s, ex) => {
            await StopProxy();
            await Task.Delay(1000);
            await StartProxy();
            NavigateHome(null, null);
        };

        panel.Children.Add(statusLabel);
        panel.Children.Add(configLabel);
        panel.Children.Add(startBtn);
        panel.Children.Add(restartBtn);

        MainContentFrame.Content = panel;
    }

    private async Task StartProxy()
    {
        if (PortService.IsPortOccupied(7890)) { MessageBox.Show("端口 7890 已被占用，请检查。"); return; }
        if (PortService.IsPortOccupied(9090)) { MessageBox.Show("端口 9090 已被占用，请检查。"); return; }

        string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "core", "mihomo.exe");
        bool needsDownload = !File.Exists(exePath);

        _status = AppStatus.Starting;
        NavigateHome(null, null);

        if (needsDownload)
        {
            // 这里可以添加一个 UI 提示正在下载
            LogService.LogApp("检测到核心缺失，正在从网络获取...");
        }

        if (await _mihomo.Start())
        {
            SystemProxyService.SetProxy("127.0.0.1:7890");
            _status = AppStatus.Running;
        }
        else
        {
            _status = AppStatus.Failed;
            MessageBox.Show("Mihomo 核心启动失败，请检查 core 文件夹及日志。");
        }
    }

    private async Task StopProxy()
    {
        _mihomo.Stop();
        SystemProxyService.UnsetProxy();
        _status = AppStatus.Stopped;
        await Task.CompletedTask;
    }

    private void NavigateConfig(object sender, RoutedEventArgs e)
    {
        var grid = new Grid { AllowDrop = true, Background = Brushes.Transparent };
        grid.Drop += (s, ev) => {
            if (ev.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])ev.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0) HandleImport(files[0]);
            }
        };

        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        
        var icon = new TextBlock 
        { 
            Text = "", 
            FontFamily = new FontFamily("Segoe MDL2 Assets"), 
            FontSize = 60, 
            Foreground = (SolidColorBrush)FindResource("BrandBlue"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0,0,0,20)
        };

        var tip = new TextBlock 
        { 
            Text = "点击导入或拖拽 YAML 配置文件到此处", 
            FontSize = 16, 
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A5568"))
        };

        var btn = new Button 
        { 
            Content = "选择文件", 
            Style = (Style)FindResource("CapsuleButton"), 
            Width = 140, 
            Margin = new Thickness(0,30,0,0) 
        };
        btn.Click += (s, ex) => {
            var ofd = new OpenFileDialog { Filter = "Clash YAML (*.yaml;*.yml)|*.yaml;*.yml" };
            if (ofd.ShowDialog() == true) HandleImport(ofd.FileName);
        };

        stack.Children.Add(icon);
        stack.Children.Add(tip);
        stack.Children.Add(btn);
        grid.Children.Add(stack);

        MainContentFrame.Content = grid;
    }

    private void HandleImport(string path)
    {
        var result = YamlConfigService.ValidateAndSave(path);
        if (result.success)
        {
            _currentConfigName = Path.GetFileName(path);
            MessageBox.Show(result.message, "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            NavHome.IsChecked = true;
        }
        else
        {
            MessageBox.Show(result.message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void NavigateProxies(object sender, RoutedEventArgs e)
    {
        if (_status != AppStatus.Running)
        {
            MainContentFrame.Content = new TextBlock 
            { 
                Text = "请先启动 Mihomo 核心以管理节点", 
                HorizontalAlignment = HorizontalAlignment.Center, 
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 18,
                Foreground = Brushes.Gray
            };
            return;
        }

        var groups = await _mihomo.GetProxyGroups();
        
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(10) };
        var stack = new StackPanel();

        foreach (var group in groups)
        {
            var groupCard = new Border 
            { 
                Background = Brushes.White, 
                CornerRadius = new CornerRadius(12), 
                Padding = new Thickness(15), 
                Margin = new Thickness(10),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                BorderThickness = new Thickness(1)
            };

            var innerStack = new StackPanel();
            innerStack.Children.Add(new TextBlock 
            { 
                Text = group.Name, 
                FontSize = 18, 
                FontWeight = FontWeights.Bold, 
                Margin = new Thickness(0,0,0,5) 
            });
            innerStack.Children.Add(new TextBlock 
            { 
                Text = $"类型: {group.Type} | 当前: {group.Now}", 
                FontSize = 12, 
                Foreground = Brushes.Gray, 
                Margin = new Thickness(0,0,0,15) 
            });

            var wrap = new WrapPanel();
            foreach (var node in group.All)
            {
                var isSelected = node == group.Now;
                var nodeBtn = new Button 
                { 
                    Content = node, 
                    Margin = new Thickness(0,0,8,8),
                    Padding = new Thickness(12,6,12,6),
                    Background = isSelected ? (SolidColorBrush)FindResource("BrandBlue") : Brushes.White,
                    Foreground = isSelected ? Brushes.White : Brushes.Black,
                    BorderBrush = (SolidColorBrush)FindResource("BrandBlue"),
                    BorderThickness = new Thickness(1),
                };
                
                // 设置圆角按钮模板
                nodeBtn.Template = (ControlTemplate)Resources["NodeButtonTemplate"] ?? CreateNodeButtonTemplate();

                nodeBtn.Click += async (s, ex) => {
                    if (await _mihomo.SwitchProxy(group.Name, node))
                    {
                        NavigateProxies(null, null);
                    }
                };
                wrap.Children.Add(nodeBtn);
            }
            innerStack.Children.Add(wrap);
            groupCard.Child = innerStack;
            stack.Children.Add(groupCard);
        }

        scroll.Content = stack;
        MainContentFrame.Content = scroll;
    }

    private ControlTemplate CreateNodeButtonTemplate()
    {
        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.Name = "border";
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
        
        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        content.SetValue(ContentPresenter.MarginProperty, new TemplateBindingExtension(Button.PaddingProperty));
        
        border.AppendChild(content);
        template.VisualTree = border;
        return template;
    }
}