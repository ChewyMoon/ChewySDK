using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sophies_Soraka
{
    using LeagueSharp;
    using LeagueSharp.SDK.Core;
    using LeagueSharp.SDK.Core.Events;

    class Program
    {
        static void Main(string[] args)
        {
            Bootstrap.Init(null);

            Load.OnLoad += LoadOnLoad;
        }

        private static void LoadOnLoad(object sender, EventArgs eventArgs)
        {
            if (ObjectManager.Player.CharData.BaseSkinName == "Soraka")
            {
                new Soraka().Load();
            }
        }
    }
}
