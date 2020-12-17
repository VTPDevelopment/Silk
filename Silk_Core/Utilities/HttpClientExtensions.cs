﻿using System.Net.Http;

namespace SilkBot.Utilities
{
    public static class HttpClientExtensions
    {
        public static HttpClient CreateSilkClient(this IHttpClientFactory httpClientFactory)
        {
            return httpClientFactory.CreateClient(Program.HttpClientName);
        }
    }
}