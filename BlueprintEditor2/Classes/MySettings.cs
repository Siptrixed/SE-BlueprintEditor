﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace BlueprintEditor2
{
    [DataContract(Name = "SE_BlueprintEditor2_Settings")]
    public class MySettings
    {
        private const string FILE_PATH = "settings.xml";

        public static MySettings Current = new MySettings();
        [DataMember(Name = "BlueprintMainFolder")]
        public string BlueprintPatch = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\SpaceEngineers\Blueprints\local\";
        [DataMember(Name = "GameFolder")]
        public string GamePatch = null;
        [DataMember(Name = "UseMultipleWindows")]
        public bool MultiWindow = false;
        [DataMember(Name = "DontOpenBlueprintsOnScan")]
        public bool DOBS = false;
        [DataMember(Name = "LangCultureID")]
        public int LCID = 0;
        MySettings()
        {
            LCID = Thread.CurrentThread.CurrentUICulture.LCID;
            if (Directory.Exists(@"C:\Program Files (x86)\Steam\SteamApps\common\SpaceEngineers"))
                GamePatch = @"C:\Program Files (x86)\Steam\SteamApps\common\SpaceEngineers\";
        }
        public void ApplySettings()
        {
            if(LCID != 0) Thread.CurrentThread.CurrentUICulture = new CultureInfo(LCID);
        }
        public static void Serialize()
        {
            DataContractSerializer formatter = new DataContractSerializer(typeof(MySettings));
            //new StreamWriter("settings.xml")
            using (Stream fs = new FileStream(FILE_PATH, FileMode.Create))
            {
                formatter.WriteObject(fs, Current);
            }
        }
        public static void Deserialize()
        {
            if (File.Exists("settings.xml"))
            {
                DataContractSerializer formatter = new DataContractSerializer(typeof(MySettings));
                using (Stream fs = new FileStream(FILE_PATH, FileMode.Open))
                {
                    Current = (MySettings)formatter.ReadObject(fs);
                }
            }
        }
    }
}
