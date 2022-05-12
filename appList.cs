using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SteamInfoGetter
{
    class AppList
    {
        public int AppId {  get; set; }

        public string AppName { get; set; }

        public AppList(int appId, string appName)
        {
            this.AppId = appId;
            this.AppName = appName;
        }
    }
}
