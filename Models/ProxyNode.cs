namespace BlueGlassMihomoClient.Models;

public class ProxyNode
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Delay { get; set; } = "-";
    public bool IsSelected { get; set; }
}
