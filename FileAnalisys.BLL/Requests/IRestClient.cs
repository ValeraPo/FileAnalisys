using RestSharp;

namespace FileAnalisys.BLL.Requests
{
    public interface IRestClient
    {
        Task<RestResponse<T>> ExecuteAsync<T>(RestRequest request, CancellationToken cancellationToken = default(CancellationToken));
    }
}
