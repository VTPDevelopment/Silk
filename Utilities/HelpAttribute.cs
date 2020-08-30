﻿using System;

namespace SilkBot
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
    public class HelpAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
    public class HelpDescriptionAttribute : Attribute
    {
        public string Description { get; set; } = "";

        public string[] ExampleUsages { get; set; } = { };

        public HelpDescriptionAttribute(string desc, params string[] usages)
        {
            Description = desc;
            ExampleUsages = usages;
        }
    }
}