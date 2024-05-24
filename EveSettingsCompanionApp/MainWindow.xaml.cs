using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EveSettingsCompanionApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.VersionLabel.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private void EveFolderSelectorControl_EvePathChanged(object source, string path)
        {
            SourceCharacters.Children.Clear();
            DestinationCharacters.Children.Clear();
            SourceAccounts.Children.Clear();
            DestinationAccounts.Children.Clear();

            var files = Directory.EnumerateFiles(path, "core_*.dat", SearchOption.TopDirectoryOnly);
            var orderedFiles = files.OrderByDescending(f => File.GetLastWriteTime(f));

            foreach (var item in orderedFiles)
            {
                var match = Regex.Match(System.IO.Path.GetFileName(item), ".*?_(char|user)_(\\d+)\\.dat$");
                if (match.Success)
                {
                    var ischar = match.Groups[1].Value.ToLowerInvariant() == "char";
                    var id = long.Parse(match.Groups[2].Value);
                    if (ischar)
                    {
                        SourceCharacters.Children.Add(new EveItemControl(item, ischar, id) {  Selected = SourceCharacters.Children.Count == 0 });
                        DestinationCharacters.Children.Add(new EveItemControl(item, ischar, id) { Selected = DestinationCharacters.Children.Count != 0 });
                    }
                    else
                    {
                        SourceAccounts.Children.Add(new EveItemControl(item, ischar, id) { Selected = SourceAccounts.Children.Count == 0 });
                        DestinationAccounts.Children.Add(new EveItemControl(item, ischar, id) { Selected = DestinationAccounts.Children.Count != 0 });
                    }
                }
            }
        }

        private void SourceCharacters_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!(e.Source is EveItemControl eveItem))
                return;

            if (sender == SourceCharacters)
            {
                foreach (EveItemControl item in SourceCharacters.Children)
                    item.Selected = false;
                eveItem.Selected = true;
            }
            else if (sender == DestinationCharacters)
            {
                eveItem.Selected = !eveItem.Selected;
            }
            else if (sender == SourceAccounts)
            {
                foreach (EveItemControl item in SourceAccounts.Children)
                    item.Selected = false;
                eveItem.Selected = true;
            }
            else if (sender == DestinationAccounts)
            {
                eveItem.Selected = !eveItem.Selected;
            }
        }

        private async Task PerformCopy(string name, IEnumerable<EveItemControl> sourcelist, IEnumerable<EveItemControl> destinationlist)
        {
            var source = sourcelist.FirstOrDefault(c => c.Selected);
            if (source == null)
            {
                await this.ShowMessageAsync("Invalid selection", $"First select the {name} you wish to use as source.");
                return;
            }

            var destinations = destinationlist.Where(c => c.Selected);
            if (!destinations.Any())
            {
                await this.ShowMessageAsync("Invalid selection", $"Select any target {name}s you want to copy settings to.");
                return;
            }

            var sb = new StringBuilder();
            foreach (var dest in destinations)
            {
                try
                {
                    source.CopyTo(dest);
                    sb.AppendLine($"Successfully copied \"{source.Title.Text}\" to \"{dest.Title.Text}\".");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"Failed copying \"{source.Title.Text}\" to \"{dest.Title.Text}\": {ex}");
                }
            }
            await this.ShowMessageAsync($"Success", sb.ToString());
        }

        private async void Character_Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            await PerformCopy("character", SourceCharacters.Children.OfType<EveItemControl>(), DestinationCharacters.Children.OfType<EveItemControl>());
        }

        private async void Account_Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            await PerformCopy("account", SourceAccounts.Children.OfType<EveItemControl>(), DestinationAccounts.Children.OfType<EveItemControl>());
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void StackPanel_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {

        }
    }
}
