using AutoMapper;
using FileAnalysis.API.Configuration;
using FileAnalysis.API.Controllers;
using FileAnalysis.BLL.Models;
using FileAnalysis.BLL.Services;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace FileAnalisys.API.Tests
{
    public class ScannerControllerTests
    {
        private Mock<IScannerService> _scannerService;
        private ScannerController _sut;
        private IMapper _autoMapper;

        [SetUp]
        public void Setup()
        {
            _scannerService = new Mock<IScannerService>();
            _autoMapper = new Mapper(new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperApiBll>()));
            _sut = new ScannerController(_scannerService.Object, _autoMapper);
        }

        // Positive tests
        [TestCase("http://example.com")]
        [TestCase("http://www.example.com")]
        [TestCase("https://example.com")]
        [TestCase("https://www.example.com")]
        public async Task ScanFile_ShouldScanFile(string url)
        {
            //given
            _scannerService
                .Setup(s => s.ScanFile(url))
                .ReturnsAsync(new ScanModel());
            
            //when
            await _sut.ScanFile(url);

            //then
            _scannerService.Verify(s => s.ScanFile(url), Times.Once());
        }

        //Negative tests with wrong URLs
        [TestCase("www.example.com")]
        [TestCase("not uri")]
        [TestCase("https:/www.example.com")]
        public async Task ScanFile_UriIsNotValid_ShouldThrowUriFormatException(string url)
        {
            //given
            var expected = $"url: '{url}' is not valid.";

            //when
            var actual = Assert
                .ThrowsAsync<UriFormatException>(async () => await _sut.ScanFile(url))!
                .Message;

            //then
            Assert.AreEqual(expected, actual);
        }
    }
}