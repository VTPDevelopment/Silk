﻿using System;

namespace SilkBot.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static T Get<T>(this IServiceProvider provider) => (T)provider.GetService(typeof(T));

        //TODO: Add this. //

        // no ~Velvet //
        public static void RegisterArgumentConverters(this IServiceProvider provider) {}
    }
}
