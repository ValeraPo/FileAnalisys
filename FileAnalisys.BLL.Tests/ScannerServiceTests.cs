using FileAnalisys.BLL.Requests;
using FileAnalysis.BLL.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using NUnit.Framework;
using RestSharp;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FileAnalisys.BLL.Tests
{
    public class ScannerServiceTests
    {
        private ScannerService _sut;
        private Mock<IWebRequestCreate> _mockWebRequestCreate;
        private Mock<IRestClient> _mockRestClient;
        private Mock<IMemoryCache> _mockMemoryCache;


        [SetUp]
        public void Setup()
        {
            _mockWebRequestCreate = new Mock<IWebRequestCreate>();
            _mockRestClient = new Mock<IRestClient>();
            _mockMemoryCache = new Mock<IMemoryCache>();
            _sut = new ScannerService(_mockWebRequestCreate.Object, _mockRestClient.Object, _mockMemoryCache.Object);
        }

        // Imitate work of WebRequest
        private void MockWebRequest(string url)
        {
            var resultContentBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
            var moqHttpWebRequest = new Mock<WebRequest>();
            var moqHttpWebResponse = new Mock<WebResponse>();
            _mockWebRequestCreate.Setup(m => m.Create(new Uri(url))).Returns(moqHttpWebRequest.Object);
            moqHttpWebRequest.Setup(_ => _.GetResponse()).Returns(moqHttpWebResponse.Object);
            moqHttpWebResponse.Setup(_ => _.GetResponseStream()).Returns(new MemoryStream(resultContentBytes)); // mock request for stream
            moqHttpWebResponse.Setup(m => m.Headers.Get("Content-Length")).Returns("42"); // mock request for file size
        }

        [Test] 
        public async Task ScanFile_ResponseIsInMemory_ShouldReturnScanModel()
        {
            // given
            // Imitate work of WebRequest
            var url = "http://example.com";
            MockWebRequest(url);

            // Imitate Memory Cache
            var data = "test answer";
            object expectedValue = data;
            _mockMemoryCache.Setup(m => m.TryGetValue(It.IsAny<string>(), out expectedValue)).Returns(true);

            // when
            var actual = await _sut.ScanFile(url);
            
            // then
            Assert.IsNotNull(actual);
            Assert.AreEqual(actual.Result, data);
            Assert.AreEqual(actual.SHA1, "8c1f28fc2f48c271d6c498f0f249cdde365c54c5".ToUpper()); //result from online hash SHA1
            _mockWebRequestCreate.Verify(m => m.Create(new Uri(url)), Times.Once());
            _mockMemoryCache.Verify(m => m.TryGetValue(It.IsAny<string>(), out expectedValue), Times.Once());
            _mockMemoryCache.Verify(m => m.CreateEntry(It.IsAny<object>()), Times.Never()); // Check that .Set was not called
            _mockRestClient.Verify(m => m.ExecuteAsync<string>(It.IsAny<RestRequest>(), It.IsAny<CancellationToken>()), Times.Never()); // check that RestRequest wasn't sent
        }

        [Test]
        public async Task ScanFile_ResponseIsNotInMemory_ShouldReturnScanModel()
        {
            // given
            // Imitate work of WebRequest
            var url = "http://example.com";
            MockWebRequest(url);

            // Imitate work of RestSharp
            var data = "test answer";
            var expected = Mock.Of<RestResponse<string>>(_ => _.Data == data && _.StatusCode == HttpStatusCode.OK);
            _mockRestClient.Setup(m => m.ExecuteAsync<string>(It.IsAny<RestRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(expected);

            // Imitate set in MemoryCache
            _mockMemoryCache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>);

            // when
            var actual = await _sut.ScanFile(url);

            // then
            Assert.IsNotNull(actual);
            Assert.AreEqual(actual.Result, data);
            Assert.AreEqual(actual.SHA1, "8c1f28fc2f48c271d6c498f0f249cdde365c54c5".ToUpper()); 
            _mockWebRequestCreate.Verify(m => m.Create(new Uri(url)), Times.Once());
            _mockMemoryCache.Verify(m => m.CreateEntry(It.IsAny<object>()), Times.Once()); // Check that .Set was called
            _mockRestClient.Verify(m => m.ExecuteAsync<string>(It.IsAny<RestRequest>(), It.IsAny<CancellationToken>()), Times.Once()); // check that RestRequest was sent
        }

    }
}