using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Configuration;
using OneBot.CommandRoute.Models.Enumeration;
using OneBot.CommandRoute.Services;

using Sora.Entities;
using Sora.EventArgs.SoraEvent;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intallk.Config
{
    internal class IntallkConfig : IOneBotCommandRouteConfiguration
    {
        public string[] CommandPrefix => new[] { "." };

        public bool IsCaseSensitive => false;

        public static Uri DataPath => new(new(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)), "./Intallk");
    }
}
