using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using Microsoft.PowerShell.Host.ISE;
using System.Management;
using System.Collections.ObjectModel;
using System.Security.Principal;

namespace PSMag
{
    public partial class WMIEventsAddon : UserControl, IAddOnToolHostObject
    {
        ObservableCollection<FilterData> _FilterCollection = new ObservableCollection<FilterData>();
        ObservableCollection<ConsumerData> _ConsumerCollection = new ObservableCollection<ConsumerData>();
        ObservableCollection<BindingData> _BindingCollection = new ObservableCollection<BindingData>();

        public WMIEventsAddon()
        {
            InitializeComponent();
            if (IsAdmin()) {
                GetFilters();
                GetConsumers();
                GetBindings();
            }
            else
            {
                IsAdministrator.Content = "This add-on requires administrative privileges";
            }
        }

        public ObservableCollection<ConsumerData> ConsumerCollection
        { 
            get { 
                return _ConsumerCollection; 
            }
        }

        public ObservableCollection<FilterData> FilterCollection
        {
            get
            {
                return _FilterCollection;
            }
        }

        public ObservableCollection<BindingData> BindingCollection
        {
            get
            {
                return _BindingCollection;
            }
        }

        public ObjectModelRoot HostObject
        {
            get;
            set;
        }

        public void GetFilters()
        {
            _FilterCollection.Clear();
            
            ManagementScope Scope;
            Scope = new ManagementScope(String.Format("\\\\{0}\\root\\Subscription", ComputerName.Text.ToString()), null);
            Scope.Connect();
            
            SelectQuery WmiQuery = new SelectQuery("SELECT * FROM __EventFilter");
            ManagementObjectSearcher filterlist = new ManagementObjectSearcher(Scope, WmiQuery);
            
            foreach (ManagementObject filter in filterlist.Get())
            {
                _FilterCollection.Add(new FilterData
                {
                    FilterName = filter.Properties["Name"].Value.ToString(),
                    FilterQuery = filter.Properties["Query"].Value.ToString()
                });
            }
        }

        public void GetConsumers()
        {
            _ConsumerCollection.Clear();
            string[] ConsumerTypeArray = new string[5];
            ConsumerTypeArray[0] = "ActiveScriptEventConsumer";
            ConsumerTypeArray[1] = "CommandLineEventConsumer";
            ConsumerTypeArray[2] = "LogFileEventConsumer";
            ConsumerTypeArray[3] = "NTEventLogEventConsumer";
            ConsumerTypeArray[4] = "SMTPEventConsumer";
            foreach (string type in ConsumerTypeArray) {
                ManagementScope Scope;
                Scope = new ManagementScope(String.Format("\\\\{0}\\root\\Subscription", ComputerName.Text.ToString()), null);
                Scope.Connect();

                SelectQuery WmiQuery = new SelectQuery("SELECT * FROM " + type);
                ManagementObjectSearcher consumerlist = new ManagementObjectSearcher(Scope, WmiQuery);
                foreach (ManagementObject consumer in consumerlist.Get())
                {
                    _ConsumerCollection.Add(new ConsumerData
                    {
                        ConsumerName = consumer.Properties["Name"].Value.ToString(),
                        ConsumerType = type
                    });
                }
            }
        }

