using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace daleel_e_shop.BLL.Services.Files
{
    public interface IFileService
    {
        Task<string> SaveFileAsync(IFormFile file, string folderName);
        void DeleteFile(string fileUrl);
    }
}
