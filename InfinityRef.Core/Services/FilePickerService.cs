using InfinityRef.Core.Interfaces;

namespace InfinityRef.Core.Services
{
    public class FilePickerService : IFilePicker
    {
        public Task<string?> PickFileAsync(string[] allowedExtensions)
        {
            throw new NotImplementedException();
        }

        public Task SaveFileAsync(string fileName, Stream fileStream)
        {
            throw new NotImplementedException();
        }
    }
}
