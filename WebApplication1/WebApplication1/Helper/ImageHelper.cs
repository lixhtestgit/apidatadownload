using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace WebApplication1.Helper
{
    public class ImageHelper
    {
        private HttpClient _httpClient;

        public ImageHelper(HttpClient httpClient)
        {
            this._httpClient = httpClient;
        }

        public async Task<bool> DownLoadImage(string imageUrl, string imageFileFullName)
        {
            bool result = false;
            if (File.Exists(imageFileFullName))
            {
                result = true;
            }
            else
            {
                using (Stream imageStream = await this._httpClient.GetStreamAsync(imageUrl))
                {
                    string fileDirectory = imageFileFullName.Substring(0, imageFileFullName.LastIndexOf('\\'));
                    if (!Directory.Exists(fileDirectory))
                    {
                        Directory.CreateDirectory(fileDirectory);
                    }
                    using (FileStream fs = new FileStream(imageFileFullName, FileMode.Create, FileAccess.ReadWrite))
                    {
                        byte[] buffer = new byte[8 * 1024];
                        int length = 0;
                        while ((length = imageStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fs.Write(buffer, 0, length);
                        }
                    }
                }
                result = true;
            }

            return result;
        }
    }
}
