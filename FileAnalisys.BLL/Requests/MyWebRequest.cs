using System.Net;

namespace FileAnalisys.BLL.Requests
{
    public class MyWebRequest : IWebRequestCreate
    {
        public WebRequest Create(Uri uri)
        {
            return WebRequest.Create(uri);
        }
    }
}
