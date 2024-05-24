using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace EveSettingsCompanionApp
{
    /// <summary>
    /// Interaction logic for EveFolderSelectorControl.xaml
    /// </summary>
    public partial class EveFolderSelectorControl : UserControl
    {
        public event Action<object, string> EvePathChanged;

        public string CurrentPath => (string)DetectedPaths.SelectedValue;

        public EveFolderSelectorControl()
        {
            InitializeComponent();
        }

        private async void Hyperlink_SelectCustomFolder(object sender, RequestNavigateEventArgs e)
        {
            var roamingappdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var ccpfolder = Path.Combine(roamingappdata, "..", "Local", "CCP", "EVE");
            ccpfolder = Path.GetFullPath(new Uri(ccpfolder).LocalPath) + Path.DirectorySeparatorChar;

            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            dialog.SelectedPath = ccpfolder;
            if (!dialog.ShowDialog(null).GetValueOrDefault())
            {
                return;
            }

            var matchingfiles = Directory.EnumerateFiles(dialog.SelectedPath, "core_*.dat", SearchOption.TopDirectoryOnly);
            if (!matchingfiles.Any())
            {
                await ((MetroWindow)Window.GetWindow(this)).ShowMessageAsync("Invalid folder", "Cannot find the expected core_char_*.dat or core_user_*.dat files in this folder.");
                return;
            }

            var dict = (Dictionary<string, string>)DetectedPaths.ItemsSource;
            if (!dict.ContainsKey("custom"))
                dict.Add("custom", dialog.SelectedPath);
            else
                dict["custom"] = dialog.SelectedPath;

            DetectedPaths.ItemsSource = dict;
            DetectedPaths.SelectedIndex = dict.Count - 1;
        }

        private void UserControl_Loaded(object sender, EventArgs e)
        {
            var eveSettingsDirectories = new Dictionary<string, string>();

            var roamingappdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var ccpfolder = Path.Combine(roamingappdata, "..", "Local", "CCP", "EVE");
            ccpfolder = Path.GetFullPath(new Uri(ccpfolder).LocalPath);
            try
            {
                foreach (var item in Directory.EnumerateFiles(ccpfolder, "core_*.dat", SearchOption.AllDirectories))
                {
                    var match = Regex.Match(Path.GetFileName(item), ".*?_(char|user)_(\\d+)\\.dat$");
                    if (match.Success)
                    {
                        var settingsdir = Path.GetDirectoryName(item);
                        var installationdir = Path.GetDirectoryName(settingsdir);
                        var friendly = $"{Path.GetFileName(installationdir)}{Path.DirectorySeparatorChar}{Path.GetFileName(settingsdir)}";
                        if (!eveSettingsDirectories.ContainsKey(friendly))
                            eveSettingsDirectories.Add(friendly, settingsdir);
                    }
                }
            }
            catch (DirectoryNotFoundException ex)
            {
                // No EVE directory found, EVE is likely not installed
            }

            DetectedPaths.DisplayMemberPath = "Key";
            DetectedPaths.SelectedValuePath = "Value";
            DetectedPaths.ItemsSource = eveSettingsDirectories;
            if (DetectedPaths.Items.Count > 0)
            {
                // To accomodate the most common users we will select tranquility first, then if there
                // are multiple tranquility installations, select the one with the most recent files in it.
                var mostRecent = eveSettingsDirectories.Values
                                    .OrderByDescending(d => d.Contains("_tq_"))
                                    .ThenByDescending(d => new DirectoryInfo(d).GetFiles().Max(f => f.LastWriteTimeUtc))
                                    .First();
                DetectedPaths.SelectedValue = mostRecent;
            }
        }

        private void DetectedPaths_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Refresh();
        }

        private void Refresh()
        {
            EvePathChanged?.Invoke(this, CurrentPath);
        }

        private async void Hyperlink_Backup(object sender, RequestNavigateEventArgs e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaSaveFileDialog();
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            dialog.OverwritePrompt = true;
            dialog.Filter = "zip files (*.zip)|*.zip";
            dialog.AddExtension = true;
            dialog.DefaultExt = ".zip";
            if (!dialog.ShowDialog(null).GetValueOrDefault())
            {
                return;
            }

            try
            {
                if (File.Exists(dialog.FileName))
                    File.Delete(dialog.FileName);
                ZipFile.CreateFromDirectory(CurrentPath, dialog.FileName);
                await ((MetroWindow)Window.GetWindow(this)).ShowMessageAsync("Success", $"Backup has been created and written to:\n{dialog.FileName}");
            }
            catch (Exception ex)
            {
                await ((MetroWindow)Window.GetWindow(this)).ShowMessageAsync("Error creating backup", $"An error occurred while trying to write the backup: {ex}");
            }
        }

        private async void Hyperlink_Restore(object sender, RequestNavigateEventArgs e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaOpenFileDialog();
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            dialog.CheckFileExists = true;
            dialog.Filter = "zip files (*.zip)|*.zip";
            dialog.AddExtension = true;
            dialog.DefaultExt = ".zip";
            dialog.Multiselect = false;
            dialog.ShowReadOnly = true;
            if (!dialog.ShowDialog(null).GetValueOrDefault())
            {
                return;
            }

            try
            {
                foreach (var item in Directory.EnumerateFiles(CurrentPath))
                    File.Delete(item);
                foreach (var item in Directory.EnumerateDirectories(CurrentPath))
                    Directory.Delete(item, true);
                ZipFile.ExtractToDirectory(dialog.FileName, CurrentPath, true);
                await ((MetroWindow)Window.GetWindow(this)).ShowMessageAsync("Success", $"Backup has been successfully restored.");
                Refresh();
            }
            catch (Exception ex)
            {
                await ((MetroWindow)Window.GetWindow(this)).ShowMessageAsync("Error restoring", $"An error occurred while trying to restore the backup: {ex}");
            }
        }
    }
}
