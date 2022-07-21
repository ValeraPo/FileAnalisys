using FileAnalisys.BLL.Exceptions;
using FileAnalysis.BLL.Models;
using RestSharp;
using System.Net;
using System.Security.Cryptography;

namespace FileAnalysis.BLL.Services
{
    public class ScannerService : IScannerService
    {
        public ScanModel ScanFile(string url)
        {
            var scanModel = new ScanModel();
            var content = GetContent(url);
            scanModel.SHA1 = Hash(content); // hashing file
            scanModel.Result = SendRequest(content); // virus scanning 
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
            var client = new RestClient("https://localhost:7030");
            var request = new RestRequest("/api/process/", Method.Post);

            request.AddQueryParameter("chunks", content.ToString());
            var response = client.Execute<string>(request);

            // Check response
            if (response.StatusCode == HttpStatusCode.OK)
                return response.Data;

            if (response.StatusCode == HttpStatusCode.RequestTimeout)
                throw new TimeoutException(response.ErrorException.Message);

            throw new ServiceUnavailableException(response.ErrorException.Message);
        }



    }
}