        public void GetBindings()
        {
            _BindingCollection.Clear();
            ManagementScope Scope;
            Scope = new ManagementScope(String.Format("\\\\{0}\\root\\Subscription", ComputerName.Text.ToString()), null);
            Scope.Connect();

            SelectQuery WmiQuery = new SelectQuery("SELECT * FROM __FilterToConsumerBinding");
            ManagementObjectSearcher bindinglist = new ManagementObjectSearcher(Scope, WmiQuery);
            foreach (ManagementObject binding in bindinglist.Get())
            {
                Regex reg = new Regex("[^\"]*");
                MatchCollection filter = reg.Matches(binding["filter"].ToString());
                MatchCollection consumer = reg.Matches(binding["consumer"].ToString());

                _BindingCollection.Add(new BindingData
                {
                    FilterName = filter[2].ToString(),
                    ConsumerName = consumer[2].ToString(), 
                    BindingPath = binding.Path.ToString()
                });
            }
        }

        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterDelete.IsEnabled = true;
        }
        private void Consumers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ConsumerDelete.IsEnabled = true;
        }

        private void Bindings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BindingDelete.IsEnabled = true;
        }

        private void FilterDelete_Click(object sender, RoutedEventArgs e)
        {
            FilterData FilterView = Filters.SelectedItem as FilterData;
            if (ConfirmDeletion("This filter may be a part of an existing event binding. Are you sure you want to delete this filter?", "Delete") == true) {
                try
                {
                    ManagementScope Scope;
                    Scope = new ManagementScope(String.Format("\\\\{0}\\root\\Subscription", ComputerName.Text.ToString()), null);
                    Scope.Connect();

                    SelectQuery WmiQuery = new SelectQuery("SELECT * FROM __EventFilter WHERE Name='" + FilterView.FilterName.ToString() + "'");
                    ManagementObjectSearcher filterlist = new ManagementObjectSearcher(Scope, WmiQuery);

                    foreach (ManagementObject seletcedfilter in filterlist.Get())
                    {
                        seletcedfilter.Delete();
                        _FilterCollection.Remove(FilterView);
                    }
                }

                catch (Exception ex)
                {
                    throw ex;
                }    
            }
        }

        private void ConsumerDelete_Click(object sender, RoutedEventArgs e)
        {
            ConsumerData ConsumerView = Consumers.SelectedItem as ConsumerData;
            if (ConfirmDeletion("This consumer may be a part of an existing event binding. Are you sure you want to delete this consumer?", "Delete") == true)
            {
                try
                {
                    ManagementScope Scope;
                    Scope = new ManagementScope(String.Format("\\\\{0}\\root\\Subscription", ComputerName.Text.ToString()), null);
                    Scope.Connect();

                    SelectQuery WmiQuery = new SelectQuery("SELECT * FROM " + ConsumerView.ConsumerType + " WHERE Name='" + ConsumerView.ConsumerName.ToString() + "'");
                    ManagementObjectSearcher consumerlist = new ManagementObjectSearcher(Scope, WmiQuery);

                    foreach (ManagementObject selectedconsumer in consumerlist.Get())
                    {
                        selectedconsumer.Delete();
                        _ConsumerCollection.Remove(ConsumerView);
                    }
                }

                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        private void BindingDelete_Click(object sender, RoutedEventArgs e)
        {
            BindingData BindingView = Bindings.SelectedItem as BindingData;
            if (ConfirmDeletion("Are you sure you want to delete this binding?", "Delete") == true) {
                try
                {
                    ManagementObject binding = new ManagementObject(BindingView.BindingPath);
                    binding.Delete();
                    _BindingCollection.Remove(BindingView);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        private bool ConfirmDeletion(string sMsgText, string sCaptionText)
        {
            string sMessageBoxText = sMsgText;
            string sCaption = sCaptionText;

            MessageBoxButton btnMessageBox = MessageBoxButton.YesNo;
            MessageBoxImage icnMessageBox = MessageBoxImage.Warning;

            MessageBoxResult rsltMessageBox = MessageBox.Show(sMessageBoxText, sCaption, btnMessageBox, icnMessageBox);
            switch (rsltMessageBox)
            {
                case MessageBoxResult.Yes:
                    return true;

                case MessageBoxResult.No:
                    return false;
            }

            return false;
        }

        private static bool IsAdmin()
        {
            WindowsIdentity CurrentIdentity = WindowsIdentity.GetCurrent();
            WindowsPrincipal UserPrincipal = new WindowsPrincipal(CurrentIdentity);
            return UserPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void refreshLists()
        {
            GetFilters();
            GetConsumers();
            GetBindings();
        }
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            refreshLists();
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            refreshLists();
        }
    }
}

    public class ConsumerData
    {
        public string ConsumerName { get; set; }
        public string ConsumerType { get; set; }
    }

    public class FilterData
    {
        public string FilterName { get; set; }
        public string FilterQuery { get; set; }
    }

    public class BindingData
    {
        public string FilterName { get; set; }
        public string ConsumerName { get; set; }
        public string BindingPath { get; set; }
    }
