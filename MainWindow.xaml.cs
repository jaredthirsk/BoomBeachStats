using BoomBeachStats.OCR;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BoomBeachStats
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private void CreateDirectoryIfNeeded(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }


        

        public MainWindow()
        {
            CreateDirectoryIfNeeded(DataDir);
            CreateDirectoryIfNeeded(RosterDir);

            InitializeComponent();
            SetImportFileName();


            DateTime date = DateTime.Now;
            for (int i = 60; i > 0; i--)
            {
                var filename = GetFileNameForRoster(date);
                if (File.Exists(filename))
                {
                    var sb = new StringBuilder();
                    using (var sr = new StreamReader(filename))
                    {
                        string s;
                        while ((s = sr.ReadLine()) != null)
                        {
                            sb.AppendLine(s);
                        }
                    }
                    TxtRoster.Text = sb.ToString();
                    break;
                }
                date = date - TimeSpan.FromDays(1);
            }

            Calendar.SelectedDatesChanged += Calendar_SelectedDatesChanged;
        }

        #region Paths

        public static string DataDir
        {
            get
            {
                string d = ConfigurationManager.AppSettings["DataDir"];
                if (d == null)
                {
                    d = Environment.CurrentDirectory + @"\BoomBeachData\";
                    var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    ConfigurationManager.AppSettings["DataDir"] = d;
                    configFile.AppSettings.Settings.Add("DataDir", d);
                    configFile.Save();
                }
                return d;
            }
        }
        public static string RosterDir
        {
            get { return DataDir + @"Roster\"; }
        }

        public static string GetAttackersFileNameForDate(DateTime date)
        {
            return DataDir + GetFileNameForDate(date);
        }
        public static string GetFileNameForDate(DateTime date)
        {
            return date.ToString("yyyy-MM-dd") + ".txt";
        }
        public static string GetFileNameForRoster(DateTime date)
        {
            return RosterDir + GetFileNameForDate(date);
        }

        #endregion

        #region Attackers

        List<string> GetAttackers(DateTime date)
        {
            var results = new List<string>();

            var path = GetAttackersFileNameForDate(date);
            if (!File.Exists(path))
            {
                return null;
            }
            var sb = new StringBuilder();
            using (var tw = new StreamReader(path))
            {
                string s;
                while (!String.IsNullOrWhiteSpace(s = tw.ReadLine()))
                {
                    results.Add(s.Trim());
                }
            }
            return results;
        }
        #endregion

        #region Roster

        public List<string> Roster
        {
            get;
            set;
        }

        #endregion
        void Calendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            var date = Calendar.SelectedDate;
            if (date.HasValue)
            {
                TxtAttackers.Text = "";
                var sb = new StringBuilder();
                var attackers = GetAttackers(date.Value);
                if (attackers != null)
                {
                    foreach (var a in attackers)
                    {
                        sb.AppendLine(a);
                    }
                }
                TxtAttackers.Text = sb.ToString();
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var date = Calendar.SelectedDate;
            if (!date.HasValue)
            {
                MessageBox.Show("Select a date");
                return;
            }
            var path = GetAttackersFileNameForDate(date.Value);
            using (var tw = new StreamWriter(path))
            {
                foreach (var name in TxtAttackers.Text.Split('\r').Select(x => x.Trim()))
                {
                    tw.WriteLine(name);
                }
            }
        }

        private void TxtRoster_TextChanged(object sender, TextChangedEventArgs e)
        {
            var list = new List<string>();
            foreach (var name in TxtRoster.Text.Split('\r').Select(x => x.Trim()))
            {
                list.Add(name);
            }
            Roster = list;
            SaveRoster();
        }

        private void SaveRoster()
        {
            List<string> list = Roster;
            if (list == null) return;

            var date = DateTime.Now;
            var path = GetFileNameForRoster(date);
            using (var tw = new StreamWriter(path))
            {
                foreach (var name in list)
                {
                    tw.WriteLine(name);
                }
            }
        }

        private void CalculateNonAttackers_Click(object sender, RoutedEventArgs e)
        {
            var result = CalculateNonAttackers();

            WhoDidntAttack.Text = "";
            var sb = new StringBuilder();
            foreach (var kvp in result.OrderBy(x => -x.Value.Count))
            {
                if (string.IsNullOrWhiteSpace(kvp.Key.Trim())) continue;
                sb.Append(kvp.Value.Count);
                sb.Append(" ");
                sb.Append(kvp.Key);
                foreach (var date in kvp.Value)
                {
                    sb.Append(" ");
                    sb.Append(date.ToString("MMM-dd"));
                }
                sb.AppendLine();
            }
            WhoDidntAttack.Text = sb.ToString();
        }

        private Dictionary<string, List<DateTime>> CalculateNonAttackers()
        {
            DateTime date = DateTime.Now;
            int opCount = 0;
            HashSet<string> attackers;

            Dictionary<string, List<DateTime>> nonAttackers = new Dictionary<string, List<DateTime>>();
            bool changedRoster = false;

            for (int i = (int)DaysToGoBackSlider.Value; i > 0; i--, date -= TimeSpan.FromDays(1))
            {
                var list = GetAttackers(date);
                if (list == null || list.Count == 0) continue;

                attackers = new HashSet<string>(list);

                opCount++;
                foreach (var member in Roster.ToArray())
                {
                    if (!attackers.Contains(member))
                    {
                        List<DateTime> missedAttacks;
                        if (nonAttackers.ContainsKey(member))
                        {
                            missedAttacks = nonAttackers[member];
                        }
                        else
                        {
                            missedAttacks = new List<DateTime>();
                            nonAttackers.Add(member, missedAttacks);
                        }
                        missedAttacks.Add(date);
                    }
                }

                foreach(var attacker in list)
                {
                    if (!Roster.Contains(attacker))
                    {
                        if (MessageBox.Show("Add " + attacker + " to roster?", "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            Roster.Add(attacker);
                            changedRoster = true;
                        }
                    }
                }
            }
            if(changedRoster)
            {
                TxtRoster.Text = Roster.Aggregate((x, y) => x + Environment.NewLine + y);
            }
            return nonAttackers;
        }

        #region Import

        #region NextImage

        public int NextImage
        {
            get { return nextImage; }
            set
            {
                if (nextImage == value) return;
                nextImage = value;
                SetImportFileName();
                OnPropertyChanged("NextImage");
            }
        } private int nextImage = 1655;

        #endregion


        public void SetImportFileName()
        {
            ImportTextBox.Text = "IMG_" + NextImage + ".png";
        }

        public int MaxAuto = 5;
        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            for (int auto = MaxAuto; auto > 0; auto--)
            {
                var path = Scanner.Instance.ImageDir + ImportTextBox.Text;
                if (!File.Exists(path)) { break; }

                var list = Scanner.Instance.ScanForOpAttacks(path);
                foreach (var item in list.Values)
                {
                    if (!String.IsNullOrWhiteSpace(TxtAttackers.Text))
                    {
                        TxtAttackers.Text += Environment.NewLine;
                    }
                    TxtAttackers.Text += item.Name;
                }
                NextImage++;
                SetImportFileName();
            }
        }

        #endregion


        #region Misc

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            var ev = PropertyChanged;
            if (ev != null) ev(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #endregion
    }
}
