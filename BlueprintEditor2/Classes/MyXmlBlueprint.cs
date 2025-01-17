﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Xml;
using System.Reflection;
using BlueprintEditor2.Resource;
using System.Threading;
using System.Windows;

namespace BlueprintEditor2
{
    public class MyXmlBlueprint
    {
        public string Patch;
        private readonly XmlDocument BlueprintXml = new XmlDocument();
        public MyXmlGrid[] Grids;
        public string Name
        {
            get
            {
                try
                {
                    return BlueprintXml.GetElementsByTagName("Id")?.Item(0).Attributes["Subtype"].Value;
                }
                catch
                {
                    return "ERROR";
                }
            }
            set
            {
                try
                {
                    BlueprintXml.GetElementsByTagName("Id").Item(0).Attributes["Subtype"].Value = value;
                }
                catch
                {
                }
            }
        }
        public string DisplayName
        {
            get
            {
                try
                {
                   return BlueprintXml.GetElementsByTagName("DisplayName").Item(0).InnerText;
                }
                catch
                {
                    return "ERROR";
                }
            }
        }
        public string Owner
        {
            get
            {
                try
                {
                    return BlueprintXml.GetElementsByTagName("OwnerSteamId").Item(0).InnerText;
                }
                catch
                {
                    return "ERROR";
                }
            }
        }
        public MyXmlBlueprint(string patch)
        {
            Patch = patch;
            if (File.Exists(Patch + "\\bp.sbc"))
            {
                BlueprintXml.Load(Patch + "\\bp.sbc");
                XmlNodeList Grides = BlueprintXml.GetElementsByTagName("CubeGrid");
                Grids = new MyXmlGrid[Grides.Count];
                for (int i = 0; i < Grides.Count; i++)
                {
                    Grids[i] = new MyXmlGrid(Grides[i]);
                }
            }
        }
        public void Save()//bool forced = false)
        {
            if (Directory.Exists(Patch))
            {
                foreach (var gr in Grids)
                    foreach (var bl in gr.Blocks)
                        foreach (var inv in bl.Inventories)
                            inv.Save();
                BlueprintXml.Save(Patch + "\\bp.sbc");
            }
        }
        public void SaveBackup(bool forced = false)
        {
            if (Directory.Exists(Patch + "/Backups"))
            {
                if (forced)
                {
                    File.Copy(Patch + "\\bp.sbc", Patch + "\\Backups\\" + DateTime.UtcNow.ToFileTimeUtc().ToString() + ".sbc");
                    return;
                }
                bool save = true;
                string Lastest = "",LastestName = "";
                List<string> Files = new List<string>();
                foreach (string file in Directory.GetFiles(Patch + "/Backups"))
                {
                    if (file.Contains("Lastest-"))
                    {
                        Lastest = File.ReadAllText(file);
                        LastestName = file;
                        continue;
                    }

                    //_ = DateTime.FromFileTimeUtc(long.Parse(file.Split('\\').Last().Replace(".sbc", "")));
                    if (Files.Contains(File.ReadAllText(file)))
                    {
                        File.Delete(file);
                    }
                    else
                    {
                        Files.Add(File.ReadAllText(file));
                    }
                }
                if (save && Lastest != File.ReadAllText(Patch + "\\bp.sbc"))
                {
                    if(LastestName != "") File.Move(LastestName,Path.GetDirectoryName(LastestName)+"\\"+Path.GetFileName(LastestName).Replace("Lastest-",""));
                    File.Copy(Patch + "\\bp.sbc", Patch + "\\Backups\\" + "Lastest-" + DateTime.UtcNow.ToFileTimeUtc().ToString() + ".sbc");
                }
            }
            else
            {
                Directory.CreateDirectory(Patch + "\\Backups");
                File.Copy(Patch + "\\bp.sbc", Patch + "\\Backups\\Lastest-" + DateTime.UtcNow.ToFileTimeUtc().ToString() + ".sbc");
            }
        }
        public BitmapImage GetPic(bool badOpac = false,bool useDialog = true)
        {
            if (!File.Exists(Patch + "\\thumb.png") || new FileInfo(Patch + "\\thumb.png").Length == 0)
            {
                if (useDialog)
                {
                    SelectBlueprint.window.Lock.Height = SystemParameters.PrimaryScreenHeight;
                    MyExtensions.AsyncWorker(() =>
                    {
                        new MessageDialog(DialogPicture.question, Lang.NoPic+" ["+ Name+"]", Lang.NoPicture, (Dial) =>
                            {
                                switch (Dial)
                                {
                                    case DialоgResult.Yes:
                                        Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
                                        {
                                            DefaultExt = ".png",
                                            Filter = Lang.ImFiles + "|*.png;*.jpeg;*.jpg"
                                        };
                                        bool? result = dlg.ShowDialog();
                                        if (result == true)
                                        {
                                            string filename = dlg.FileName;
                                            File.Copy(filename, Patch + "\\thumb.png");
                                        }
                                        SelectBlueprint.window.Lock.Height = 0;
                                        SelectBlueprint.window.BluePicture.Source = SelectBlueprint.window.CurrentBlueprint.GetPic();
                                        break;
                                    case DialоgResult.No:
                                        Properties.Resources.blueprints_textures_00394054.Save(Patch + "\\thumb.png");
                                        SelectBlueprint.window.Lock.Height = 0;
                                        SelectBlueprint.window.BluePicture.Source = SelectBlueprint.window.CurrentBlueprint.GetPic();
                                        break;
                                    default:
                                        SelectBlueprint.window.Lock.Height = 0;
                                        break;
                                }
                            },DialogType.Cancelable).Show();
                    });
                }
                return new BitmapImage(new Uri("pack://application:,,,/Resource/thumbDefault.png"));
            }
            else
            {
                if (badOpac)
                {
                    Image raw = Image.FromFile(Patch + "\\thumb.png");
                    Image img = SetImgOpacity(raw, 1);
                    raw.Dispose();
                    img.Save(Patch + "\\thumb.png");
                    img.Dispose();
                }
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                image.UriSource = new Uri(Patch + "\\thumb.png");
                image.EndInit();
                return image;
            }
        }
        private static Image SetImgOpacity(Image imgPic, float imgOpac)
        {
            Bitmap bmpPic = new Bitmap(imgPic.Width, imgPic.Height);
            Graphics gfxPic = Graphics.FromImage(bmpPic);
            ColorMatrix cmxPic = new ColorMatrix
            {
                Matrix33 = imgOpac,
                Matrix23 = imgOpac,
                Matrix13 = imgOpac,
                Matrix03 = imgOpac,
                Matrix43 = imgOpac
            };
            ImageAttributes iaPic = new ImageAttributes();
            iaPic.SetColorMatrix(cmxPic, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            gfxPic.DrawImage(imgPic, new Rectangle(0, 0, bmpPic.Width, bmpPic.Height), 0, 0, imgPic.Width, imgPic.Height, GraphicsUnit.Pixel, iaPic);
            gfxPic.Dispose();
            return bmpPic;
        }

        public string ConvertToPrefab()
        {
            StringBuilder str = new StringBuilder();
            str.Append("<?xml version=\"1.0\"?>")
.Append("<Definitions xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><Prefabs><Prefab><Id><TypeId>MyObjectBuilder_PrefabDefinition</TypeId>")
        .Append("<SubtypeId>").Append(Name).Append("</SubtypeId></Id>")
      .Append("<CubeGrids>").Append(BlueprintXml.GetElementsByTagName("CubeGrids").Item(0).InnerXml)
      .Append("</CubeGrids></Prefab></Prefabs></Definitions>");
            return str.ToString();
        }
    }
}
