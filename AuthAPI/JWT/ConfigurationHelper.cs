using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthAPI.JWT
{
    public static class ConfigurationHelper
    {
        public static IConfigurationRoot GetConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("jwtsettings.json");
            return builder.Build();
        }
    }
}