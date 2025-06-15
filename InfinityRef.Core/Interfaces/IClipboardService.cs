namespace InfinityRef.Core.Interfaces
{
    public interface IClipboardService
    {
        Task<string?> GetTextAsync();
        Task SetTextAsync(string text);
    }
}
