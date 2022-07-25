using AutoMapper;
using FileAnalisys.API.Infrastructure;
using FileAnalisys.API.Models;
using FileAnalisys.BLL.Exceptions;
using FileAnalysis.API.Configuration;
using FileAnalysis.API.Controllers;
using FileAnalysis.BLL.Models;
using FileAnalysis.BLL.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileAnalisys.API.Tests
{
    public class ErrorExceptionMiddlewareTests
    {
        private DefaultHttpContext _defaultContext;
        private const string ExceptionMassage = "Exception massage";

        [SetUp]
        public void Setup()
        {
            _defaultContext = new DefaultHttpContext
            {
                Response = { Body = new MemoryStream() },
                Request = { Path = "/" }
            };
        }

        // Check when everything is ok
        [Test]
        public void Invoke_ValidRequestReceived_ShouldResponse()
        {
            //given
            const string expectedOutput = "Request handed over to next request delegate";
            var middlewareInstance = new ErrorExceptionMiddleware(innerHttpContext =>
            {
                innerHttpContext.Response.WriteAsync(expectedOutput);
                return Task.CompletedTask;
            });

            //when
            middlewareInstance.Invoke(_defaultContext);

            //then
            var actual = GetResponseBody();
            Assert.AreEqual(expectedOutput, actual);
        }

        // Check if FormatException, should be Status Code 400
        [Test]
        public void Invoke_WhenThrowFormatException_ShouldExceptionResponseModel()
        {
            //given
            var expected = GetJsonExceptionResponseModel(400);
            var middlewareInstance = new ErrorExceptionMiddleware(_ => throw new FormatException(ExceptionMassage));

            //when
            middlewareInstance.Invoke(_defaultContext);

            //then
            var actual = GetResponseBody();
            Assert.AreEqual(expected, actual);
        }

        // Check if ServiceUnavailableException, should be Status Code 503
        [Test]
        public void Invoke_WhenThrowServiceUnavailableException_ShouldExceptionResponseModel()
        {
            //given
            var expected = GetJsonExceptionResponseModel(503);
            var middlewareInstance = new ErrorExceptionMiddleware(_ => throw new ServiceUnavailableException(ExceptionMassage));

            //when
            middlewareInstance.Invoke(_defaultContext);

            //then
            var actual = GetResponseBody();
            Assert.AreEqual(expected, actual);
        }

        // Check if TimeoutException, should be Status Code 504
        [Test]
        public void Invoke_WhenThrowTimeoutException_ShouldExceptionResponseModel()
        {
            //given
            var expected = GetJsonExceptionResponseModel(504);
            var middlewareInstance = new ErrorExceptionMiddleware(_ => throw new TimeoutException(ExceptionMassage));

            //when
            middlewareInstance.Invoke(_defaultContext);

            //then
            var actual = GetResponseBody();
            Assert.AreEqual(expected, actual);
        }

        // Check if other Exceptions, should be Status Code 500
        [Test]
        public void Invoke_WhenOtheException_ShouldExceptionResponseModel()
        {
            //given
            var expected = GetJsonExceptionResponseModel(500);
            var middlewareInstance = new ErrorExceptionMiddleware(_ => throw new Exception(ExceptionMassage));

            //when
            middlewareInstance.Invoke(_defaultContext);

            //then
            var actual = GetResponseBody();
            Assert.AreEqual(expected, actual);
        }

        private string GetResponseBody()
        {
            _defaultContext.Response.Body.Seek(0, SeekOrigin.Begin);
            return new StreamReader(_defaultContext.Response.Body).ReadToEnd();
        }

        private static string GetJsonExceptionResponseModel(int statusCode) =>
            JsonSerializer.Serialize(new ExceptionResponse { Code = statusCode, Message = ExceptionMassage });

    }
}