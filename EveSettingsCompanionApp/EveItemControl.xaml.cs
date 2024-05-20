using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Cache;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EveSettingsCompanionApp
{
    /// <summary>
    /// Interaction logic for EveItemControl.xaml
    /// </summary>
    public partial class EveItemControl : UserControl
    {
        private static readonly ConcurrentDictionary<long, Task<string>> _apiCache = new ConcurrentDictionary<long, Task<string>>();

        internal string FilePath { get; private set; }
        internal bool IsChar { get; private set; }
        internal long Id { get; private set; }

        public EveItemControl()
        {
            InitializeComponent();
        }

        public EveItemControl(string path, bool isChar, long id)
        {
            FilePath = path;
            IsChar = isChar;
            Id = id;
            InitializeComponent();
        }

        private async void UserControl_Initialized(object sender, EventArgs e)
        {
            await Update();
        }

        private bool _selected;
        public bool Selected
        {
            get
            {
                return _selected;
            }
            set
            {
                _selected = value;
                if (_selected)
                {
                    BorderBrush = new SolidColorBrush(Colors.Red);
                    BorderThickness = new Thickness(5);
                    Margin = new Thickness(0);
                }
                else
                {
                    BorderBrush = new SolidColorBrush(Colors.Red);
                    BorderThickness = new Thickness(0);
                    Margin = new Thickness(5);
                }
            }
        }

        private async Task Update()
        {
            if (FilePath != null)
            {
                if (IsChar)
                {
                    try
                    {
                        var json = await _apiCache.GetOrAdd(Id, async (id) => {
                            using var client = new HttpClient();
                            return await client.GetStringAsync($"https://esi.evetech.net/latest/characters/{id}/?datasource=tranquility");
                        });
                        var info = JsonConvert.DeserializeObject<EveCharacterResponse>(json);
                        Title.Text = info.name;
                        ProfileImage.Source = new BitmapImage(new Uri($"https://images.evetech.net/characters/{Id}/portrait"), new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable));
                        LastModified.Text = File.GetLastWriteTime(FilePath).ToString("F");
                    }
                    catch (Exception ex)
                    {
                        // Likely an HTTP exception, which happens when a character has been deleted. Fall back to Id and default image.
                        Title.Text = Id.ToString();
                        ProfileImage.Source = new BitmapImage(new Uri("pack://application:,,,/portrait.jpg"));
                        LastModified.Text = File.GetLastWriteTime(FilePath).ToString("F");
                    }
                }
                else
                {
                    Title.Text = Id.ToString();
                    ProfileImage.Source = new BitmapImage(new Uri("pack://application:,,,/portrait.jpg"));
                    LastModified.Text = File.GetLastWriteTime(FilePath).ToString("F");
                }
            }
            else
            {
                ProfileImage.Source = new BitmapImage(new Uri("pack://application:,,,/portrait.jpg"));
                Title.Text = "My Title";
                LastModified.Text = DateTime.Now.ToString("F");
            }
        }

        public void CopyTo(EveItemControl other)
        {
            if (FilePath != other.FilePath)
            {
                File.Copy(FilePath, other.FilePath, true);
                File.SetLastWriteTime(other.FilePath, File.GetLastWriteTime(FilePath).AddSeconds(-1));
            }
        }

#pragma warning disable IDE1006 // Naming Styles
        public class EveCharacterResponse
        {
            public int ancestry_id { get; set; }
            public DateTime birthday { get; set; }
            public int bloodline_id { get; set; }
            public int corporation_id { get; set; }
            public string description { get; set; }
            public string gender { get; set; }
            public string name { get; set; }
            public int race_id { get; set; }
            public float security_status { get; set; }
        }
#pragma warning restore IDE1006 // Naming Styles

    }
}
