using FileAnalisys.BLL.Exceptions;
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

        #region ScanFile tests
        [Test] 
        public async Task ScanFile_ResponseIsInMemory_ShouldReturnScanModel()
        {
            // given
            // Imitate work of WebRequest
            var url = "http://example.com";
            MockWebRequest(url);

            // Imitate Memory Cache
            var expected = "test answer";
            object expectedValue = expected;
            _mockMemoryCache.Setup(m => m.TryGetValue(It.IsAny<string>(), out expectedValue)).Returns(true);

            // when
            var actual = await _sut.ScanFile(url);
            
            // then
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual.Result);
            Assert.AreEqual("8c1f28fc2f48c271d6c498f0f249cdde365c54c5".ToUpper(), actual.SHA1); //result from online hash SHA1
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
            var expected = "test answer";
            var response = Mock.Of<RestResponse<string>>(_ => _.Data == expected && _.StatusCode == HttpStatusCode.OK);
            _mockRestClient.Setup(m => m.ExecuteAsync<string>(It.IsAny<RestRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

            // Imitate set in MemoryCache
            _mockMemoryCache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>);

            // when
            var actual = await _sut.ScanFile(url);

            // then
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual.Result);
            Assert.AreEqual("8c1f28fc2f48c271d6c498f0f249cdde365c54c5".ToUpper(), actual.SHA1); //result from online hash SHA1
            _mockWebRequestCreate.Verify(m => m.Create(new Uri(url)), Times.Once());
            _mockMemoryCache.Verify(m => m.CreateEntry(It.IsAny<object>()), Times.Once()); // Check that .Set was called
            _mockRestClient.Verify(m => m.ExecuteAsync<string>(It.IsAny<RestRequest>(), It.IsAny<CancellationToken>()), Times.Once()); // check that RestRequest was sent
        }

        [Test]
        public async Task ScanFile_FileSizeIsTooBig_ShouldThrowSizeException()
        {
            // given
            // Imitate work of WebRequest
            var url = "http://example.com";
            var moqHttpWebRequest = new Mock<WebRequest>();
            var moqHttpWebResponse = new Mock<WebResponse>();
            _mockWebRequestCreate.Setup(m => m.Create(new Uri(url))).Returns(moqHttpWebRequest.Object);
            moqHttpWebRequest.Setup(_ => _.GetResponse()).Returns(moqHttpWebResponse.Object);
            moqHttpWebResponse.Setup(m => m.Headers.Get("Content-Length")).Returns("210763776"); // mock request for file size
            var expected = "Size is too big";

            // when
            var actual = Assert
                .ThrowsAsync<SizeException>(async () => await _sut.ScanFile(url))!
                .Message;

            // then
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public async Task ScanFile_ResponseIsTimeout_ShouldThrowTimeoutException()
        {
            // given
            // Imitate work of WebRequest
            var url = "http://example.com";
            MockWebRequest(url);

            // Imitate work of RestSharp
            var expected = "test error";
            var response = Mock.Of<RestResponse<string>>(_ =>_.StatusCode == HttpStatusCode.RequestTimeout && _.ErrorException == new Exception(expected));
            _mockRestClient.Setup(m => m.ExecuteAsync<string>(It.IsAny<RestRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

            // Imitate set in MemoryCache
            _mockMemoryCache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>);

            // when
            var actual = Assert
                .ThrowsAsync<TimeoutException>(async () => await _sut.ScanFile(url))!
                .Message;

            // then
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public async Task ScanFile_ResponseIsNotOk_ShouldThrowServiceUnavailableException()
        {
            // given
            // Imitate work of WebRequest
            var url = "http://example.com";
            MockWebRequest(url);

            // Imitate work of RestSharp
            var expected = "test error";
            var response = Mock.Of<RestResponse<string>>(_ => _.StatusCode == HttpStatusCode.BadRequest && _.ErrorException == new Exception(expected));
            _mockRestClient.Setup(m => m.ExecuteAsync<string>(It.IsAny<RestRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

            // Imitate set in MemoryCache
            _mockMemoryCache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>);

            // when
            var actual = Assert
                .ThrowsAsync<ServiceUnavailableException>(async () => await _sut.ScanFile(url))!
                .Message;

            // then
            Assert.AreEqual(expected, actual);
        }
        #endregion

        #region GetContent tests
        [Test]
        public async Task GetContent_ShouldReturnContent()
        {
            // given
            // Imitate work of WebRequest
            var url = "http://example.com";
            var uri = new Uri(url);
            var expected = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
            var moqHttpWebRequest = new Mock<WebRequest>();
            var moqHttpWebResponse = new Mock<WebResponse>();
            _mockWebRequestCreate.Setup(m => m.Create(uri)).Returns(moqHttpWebRequest.Object);
            moqHttpWebRequest.Setup(_ => _.GetResponse()).Returns(moqHttpWebResponse.Object);
            moqHttpWebResponse.Setup(_ => _.GetResponseStream()).Returns(new MemoryStream(expected)); // mock request for stream
            moqHttpWebResponse.Setup(m => m.Headers.Get("Content-Length")).Returns("42"); // mock request for file size


            // when
            var actual = await _sut.GetContent(url);

            // then
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual);
            _mockWebRequestCreate.Verify(m => m.Create(new Uri(url)), Times.Once());
        }

        [Test]
        public async Task GetContent_FileSizeIsTooBig_ShouldThrowSizeException()
        {
            // given
            // Imitate work of WebRequest
            var url = "http://example.com";
            var moqHttpWebRequest = new Mock<WebRequest>();
            var moqHttpWebResponse = new Mock<WebResponse>();
            _mockWebRequestCreate.Setup(m => m.Create(new Uri(url))).Returns(moqHttpWebRequest.Object);
            moqHttpWebRequest.Setup(_ => _.GetResponse()).Returns(moqHttpWebResponse.Object);
            moqHttpWebResponse.Setup(m => m.Headers.Get("Content-Length")).Returns("210763776"); // mock request for file size
            var expected = "Size is too big";

            // when
            var actual = Assert
                .ThrowsAsync<SizeException>(async () => await _sut.GetContent(url))!
                .Message;

            // then
            Assert.AreEqual(expected, actual);
        }
        #endregion

        #region SendRequest tests
        [Test]
        public async Task SendRequest_ShouldReturnString()
        {
            // given
            // Imitate work of RestSharp
            var file = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
            var expected = "test answer";
            var response = Mock.Of<RestResponse<string>>(_ => _.Data == expected && _.StatusCode == HttpStatusCode.OK);
            _mockRestClient.Setup(m => m.ExecuteAsync<string>(It.IsAny<RestRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

            // when
            var actual = await _sut.SendRequest(file);

            // then
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual);
            _mockRestClient.Verify(m => m.ExecuteAsync<string>(It.IsAny<RestRequest>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Test]
        public async Task SendRequest_ResponseIsTimeout_ShouldThrowTimeoutException()
        {
            // given
            // Imitate work of RestSharp
            var file = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
            var expected = "test error";
            var response = Mock.Of<RestResponse<string>>(_ => _.StatusCode == HttpStatusCode.RequestTimeout && _.ErrorException == new Exception(expected));
            _mockRestClient.Setup(m => m.ExecuteAsync<string>(It.IsAny<RestRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

            // when
            var actual = Assert
                .ThrowsAsync<TimeoutException>(async () => await _sut.SendRequest(file))!
                .Message;

            // then
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public async Task SendRequest_ResponseIsNotOk_ShouldThrowServiceUnavailableException()
        {
            // given
            // Imitate work of WebRequest
            // Imitate work of RestSharp
            var file = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
            var expected = "test error";
            var response = Mock.Of<RestResponse<string>>(_ => _.StatusCode == HttpStatusCode.BadRequest && _.ErrorException == new Exception(expected));
            _mockRestClient.Setup(m => m.ExecuteAsync<string>(It.IsAny<RestRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

            // Imitate set in MemoryCache
            _mockMemoryCache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>);

            // when
            var actual = Assert
                .ThrowsAsync<ServiceUnavailableException>(async () => await _sut.SendRequest(file))!
                .Message;

            // then
            Assert.AreEqual(expected, actual);
        }
        #endregion

        #region CheckCacheBeforeSend tests
        [Test]
        public async Task CheckCacheBeforeSend_ResponseIsInMemory_ShouldReturnString()
        {
            // given
            // Imitate Memory Cache
            var file = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
            var expected = "test answer";
            object expectedValue = expected;
            _mockMemoryCache.Setup(m => m.TryGetValue(It.IsAny<string>(), out expectedValue)).Returns(true);

            // when
            var actual = await _sut.CheckCacheBeforeSend(It.IsAny<string>(), file);

            // then
            Assert.AreEqual(expected, actual);
            _mockMemoryCache.Verify(m => m.TryGetValue(It.IsAny<string>(), out expectedValue), Times.Once());
            _mockMemoryCache.Verify(m => m.CreateEntry(It.IsAny<object>()), Times.Never()); // Check that .Set was not called
            _mockRestClient.Verify(m => m.ExecuteAsync<string>(It.IsAny<RestRequest>(), It.IsAny<CancellationToken>()), Times.Never()); // check that RestRequest wasn't sent
        }

        [Test]
        public async Task CheckCacheBeforeSend_ResponseIsNotInMemory_ShouldReturnString()
        {
            // given
            // Imitate work of RestSharp
            var file = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
            var expected = "test answer";
            var response = Mock.Of<RestResponse<string>>(_ => _.Data == expected && _.StatusCode == HttpStatusCode.OK);
            _mockRestClient.Setup(m => m.ExecuteAsync<string>(It.IsAny<RestRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

            // Imitate set in MemoryCache
            _mockMemoryCache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>);

            // when
            var actual = await _sut.CheckCacheBeforeSend(It.IsAny<string>(), file);

            // then
            Assert.AreEqual(expected, actual);
            _mockMemoryCache.Verify(m => m.CreateEntry(It.IsAny<object>()), Times.Once()); // Check that .Set was called
            _mockRestClient.Verify(m => m.ExecuteAsync<string>(It.IsAny<RestRequest>(), It.IsAny<CancellationToken>()), Times.Once()); // check that RestRequest was sent
        }

        [Test]
        public async Task CheckCacheBeforeSend_ResponseIsTimeout_ShouldThrowTimeoutException()
        {
            // given
            // Imitate work of RestSharp
            var file = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
            var expected = "test error";
            var response = Mock.Of<RestResponse<string>>(_ => _.StatusCode == HttpStatusCode.RequestTimeout && _.ErrorException == new Exception(expected));
            _mockRestClient.Setup(m => m.ExecuteAsync<string>(It.IsAny<RestRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

            // Imitate set in MemoryCache
            _mockMemoryCache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>);

            // when
            var actual = Assert
                .ThrowsAsync<TimeoutException>(async () => await _sut.CheckCacheBeforeSend(It.IsAny<string>(), file))!
                .Message;

            // then
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public async Task CheckCacheBeforeSend_ResponseIsNotOk_ShouldThrowServiceUnavailableException()
        {
            // given
            // Imitate work of RestSharp
            var file = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
            var expected = "test error";
            var response = Mock.Of<RestResponse<string>>(_ => _.StatusCode == HttpStatusCode.BadRequest && _.ErrorException == new Exception(expected));
            _mockRestClient.Setup(m => m.ExecuteAsync<string>(It.IsAny<RestRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

            // Imitate set in MemoryCache
            _mockMemoryCache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>);

            // when
            var actual = Assert
                .ThrowsAsync<ServiceUnavailableException>(async () => await _sut.CheckCacheBeforeSend(It.IsAny<string>(), file))!
                .Message;

            // then
            Assert.AreEqual(expected, actual);
        }
        #endregion

        [TestCase(new byte[] { 1, 2, 3, 4, 5, 6, 7 }, "8c1f28fc2f48c271d6c498f0f249cdde365c54c5")]
        [TestCase(new byte[] { (byte)'t', (byte)'e', (byte)'s', (byte)'t' }, "a94a8fe5ccb19ba61c4c0873d391e987982fbbd3")]
        public void Hash_ShouldReturnSha1(byte[] file, string expected)
        {
            //when
            var actual = _sut.Hash(file);

            //then
            Assert.AreEqual(expected.ToUpper(), actual);
        }
    }
}