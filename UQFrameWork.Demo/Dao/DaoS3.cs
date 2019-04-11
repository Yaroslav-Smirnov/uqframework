using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UQFramework.Attributes;
using UQFramework.DAO;

namespace UQFrameWork.Demo.Dao
{
    [DaoVersion("1.1.0.0")]
    internal class DaoS3 : IDataSourceReader<Entity>, IDataSourceEnumerator<Entity>, IDataSourceWriter<Entity>
    {
        public Entity GetEntity(string identifier)
        {
            var data = Task.Run(() => GetFileContent($"_TestStorage/{identifier}.json")).Result;
            if (data == null)
                return null;
            return JsonConvert.DeserializeObject<Entity>(data);
        }

        public void AddEntity(Entity entity)
        {
            var content = JsonConvert.SerializeObject(entity);
            var task = Task.Run(() => WriteFileContent($"_TestStorage/{entity.Identifier}.json", content));
            task.Wait();
        }

        public void UpdateEntity(Entity entity)
        {
            AddEntity(entity);
        }

        public void DeleteEntity(Entity entity)
        {
            var task = Task.Run(() => DeleteFile($"_TestStorage/{entity.Identifier}.json"));
            task.Wait();
        }
        public IEnumerable<string> GetAllEntitiesIdentifiers()
        {
            var task = Task.Run(() => GetFileList("_TestStorage"));
            var list = task.Result;
            return list.Select(Path.GetFileNameWithoutExtension).ToList();

            //return Enumerable.Range(0, 100000).Select(i => i.ToString());
        }

        public int Count()
        {
            return GetAllEntitiesIdentifiers().Count();
        }

        #region S3

        static DaoS3()
        {
            ServicePointManager.DefaultConnectionLimit = 1000;
        }

        private const string _fileApiUrl = "https://leapdesignweb-test.leapaws.com.au/api/S3/File";
        private static async Task<string> GetFileContent(string pathToFile)
        {
            var fileName = Path.GetFileName(pathToFile);
            var folder = GetPathWithoutFileName(pathToFile);

            using (var client = new HttpClientWithAuth())
            {
                var urlWithQueryString = $"{_fileApiUrl}?fileName={pathToFile}";

                var response = await client.GetAsync(urlWithQueryString);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    return await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                throw new Exception($"Failed to download {fileName}.");
            }
        }

        private static async Task WriteFileContent(string pathToFile, string content)
        {
            using (HttpClientWithAuth client = new HttpClientWithAuth())
            {
                var url = $"{_fileApiUrl}api/S3/File";

                var form = new MultipartFormDataContent
                {
                    { new StringContent(pathToFile), "fileName" },

                    //{ new ByteArrayContent(Encoding.UTF8.GetBytes(content)), "file", pathToFile }
                    { new StringContent(content), "file", pathToFile }
                };

                var response = await client.PostAsync(_fileApiUrl, form);
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception($"Failed to upload {pathToFile}.");
                }
            }
        }

        private static async Task DeleteFile(string pathToFile)
        {
            var fileName = Path.GetFileName(pathToFile);
            var folder = GetPathWithoutFileName(pathToFile);

            using (var client = new HttpClientWithAuth())
            {
                var urlWithQueryString = $"{_fileApiUrl}?fileName={pathToFile}";

                var response = await client.DeleteAsync(urlWithQueryString);

                if (!response.IsSuccessStatusCode )
                {
                    throw new Exception($"Failed to delete {pathToFile}.");
                }
            }
        }

        private static async Task<List<string>> GetFileList(string folder)
        {
            using (var client = new HttpClientWithAuth())
            {
                var urlWithQueryString = $"{_fileApiUrl}/List?directory={folder}";

                var response = await client.GetAsync(urlWithQueryString);

                if (response.StatusCode == HttpStatusCode.OK)
                    return JsonConvert.DeserializeObject<List<string>>(await response.Content.ReadAsStringAsync());

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;

                throw new Exception($"Failed to get list of files in {folder}.");
            }
        }

        private static string GetPathWithoutFileName(string pathToFile)
        {
            return Path.GetDirectoryName(pathToFile).Replace('\\', '/');
        }

        private class HttpClientWithAuth : HttpClient
        {

            public static string UserName { private get; set; } = "yaroslav.smirnov@leapdev.io";
            public static string Password { private get; set; } = "yaroslaV18";

            public HttpClientWithAuth() : base()
            {
                this.AddAuthorizationHeader();
            }

            public HttpClientWithAuth(HttpMessageHandler handler) : base(handler)
            {
                this.AddAuthorizationHeader();
            }

            public HttpClientWithAuth(HttpMessageHandler handler, bool disposeHandler) : base(handler, disposeHandler)
            {
                this.AddAuthorizationHeader();
            }

            private void AddAuthorizationHeader()
            {
                string authToken = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes($"{UserName}:{Password}"));
                this.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            }

            public async Task<HttpResponseMessage> PatchAsync(string requestUri, HttpContent content)
            {
                var method = new HttpMethod("PATCH");

                var request = new HttpRequestMessage(method, requestUri)
                {
                    Content = content
                };

                HttpResponseMessage response = new HttpResponseMessage();

                response = await this.SendAsync(request);
                return response;
            }
        }

        #endregion
    }
}
