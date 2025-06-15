namespace InfinityRef.Core.Interfaces
{
    public interface IFilePicker
    {
        Task<string?> PickFileAsync(string[] allowedExtensions);
        Task SaveFileAsync(string fileName, Stream fileStream);
    }
}
