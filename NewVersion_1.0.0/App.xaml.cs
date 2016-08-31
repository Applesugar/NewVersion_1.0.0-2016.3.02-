using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace NewVersion_1._0._0
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private static bool isPathIn = false;
        private static string UserPath = "";
        private static string UserInfor = "";

        private void Application_StartUp(object sender, StartupEventArgs e)
        {
            if (e.Args.Length != 0)
            {
                isPathIn = true;
                UserPath = e.Args[0];
                UserInfor = e.Args[1];
            }
        }

        public static string GetPath()
        {
            if (isPathIn)
            {
                return UserPath;
            }
            else
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        public static string GetInfo()
        {
            if (isPathIn)
            {
                return UserInfor;
            }
            else
            {
                return "";
            }
        }
    }
}
