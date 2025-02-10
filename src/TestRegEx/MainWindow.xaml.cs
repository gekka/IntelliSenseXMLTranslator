namespace WpfApp1
{
    using System;
    using System.Linq;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            MainModel mm = new MainModel();

            mm.Items.Add(new Item() { Text = "<member><summary /></member>" });
            mm.Items.Add(new Item() { Text = "<member><summary>A<see />B</summary></member>" });
            mm.Items.Add(new Item() { Text = "<member><summary > <X> A <y/> <see />B</X> </summary> </member>" });
            mm.Pattern = "(<(?<X>summary)[^<]*/>)|(<(?<X>summary).*?</\\k<X>>)";
            this.DataContext = mm;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }

    class MainModel : ModelBase
    {
        public MainModel()
        {
            this.Items.CollectionChanged += Items_CollectionChanged;
        }

        private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<Item>())
                {
                    item.PropertyChanged += item_PropertyChanged;
                }
            }
        }

        private void item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var reg = this.reg;
            ((Item)sender).Update(this.reg);
        }

        public System.Collections.ObjectModel.ObservableCollection<Item> Items { get; } = new System.Collections.ObjectModel.ObservableCollection<Item>();

        System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex("");

        /// <summary></summary>
        public string Pattern
        {
            get => _Pattern;
            set
            {
                if (SetValue(value, ref _Pattern))
                {
                    try
                    {
                        this.reg = null;
                        this.reg = new System.Text.RegularExpressions.Regex(value);

                        Success = true;

                        foreach (var item in Items)
                        {
                            item.Update(reg);
                        }
                        this.Message = "";
                    }
                    catch(Exception ex)
                    {
                        foreach (var item in Items)
                        {
                            item.Success = false;
                            item.Result = "";
                        }
                        Success = false;

                        this.Message = ex.Message;
                    }
                }
            }
        }
        private string _Pattern = "";

        /// <summary></summary>
        public bool Success
        {
            get => _Success;
            set => SetValue(value, ref _Success);
        }
        private bool _Success;

        /// <summary></summary>
        public string Message
        {
            get => _Message;
            set => SetValue(value, ref _Message);
        }
        private string _Message;




    }


    class Item : ModelBase
    {
        /// <summary></summary>
        public string Text
        {
            get => _Text;
            set => SetValue(value, ref _Text);
        }
        private string _Text;


        /// <summary></summary>
        public string Result
        {
            get => _Result;
            set => SetValue(value, ref _Result);
        }
        private string _Result;

        /// <summary></summary>
        public bool Success
        {
            get => _Success;
            set => SetValue(value, ref _Success);
        }
        private bool _Success;

        public void Update(System.Text.RegularExpressions.Regex reg)
        {
            if (reg == null)
            {
                this.Result = "";
                this.Success = false;
            }
            else
            {
                var m = reg.Match(Text);
                this.Success = m.Success;

                if (m.Success)
                {
                    this.Result = m.Value;
                }
                else
                {
                    this.Result = "";
                }
            }
        }
    }

    class ModelBase : System.ComponentModel.INotifyPropertyChanged
    {

        #region INotifyPropertyChanged メンバ

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
        }
        protected virtual void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        protected bool SetValue<T>(T newValue, ref T field, [System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            if (object.Equals(newValue, field))
            {
                return false;
            }
            else
            {
                field = newValue;
                OnPropertyChanged(name);
                return true;
            }
        }
        protected bool SetValue<T>(ref T field, T newValue, [System.Runtime.CompilerServices.CallerMemberName] string name = null)
            => SetValue<T>(newValue, ref field, name);

        protected bool SetValue<T>(T newValue, ref T field, Action<T> pre, [System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            if (object.Equals(newValue, field))
            {
                return false;
            }
            else
            {
                pre(newValue);

                field = newValue;
                OnPropertyChanged(name);
                return true;
            }
        }
        protected bool SetValue<T>(ref T field, T newValue, Action<T> pre, [System.Runtime.CompilerServices.CallerMemberName] string name = null)
            => SetValue(newValue, ref field, pre, name);

        #endregion

    }
}