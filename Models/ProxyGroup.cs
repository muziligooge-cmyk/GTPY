using System.Collections.Generic;

namespace BlueGlassMihomoClient.Models;

public class ProxyGroup
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Now { get; set; } = string.Empty;
    public List<string> All { get; set; } = new();
}
