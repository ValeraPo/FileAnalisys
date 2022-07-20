using FileAnalisys.BLL.Exceptions;
using FileAnalysis.BLL.Models;
using RestSharp;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace FileAnalysis.BLL.Services
{
    public class ScannerService : IScannerService
    {
        public ScanModel ScanFile(string url)
        {
            var content = GetContent(url);
            var scanModel = new ScanModel();
            scanModel.SHA1 = Hash(content); // hashing file
            scanModel.Result = SendRequest(content); // virus scanning 
            return scanModel;
        }

        // Parsing file
        public string GetContent(string url)
        {
            var webRequest = WebRequest.Create(url);
            webRequest.Timeout = 36000;

            using (var response = webRequest.GetResponse())
            {
                var fileSize = response.Headers.Get("Content-Length");
                var fileSizeInMegaByte = Math.Round(Convert.ToDouble(fileSize) / 1024.0 / 1024.0, 2);
                // Check if size is not very big
                if (fileSizeInMegaByte > 200)
                    throw new FormatException("Size is too big");
                using var content = response.GetResponseStream();
                using var reader = new StreamReader(content);
                return reader.ReadToEnd();
            }
        }

        //Hashing file
        public string Hash(string input)
        {
            using var sha1 = SHA1.Create();
            return Convert.ToHexString(sha1.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }


        // Sending request to scan virus
        public string SendRequest(string content)
        {
            var client = new RestClient("ServiceUrl"); // TODO add real link
            var request = new RestRequest("/resource/", Method.Post);
            request.AddParameter("file", content);
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
