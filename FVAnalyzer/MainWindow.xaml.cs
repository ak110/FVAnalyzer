using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace FVAnalyzer {
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window {
        /// <summary>
        /// feature vector
        /// </summary>
        public class FV : INotifyPropertyChanged {
            public event PropertyChangedEventHandler PropertyChanged;

            public string Name { get; set; }
            public double Scale { get; set; }

            int? _Min, _Max, _AbsMax, _Mean, _StdEvp, _MeanEvp;
            public int? MinIndirect {
                get { return _Min; }
                set { _Min = value; NotifyChanged("Min"); }
            }
            public int? MaxIndirect {
                get { return _Max; }
                set { _Max = value; NotifyChanged("Max"); }
            }
            public int? AbsMaxIndirect {
                get { return _AbsMax; }
                set { _AbsMax = value; NotifyChanged("AbsMax"); }
            }
            public int? MeanIndirect {
                get { return _Mean; }
                set { _Mean = value; NotifyChanged("Mean"); }
            }
            public int? StdEvpIndirect {
                get { return _StdEvp; }
                set { _StdEvp = value; NotifyChanged("StdEvp"); }
            }
            public int? MeanEvpIndirect {
                get { return _MeanEvp; }
                set { _MeanEvp = value; NotifyChanged("MeanEvp"); }
            }

            public int? Min { get { return DoScale(_Min); } }
            public int? Max { get { return DoScale(_Max); } }
            public int? AbsMax { get { return DoScale(_AbsMax); } }
            public int? Mean { get { return DoScale(_Mean); } }
            public int? StdEvp { get { return DoScale(_StdEvp); } }
            public int? MeanEvp { get { return DoScale(_MeanEvp); } }

            private int? DoScale(int? value) {
                return value.HasValue ? (int?)(int)((double)value.Value / Scale) : null;
            }

            public void SetScale(double scale) {
                Scale = scale;
                NotifyAll();
            }

            /// <summary>
            /// 変更の通知
            /// </summary>
            public void NotifyAll() {
                NotifyChanged("Min");
                NotifyChanged("Max");
                NotifyChanged("AbsMax");
                NotifyChanged("Mean");
                NotifyChanged("StdEvp");
                NotifyChanged("MeanEvp");
            }

            /// <summary>
            /// 変更の通知
            /// </summary>
            /// <param name="propertyName">プロパティ名</param>
            public void NotifyChanged(string propertyName) {
                var PropertyChanged = this.PropertyChanged;
                if (PropertyChanged != null) {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }

        public ObservableCollection<FV> FVList { get; private set; }

        CollectionViewSource viewSource;

        public MainWindow() {
            InitializeComponent();
            FVList = new ObservableCollection<FV>();
            viewSource = new CollectionViewSource() { Source = FVList };
            ListView1.DataContext = viewSource;
        }

        private void ButtonClear_Click(object sender, RoutedEventArgs e) {
            FVList.Clear();
        }

        private void ButtonApplyScale_Click(object sender, RoutedEventArgs e) {
            double scale;
            if (double.TryParse(TextBoxScale.Text.Trim(), out scale)) {
                foreach (FV fv in FVList) fv.SetScale(scale);
            } else {
                MessageBox.Show(this, "FV_SCALEは数値で入力してください。");
            }
        }

        private void Window_DragOver(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
        }

        private void Window_Drop(object sender, DragEventArgs e) {
            double scale;
            if (double.TryParse(TextBoxScale.Text.Trim(), out scale)) {
                foreach (FV fv in FVList) fv.SetScale(scale);
            } else {
                MessageBox.Show(this, "FV_SCALEは数値で入力してください。");
            }

            FVList.Clear();
            foreach (string path in e.Data.GetData(DataFormats.FileDrop) as string[]) {
                Add(path, scale);
            }
        }

        /// <summary>
        /// ファイルの追加
        /// </summary>
        /// <param propertyName="path"></param>
        private void Add(string path, double scale) {
            if (Directory.Exists(path)) {
                foreach (string s in Directory.GetFiles(path, "*", SearchOption.AllDirectories)) {
                    Add(s, scale);
                }
                return;
            } else if (File.Exists(path)) {
                if (string.Compare(System.IO.Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase) == 0) {
                    // Blunder風(?)CSV
                    foreach (string line in File.ReadAllLines(path)) {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var sp = line.Split(',');
                        if (2 <= sp.Length) {
                            AddFV(sp[0], () => sp.Skip(1).Select(x => short.Parse(x)), scale);
                        }
                    }
                } else {
                    // fv.binなど、符号付き2バイトの値が連続してるファイル
                    long size = new FileInfo(path).Length;
                    if (size % 2 != 0) {
                        //MessageBox.Show(this, path + " のサイズが奇数です。");
                        return;
                    }
                    AddFV(System.IO.Path.GetFileName(path), () => {
                        var bytes = File.ReadAllBytes(path);
                        var fv = new short[bytes.Length / 2];
                        Buffer.BlockCopy(bytes, 0, fv, 0, bytes.Length);
                        return fv;
                    }, scale);
                }
            } else {
                MessageBox.Show(this, path + " が存在しません。");
            }
        }

        /// <summary>
        /// 特徴の追加
        /// </summary>
        /// <param propertyName="propertyName">特徴の名前</param>
        /// <param propertyName="readFV"></param>
        private async void AddFV(string name, Func<IEnumerable<short>> readFV, double scale) {
            FV fv = new FV() { Name = name, Scale = scale };
            FVList.Add(fv);
            try {
                IEnumerable<short> data = await Task.Run(readFV);
                fv.MinIndirect = await Task.Run(() => data.Min());
                fv.MaxIndirect = await Task.Run(() => data.Max());

                IEnumerable<short> nz = await Task.Run(() => data.Where(x => 1 < Math.Abs(x)));
                if (nz.Any()) {
                    IEnumerable<short> nzAbs = await Task.Run(() => nz.Select(x => Math.Abs(x)));
                    fv.AbsMaxIndirect = await Task.Run(() => nzAbs.Max());
                    fv.MeanIndirect = await Task.Run(() => (int)nz.Average(x => (double)x));
                    fv.StdEvpIndirect = await Task.Run(() => (int)(Math.Sqrt(nz.Sum(x => (double)x * x)) / nz.Count()));
                    fv.MeanEvpIndirect = await Task.Run(() => (int)nzAbs.Average(x => (double)x));
                } else {
                    fv.AbsMaxIndirect = 0;
                    fv.MeanIndirect = 0;
                    fv.StdEvpIndirect = 0;
                    fv.MeanEvpIndirect = 0;
                }
            } catch (Exception e) {
                fv.Name += " (エラー：" + e.Message + ")";
                fv.NotifyChanged("Name");
                fv.MinIndirect = null;
                fv.MaxIndirect = null;
                fv.AbsMaxIndirect = null;
                fv.MeanIndirect = null;
                fv.StdEvpIndirect = null;
                fv.MeanEvpIndirect = null;
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e) {

        }

        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;

        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e) {
            GridViewColumnHeader headerClicked = (GridViewColumnHeader)e.OriginalSource;
            if (headerClicked.Role == GridViewColumnHeaderRole.Padding) return;

            ListSortDirection direction;
            if (headerClicked != _lastHeaderClicked) {
                if ((string)headerClicked.Tag == "Min") {
                    direction = ListSortDirection.Ascending;
                } else {
                    direction = ListSortDirection.Descending; // 初回は降順にする
                }
            } else {
                if (_lastDirection == ListSortDirection.Ascending) {
                    direction = ListSortDirection.Descending;
                } else {
                    direction = ListSortDirection.Ascending;
                }
            }

            // ソート。await での値の設定が完了する前にやると変な事になるのだが…。

            switch ((string)headerClicked.Tag) {
                case "Name": SetSort("Name", direction); break;
                case "Min": SetSort("Min", direction); break;
                case "Max": SetSort("Max", direction); break;
                case "AbsMax": SetSort("AbsMax", direction); break;
                case "Mean": SetSort("Mean", direction); break;
                case "StdEvp": SetSort("StdEvp", direction); break;
                case "MeanEvp": SetSort("MeanEvp", direction); break;
            }

            //if (direction == ListSortDirection.Ascending) {
            //    headerClicked.Column.HeaderTemplate =
            //        Resources["HeaderTemplateArrowUp"] as DataTemplate;
            //} else {
            //    headerClicked.Column.HeaderTemplate =
            //        Resources["HeaderTemplateArrowDown"] as DataTemplate;
            //}
            //if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked) {
            //    _lastHeaderClicked.Column.HeaderTemplate = null;
            //}

            _lastHeaderClicked = headerClicked;
            _lastDirection = direction;

            //if ((string)columnHeader.Tag
        }

        private void SetSort(string propertyName, ListSortDirection direction) {
            viewSource.SortDescriptions.Clear();
            if (propertyName == "Name") {
                viewSource.SortDescriptions.Add(new SortDescription(propertyName, direction));
            } else {
                viewSource.SortDescriptions.Add(new SortDescription(propertyName + "Indirect", direction));
                viewSource.SortDescriptions.Add(new SortDescription(propertyName + "Indirect", direction));
            }
        }
    }
}
