using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BlueprintEditor2
{
    /// <summary>
    /// Логика взаимодействия для Reporter.xaml
    /// </summary>
    public partial class Reporter : Window
    {
        public Reporter()
        {
            InitializeComponent();
            Who.Text = MySettings.Current.UserName;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Run.IsChecked.Value)
            {
                Process.Start(MyExtensions.AppFile);
                Application.Current.Shutdown();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //
            //Process.Start($"mailto:siptrixed@gmail.com?subject=CrashReport&body="+ HttpUtility.UrlEncode($"Sender:+{Who.Text}\n\nComment:\n{What.Text}\n{File.ReadAllText("LastCrash.txt")}\n\nSettings:\n{(File.Exists("config.xml") ? File.ReadAllText("config.xml") : "File not exists")}".Replace("\r", "").Replace("\n", "")));
            /* TODO: Make upload to issues or something
            var x = MyExtensions.ApiServer(ApiServerAct.Report, ApiServerOutFormat.@string,
                ",\"body\":\"" + ("Crash Report:" +
                "<br>Sender: " + Who.Text +
                "<br><br>Comment: <br>" + What.Text + //
                "<br><br>Log:<br>" + File.ReadAllText("LastCrash.txt") +
                "<br><br>PC: <br>" + GetPCInfo() +
                "<br><br>Settings: <br>"+(File.Exists("config.xml") ? File.ReadAllText("config.xml").Replace("<", "&lt;") : "")
                ).Replace("\n", "<br>").Replace("\r", "").Replace("\"", "'").Replace("\\", "\\\\") + "\"");*/
            Button_Click(this, null);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

    }
}
