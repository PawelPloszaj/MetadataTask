namespace FivetranClient.Models;

public class Connector
{
    public string Id { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string Schema { get; set; } = string.Empty;
    public bool? Paused { get; set; }
}