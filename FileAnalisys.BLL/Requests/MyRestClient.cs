using RestSharp;

namespace FileAnalisys.BLL.Requests
{
    public class MyRestClient : RestClient, IRestClient
    {
        public Task<RestResponse<T>> ExecuteAsync<T>(RestRequest request, CancellationToken cancellationToken = default) =>
                RestClientExtensions.ExecuteAsync<T>(this, request, cancellationToken);
    }
}
