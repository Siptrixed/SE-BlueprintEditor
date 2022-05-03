using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.IO;
using BlueprintEditor2.Resource;
using System.Diagnostics;
using Path = System.IO.Path;
using System.ComponentModel;
using System.Net;
using System.Threading;
using System.Text;
using System.Globalization;
using System.Resources;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace BlueprintEditor2
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class SelectBlueprint : Window
    {
        public static SelectBlueprint window;
        internal MyXmlBlueprint CurrentBlueprint;
        string currentBluePatch;

        public SelectBlueprint()
        {
#if DEBUG
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
#endif
            Thread.CurrentThread.Name = "Main";
            MySettings.Deserialize();
            new Task(() => {
                ArmorReplaceClass.Deserialize();
            }).Start();
            MySettings.Current.ApplySettings();
            window = this;
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                foreach (var arg in args)
                {
                    switch (arg)
                    {
                        case "Crash":
#if DEBUG
                            Application.Current.Shutdown();
                            return;
#else
                            new Reporter().Show();
                            Hide();
                            return;
#endif
                        case "-debug":
                            ConsoleManager.Show();
                            break;
                    }
                }
            }
            Logger.HandleUnhandledException();

            InitializeComponent();
            currentBluePatch = MySettings.Current.BlueprintPatch;
            InitBlueprints();
#if false
#else
            new Task(() =>
            {
                Thread.CurrentThread.Name = "Updating";
                string downloadURL = null, lastVersion, git_ingo;
                using (var client = new System.Net.WebClient())
                {
                    client.Headers.Add("User-Agent", "SE-BlueprintEditor");
                    client.Encoding = Encoding.UTF8;
                    git_ingo = client.DownloadString("https://api.github.com/repos/Siptrixed/SE-BlueprintEditor/releases");
                    lastVersion = MyExtensions.RegexMatch(git_ingo, @"""tag_name"":""([^""]*)""");
                    downloadURL = MyExtensions.RegexMatch(git_ingo, @"""browser_download_url"":""([^""]*)""");
                }
                if (!string.IsNullOrEmpty(downloadURL))
                {
                    if (MyExtensions.CheckVersion(lastVersion, MyExtensions.Version))
                    {
                        MyExtensions.AsyncWorker(() => new UpdateAvailable(lastVersion, downloadURL, git_ingo).Show());
                    }
                    else
                    {
                    }
                }
                else
                {
                }
            }).Start();
#endif
            OldSortBy = FirstSorter;
            if (string.IsNullOrWhiteSpace(MySettings.Current.SteamWorkshopPatch))
            {
                MenuItem_1_1.IsEnabled = false;
                MenuItem_2_1.IsEnabled = false;
                MenuItem_1_2.IsEnabled = false;
                MenuItem_2_2.IsEnabled = false;
                MenuItem_1_3.IsEnabled = false;
                MenuItem_2_3.IsEnabled = false;
            }
            Random rnd = new Random();
            Welcome.Content = Lang.Welcome+" "+ (MyExtensions.CheckGameLicense() ?MySettings.Current.UserName.Replace("_", "__") : "Pirate#"+ rnd.Next(90,9999));
            //MessageBox.Show("Hello "+MySettings.Current.UserName);
            new Task(() => {
                Thread.CurrentThread.Name = "DataParser";
                MyGameData.Init();
                MyExtensions.AsyncWorker(() => {
                    CalculateButton.IsEnabled = BlueList.SelectedIndex != -1;
                    CalculateButton.Content = Lang.Calculate;
                });
            }).Start();
        }
        public void SettingsUpdated()
        {
            Welcome.Content = Lang.Welcome + " " + MySettings.Current.UserName.Replace("_", "__");
            //ResourceManager.Refresh();
            //MyExtensions.ThemeChange("Dark");
        }
        internal void BlueList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MyDisplayBlueprint Selected = (MyDisplayBlueprint)BlueList.SelectedItem;
            if (Selected != null && File.Exists(currentBluePatch + Selected.Name + "\\bp.sbc"))
            {
                CurrentBlueprint = new MyXmlBlueprint(currentBluePatch + Selected.Name);
                if (MySettings.Current.DontOpenBlueprintsOnScan)
                {
                    Selected.AddXmlData(CurrentBlueprint);
                }
                BluePicture.Source = CurrentBlueprint.GetPic();


                BlueText.Inlines.Clear();
                BlueText.Inlines.Add($"{Lang.Blueprint}: {Selected.Name}\r\n" +
                    $"{Lang.Name}: {CurrentBlueprint.Name}\r\n" +
                    $"{Lang.Created}: {Selected.CreationTimeText}\r\n" +
                    $"{Lang.Changed}: {Selected.LastEditTimeText}\r\n" +
                    $"{Lang.GridCount}: {Selected.GridCountText}\r\n" +
                    $"{Lang.BlockCount }: {Selected.BlockCountText}\r\n" +
                    $"{Lang.Owner}: ");
                if (CurrentBlueprint.Owner != "0")
                {
                    Hyperlink Owner = new Hyperlink(new Run(Selected.Owner));
                    if (CurrentBlueprint.Owner == MySettings.Current.SteamID)
                        Owner = new Hyperlink(new Run(MySettings.Current.UserName));
                    Owner.NavigateUri = new Uri("steam://url/SteamIDPage/" + CurrentBlueprint.Owner);
                    Owner.RequestNavigate += Hyperlink_RequestNavigate;
                    BlueText.Inlines.Add(Owner);
                }
                else
                {
                    BlueText.Inlines.Add(Selected.Owner);
                }
                CalculateButton.IsEnabled = MyGameData.IsInitialized;
                EditButton.IsEnabled = true;
                PrefabButton.IsEnabled = true;
                BackupButton.IsEnabled = Directory.Exists(CurrentBlueprint.Patch + "/Backups");
                foreach (string file in Directory.GetFiles(CurrentBlueprint.Patch, "bp.sbc*", SearchOption.TopDirectoryOnly))
                {
                    if (File.Exists(file) && Path.GetFileName(file) != "bp.sbc") File.Delete(file);
                }
                if (MySettings.Current.DontOpenBlueprintsOnScan)
                {
                    if (OldSortBy != null)
                        GoSort(OldSortBy, null);
                    BlueList.ScrollIntoView(Selected);
                }
            }
            else
            {
                if (BlueList.SelectedIndex != -1) BlueList.Items.Remove(BlueList.SelectedItem);
                CurrentBlueprint = null;
                BluePicture.Source = new BitmapImage(new Uri("pack://application:,,,/Resource/blueprints-textures_00394054.jpg"));
                BlueText.Text = Lang.SelectBlue;
                CalculateButton.IsEnabled = false;
                EditButton.IsEnabled = false;
                PrefabButton.IsEnabled = false;
                BackupButton.IsEnabled = false;
            }
            Height++; Height--;
        }
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.AbsoluteUri);
            e.Handled = true;
        }
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(CurrentBlueprint.Patch + "/~lock.dat"))
            {
                
                if (!MySettings.Current.MultiWindow) Hide();
                else
                {
                    //Left = 0;
                    //Top = SystemParameters.PrimaryScreenHeight / 2 - (Height / 2);
                }
                if (MySettings.Current.SaveBackups)
                {
                    CurrentBlueprint.SaveBackup();
                    BackupButton.IsEnabled = true;
                }
                EditBlueprint Form = new EditBlueprint(File.Create(CurrentBlueprint.Patch + "/~lock.dat", 256, FileOptions.DeleteOnClose), CurrentBlueprint);
                Form.Show();
            }
            else
            {
                new MessageDialog(DialogPicture.warn, "Error", Lang.AlreadyOpened, null, DialogType.Message).Show();
            }
        }
        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(CurrentBlueprint.Patch + "/~lock.dat"))
            {
                new BackupManager(File.Create(CurrentBlueprint.Patch + "/~lock.dat", 256, FileOptions.DeleteOnClose), CurrentBlueprint).Show();
            }
            else
            {
                new MessageDialog(DialogPicture.warn, "Error", Lang.AlreadyOpened, null, DialogType.Message).Show();
            }
        }
        private void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            //new MesassageDialog(DialogPicture.attention, "Attention", Lang.ComingSoon, null, DialogType.Message).Show(); return;
            if (!File.Exists(CurrentBlueprint.Patch + "/~lock.dat"))
            {
                if (!MySettings.Current.MultiWindow) Hide();
                else
                {
                    //Left = SystemParameters.PrimaryScreenWidth / 2 - ((360 + 800) / 2);
                    //Top = SystemParameters.PrimaryScreenHeight / 2 - (Height / 2);
                }
                Calculator Form = new Calculator(File.Create(CurrentBlueprint.Patch + "/~lock.dat", 256, FileOptions.DeleteOnClose), CurrentBlueprint);
                try
                {
                    Form.Show();
                }
                catch
                {
                    Focus();
                }
            }
            else
            {
                new MessageDialog(DialogPicture.warn, "Error", Lang.AlreadyOpened, null, DialogType.Message).Show();
            }
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            InitBlueprints();
        }
        private void PicMenuItemNormalize_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentBlueprint != null)
            {
                BluePicture.Source = CurrentBlueprint.GetPic(true);
            }
        }
        private void SelectorMenuItemFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(currentBluePatch);
            }
            catch
            {
            }
        }
        private void SelectorMenuItemFolder2_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentBlueprint != null)
            {
                try
                {
                    Process.Start(CurrentBlueprint.Patch);
                }
                catch
                {
                }
            }
            else
            {
                new MessageDialog(DialogPicture.attention, "Attention", Lang.SelectBlueForOpen, null, DialogType.Message).Show();
            }
        }
        private void BackupsMenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            Lock.Height = SystemParameters.PrimaryScreenHeight;
            Lock.DataContext = 0;
            new MessageDialog(DialogPicture.warn, Lang.UnsafeAction, Lang.ItWillDeleteAllBackps, (Dial) =>
            {
                if (Dial == DialоgResult.Yes)
                {
                    foreach (string dir in Directory.GetDirectories(currentBluePatch))
                    {
                        if (Directory.Exists(dir + "\\Backups")) Directory.Delete(dir + "\\Backups", true);
                    }
                    BlueList_SelectionChanged(null, null);
                }
                Lock.Height = 0;
            }).Show();
        }
        private void WindowsMenuItemAbout_Click(object sender, RoutedEventArgs e)
        {
            if (About.LastWindow == null) new About().Show();
            else About.LastWindow.Focus();
        }
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.LastWindow == null) new Settings().Show();
            else Settings.LastWindow.Focus();
        }
        private void MenuFolderItem_Click(object sender, RoutedEventArgs e)
        {
            string folder = ((MenuItem)sender).Header.ToString();
            string newDir = currentBluePatch + folder + "\\";
            if (Directory.Exists(newDir))
            {
                currentBluePatch = newDir;
                InitBlueprints();
            }
            else
            {
                ((MenuItem)sender).Visibility = Visibility.Collapsed;
            }
        }
        private void MenuBackItem_Click(object sender, RoutedEventArgs e)
        {
            currentBluePatch = Path.GetDirectoryName(currentBluePatch.TrimEnd('\\')) + "\\";
            InitBlueprints();
        }
        private void MenuHomeItem_Click(object sender, RoutedEventArgs e)
        {
            currentBluePatch = MySettings.Current.BlueprintPatch;
            InitBlueprints();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                MySettings.Serialize();
                ArmorReplaceClass.Serialize();
                if (UpdateAvailable.window != null && UpdateAvailable.window.IsLoaded)
                {
                    e.Cancel = true;
                    Hide();
                    UpdateAvailable.window.Show();
                    UpdateAvailable.last_open = true;
                }
                else
                {
                    Application.Current.Shutdown();
                }
            }
            catch(Exception ee)
            {
            }
        }
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //Logger.Add($"Main window size changed to {Width}x{Height}");
            //new Task(() =>
            //{
            //    Thread.Sleep(100);
            //    MyExtensions.AsyncWorker(() =>
            MinHeight = (ImageRow.ActualHeight + TextRow.ActualHeight + 40 + (ActualHeight - ControlsConteiner.ActualHeight));
            //});
        }

        private void Lock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (Lock.DataContext)
            {
                case 0:
                    if (MessageDialog.Last != null)
                    {
                        MessageDialog.Last.Focus();
                    }
                    break;
                case 1:
                    break;
                default:
                    Window wind = (Window)Lock.DataContext;
                    if (wind != null)
                    {
                        wind.Focus();
                    }
                    break;
            }
        }
        public void SetLock(bool lockly, object DataContext)
        {
            if (lockly)
            {
                Lock.Height = SystemParameters.PrimaryScreenHeight;
                Lock.DataContext = DataContext;
            }
            else
            {
                Lock.Height = 0;
                Lock.DataContext = null;
            }
        }
        bool BlueprintInitializing = false;
        private void InitBlueprints()
        {
            if (BlueprintInitializing) return;
            BlueprintInitializing = true;
            Title = "SE BlueprintEditor Loading...";
            FoldersItem.Items.Clear();
            BlueList.Items.Clear();
            if (currentBluePatch != MySettings.Current.BlueprintPatch)
            {
                MenuItem Fldr = new MenuItem
                {
                    Header = Lang.GoBack,
                    Icon = new Image
                    {
                        Source = new BitmapImage(new Uri("pack://application:,,,/Resource/img_354138.png"))
                    }
                };
                Fldr.Click += MenuBackItem_Click;
                FoldersItem.Items.Add(Fldr);
                MenuItem Fldr2 = new MenuItem
                {
                    Header = Lang.GoHome,
                    Icon = new Image
                    {
                        Source = new BitmapImage(new Uri("pack://application:,,,/Resource/img_144440.png"))
                    }
                };
                Fldr2.Click += MenuHomeItem_Click;
                FoldersItem.Items.Add(Fldr2);
            }
            else FoldersItem.IsEnabled = false;
            BitmapImage fldicn = new BitmapImage(new Uri("pack://application:,,,/Resource/img_308586.png"));
            List<MenuItem> foldrmenu = new List<MenuItem>();
            string SerachQuery = Search.Text.ToLower();
            new Task(() =>
            {
                Thread.CurrentThread.Name = "BlueprintInitializer";
                bool First = true;
                ParallelLoopResult status = Parallel.ForEach(Directory.GetDirectories(currentBluePatch), x =>//.OrderBy(x => Path.GetFileName(x))
                {
                    string feld = Path.GetFileNameWithoutExtension(x);
                    MyDisplayBlueprint Elem = MyDisplayBlueprint.FromBlueprint(x);
                    if (Elem is null)
                    {
                        MyExtensions.AsyncWorker(() =>
                        {
                            if (First && FoldersItem.IsEnabled)
                            {
                                FoldersItem.Items.Add(new Separator());
                            }
                            First = false;
                            FoldersItem.IsEnabled = true;
                            MenuItem Fldr = new MenuItem
                            {
                                Header = feld,
                                Icon = new Image
                                {
                                    Source = fldicn
                                }
                            };
                            Fldr.Click += MenuFolderItem_Click;
                            foldrmenu.Add(Fldr);
                            //FoldersItem.Items.Add(Fldr);
                        });
                        return;
                    }
                     MyExtensions.AsyncWorker(() =>
                     {
                         bool AddIt = true;
                         if (!string.IsNullOrEmpty(Search.Text))
                         {
                             switch (SearchBy.SelectedIndex)
                             {
                                 case 0:
                                     if (!Elem.Name.ToLower().Contains(SerachQuery)) AddIt = false;
                                     break;
                                 case 1:
                                     if (!Elem.Owner.ToLower().Contains(SerachQuery)) AddIt = false;
                                     break;
                                 case 2:
                                     if (!Elem.CreationTimeText.ToLower().Contains(SerachQuery)) AddIt = false;
                                     break;
                                 case 3:
                                     if (!Elem.LastEditTimeText.ToLower().Contains(SerachQuery)) AddIt = false;
                                     break;
                             }
                         }
                         if (AddIt)
                             BlueList.Items.Add(Elem);
                     });
                });
                new Task(() =>
                {
                    while (!status.IsCompleted) { }
                    MyExtensions.AsyncWorker(() =>
                    {
                        foreach (MenuItem x in foldrmenu.OrderBy(x => x.Header))
                            FoldersItem.Items.Add(x);
                        if (OldSortBy != null)
                            GoSort(OldSortBy,null);
                        Title = "SE BlueprintEditor";
                        BlueprintInitializing = false;
                    });
                }).Start();
            }).Start();
        }
        GridViewColumn OldSortBy;
        private void GoSort(object sender, RoutedEventArgs e)
        {
            if (!(sender is GridViewColumn SortBy)) SortBy = ((GridViewColumnHeader)sender).Column;
            if (SortBy == null) return;
            string PropertyPatch = ((Binding)SortBy.DisplayMemberBinding).Path.Path.Replace("Text", "");
            if (OldSortBy != null) {
                if (OldSortBy.Header is GridViewColumnHeader cHead)
                    cHead.Content = cHead.Content.ToString().Trim('↓', '↑', ' ');
            }
            ListSortDirection OldDirection = ListSortDirection.Ascending;
            if (BlueList.Items.SortDescriptions.Count > 0 && BlueList.Items.SortDescriptions[0].PropertyName == PropertyPatch)
                OldDirection = BlueList.Items.SortDescriptions[0].Direction;
            ListSortDirection NewDirection;
            if (e != null)
                NewDirection = OldDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
            else
                NewDirection = OldDirection;
            BlueList.Items.SortDescriptions.Clear();
            BlueList.Items.SortDescriptions.Add(new SortDescription(PropertyPatch, NewDirection));
            //BlueList.Items.SortDescriptions.Add(new SortDescription("Name", NewDirection));
            if (SortBy.Header is GridViewColumnHeader cvHead)
                cvHead.Content += NewDirection == ListSortDirection.Ascending ? " ↓" : " ↑";
            OldSortBy = SortBy;
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            /*
            int x = 0;
            int y = 1 / x;*/
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            InitBlueprints();
            /*
            int x = 0;
            int y = 13 / x;*/
        
        }
        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            if (CurrentBlueprint != null)
            {
                Lock.Height = SystemParameters.PrimaryScreenHeight;
                Lock.DataContext = 0;
                new MessageDialog(DialogPicture.warn, Lang.UnsafeAction, Lang.ItWillDeleteThisBackp, (Dial) =>
                {
                    if (Dial == DialоgResult.Yes)
                    {
                        if (Directory.Exists(CurrentBlueprint?.Patch + "\\Backups"))
                            Directory.Delete(CurrentBlueprint.Patch + "\\Backups", true);
                        BlueList_SelectionChanged(null, null);
                    }
                    Lock.Height = 0;
                }).Show();
            }
            else
            {
                new MessageDialog(DialogPicture.attention, "Attention", Lang.SelectBlueForDelBack, null, DialogType.Message).Show();
            }
        }
        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            WorkshopCache.ClearMods();
        }
        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MySettings.Current.SavesPatch))
            {
                new MessageDialog(DialogPicture.attention, "Saves patch not found", Lang.SPNF, null, DialogType.Message).Show();
                return;
            }
            string mods = WorkshopCache.GetModsForWorld();
            new MessageDialog((x)=> {
                MyWorld.Create(x, mods);
            }, Lang.CreateWorld, Lang.EnterWorldNameForCreate).Show();
        }
        private void MenuItem_Click_5(object sender, RoutedEventArgs e)
        {
            WorkshopCache.MoveBlueprintsToLocal();
            InitBlueprints();
        }

        private void InDev(object sender, RoutedEventArgs e)
        {
            new MessageDialog(DialogPicture.attention, "InDev", "This features in development, please wait for new version!", null, DialogType.Message).Show();
        }

        private void PrefabButton_Click(object sender, RoutedEventArgs e)
        {
            new MessageDialog(DialogPicture.attention, "Experemental", "This experemental function, your prefab in file PrefabTest.xml.", (x) => {
                File.WriteAllText("PrefabTest.xml", CurrentBlueprint.ConvertToPrefab());
            },DialogType.Message).Show();
            
        }

        private void MenuItem_Click_6(object sender, RoutedEventArgs e)
        {
            if (ImageConverter.Opened != null)
                ImageConverter.Opened.Focus();
            else
                new ImageConverter().Show();
        }

        private void MenuItem_Click_7(object sender, RoutedEventArgs e)
        {
            if (WorkshopDownloader.Opened != null)
                WorkshopDownloader.Opened.Focus();
            else
                new WorkshopDownloader().Show();
        }

        private void Welcome_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
