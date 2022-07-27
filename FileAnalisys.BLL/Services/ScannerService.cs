using FileAnalisys.BLL.Exceptions;
using FileAnalisys.BLL.Requests;
using FileAnalysis.BLL.Models;
using Microsoft.Extensions.Caching.Memory;
using RestSharp;
using System.Net;
using System.Security.Cryptography;

namespace FileAnalysis.BLL.Services
{
    public class ScannerService : IScannerService
    {
        private IWebRequestCreate _webRequest;
        private IRestClient _restClient;
        private IMemoryCache _memoryCache;

        public ScannerService(IWebRequestCreate webRequest, IRestClient restClient, IMemoryCache memoryCache)
        {
            _webRequest = webRequest;
            _restClient = restClient;
            _memoryCache = memoryCache;
        }

        public async Task<ScanModel> ScanFile(string url)
        {
            var scanModel = new ScanModel();
            var content = await GetContent(url);
            scanModel.SHA1 = Hash(content); // hash file
            scanModel.Result = await CheckCacheBeforeSend(scanModel.SHA1, content); // scan file

            return scanModel;
        }

        // Parsing file
        public async Task<byte[]> GetContent(string url)
        {
            var webRequest = _webRequest.Create(new Uri(url));
            webRequest.Timeout = 36000;

            using (var response = webRequest.GetResponse())
            {
                var fileSize = long.Parse(response.Headers.Get("Content-Length"));
                var fileSizeInMegaByte = Math.Round(fileSize / 1024.0 / 1024.0, 2);
                // Check if size is not very big
                if (fileSizeInMegaByte > 200)
                    throw new SizeException("Size is too big");

                using (Stream content = response.GetResponseStream())
                {
                    byte[] buffer = new byte[32768]; //set the size of buffer (chunk) 4KB
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        while (true) //loop to the end of the file
                        {
                            // I chose '.ReadAsync' because I measured time that both functions required and
                            // '.ReadAsync' works faster than '.Read'
                            int read = await content.ReadAsync(buffer, 0, buffer.Length); //read each chunk
                            if (read <= 0) //check for end of file
                                return memoryStream.ToArray();
                            memoryStream.Write(buffer, 0, read);
                        }
                    }
                }
            }
        }

        //Hashing file
        public string Hash(byte[] input)
        {
            using var sha1 = SHA1.Create();
            return Convert.ToHexString(sha1.ComputeHash(input));
        }

        // Sending request to scan virus
        public async Task<string> SendRequest(byte[] content)
        {
            var request = new RestRequest("https://localhost:7030/api/process/", Method.Post);
            request.AddBody(content);
            // get response
            var response = await _restClient.ExecuteAsync<string>(request);

            // Check response
            if (response.StatusCode == HttpStatusCode.OK)
                return response.Data;

            if (response.StatusCode == HttpStatusCode.RequestTimeout)
                throw new TimeoutException(response.ErrorException.Message);

            throw new ServiceUnavailableException(response.ErrorException.Message);
        }

        // Searching or saving to cache memory
        // **I understand that it may not fulfill the Single Responsibility principle
        // **but I think that Send Request should be next to the saving result of scanning in memory
        public async Task<string> CheckCacheBeforeSend(string hashKey, byte[] file)
        {
            string scanResult;

            // if we have analised this file, we will not send a request
            if (!_memoryCache.TryGetValue(hashKey, out scanResult))
            {
                scanResult = await SendRequest(file); // virus scanning 
                _memoryCache.Set(hashKey, scanResult);
            }

            return scanResult;
        }

    }
}
