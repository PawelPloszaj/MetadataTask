namespace FivetranClient.Models;

public class Data<T>
{
    public List<T> Items { get; set; } = new List<T>();
    public string? NextCursor { get; set; }
}