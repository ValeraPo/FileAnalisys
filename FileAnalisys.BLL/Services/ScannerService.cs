using FileAnalisys.BLL.Exceptions;
using FileAnalysis.BLL.Models;
using RestSharp;
using System.Net;
using System.Runtime.Caching;
using System.Security.Cryptography;

namespace FileAnalysis.BLL.Services
{
    public class ScannerService : IScannerService
    {
        private ObjectCache _cache = MemoryCache.Default;

        public ScanModel ScanFile(string url)
        {
            var scanModel = new ScanModel();
            var content = GetContent(url);
            scanModel.SHA1 = Hash(content); // hash file
            scanModel.Result = CheckCacheBeforeSend(scanModel.SHA1, content); // scan file

            return scanModel;
        }

        // Parsing file
        public byte[] GetContent(string url)
        {
            var webRequest = WebRequest.Create(url);
            webRequest.Timeout = 36000;

            using (var response = webRequest.GetResponse())
            {
                var fileSize = long.Parse(response.Headers.Get("Content-Length"));
                var fileSizeInMegaByte = Math.Round(fileSize / 1024.0 / 1024.0, 2);
                // Check if size is not very big
                if (fileSizeInMegaByte > 200)
                    throw new FormatException("Size is too big");

                using (Stream content = response.GetResponseStream())
                {
                    byte[] buffer = new byte[32768]; //set the size of buffer (chunk)
                    using (MemoryStream ms = new MemoryStream()) 
                    {
                        while (true) //loop to the end of the file
                        {
                            // I chose '.ReadAsync' because I measured time that both functions required and
                            // '.ReadAsync' works faster than '.Read'
                            int read = content.ReadAsync(buffer, 0, buffer.Length).Result; //read each chunk
                            if (read <= 0) //check for end of file
                                return ms.ToArray();
                            ms.Write(buffer, 0, read); 
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
        public string SendRequest(byte[] content)
        {
            // create request
            var client = new RestClient("https://localhost:7030");
            var request = new RestRequest("/api/process/", Method.Post);
            request.AddBody(content);
            // get response
            var response = client.Execute<string>(request);

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
        public string CheckCacheBeforeSend(string hashKey, byte[] file)
        {
            // if we have analised this file, we will not send a request
            string scanResult;
            var cacheItem = _cache.GetCacheItem(hashKey);
            if (cacheItem is null)
            {
                scanResult = SendRequest(file); // virus scanning 
                _cache.Add(hashKey, scanResult, null); // save result in memory cache
            }
            else
                // get result from past scanning 
                scanResult = (string)cacheItem.Value;
            return scanResult;
        }

    }
}
