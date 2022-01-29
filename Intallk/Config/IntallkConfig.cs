using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Configuration;
using OneBot.CommandRoute.Models.Enumeration;
using OneBot.CommandRoute.Services;
using Sora.Entities;
using Sora.EventArgs.SoraEvent;

namespace Intallk.Config 
{
    class IntallkConfig : IOneBotCommandRouteConfiguration
    {
        public string[] CommandPrefix => new string[]{"."};

        public bool IsCaseSensitive => false;

        public static string DataPath => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Intallk";
    }
}
