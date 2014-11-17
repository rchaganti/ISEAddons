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
            GetFilters();
            GetConsumers();
            GetBindings();
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
            ManagementClass Allfilters = new ManagementClass(@"root\subscription:__EventFilter");
            ManagementObjectCollection filterlist = Allfilters.GetInstances();
            foreach (ManagementObject filter in filterlist)
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
                ManagementClass AllConsumers = new ManagementClass(@"root\subscription:" + type);
                ManagementObjectCollection consumerlist = AllConsumers.GetInstances();
                foreach (ManagementObject consumer in consumerlist)
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
            ManagementClass AllBindings = new ManagementClass(@"root\subscription:__FilterToConsumerBinding");
            ManagementObjectCollection bindinglist = AllBindings.GetInstances();
            foreach (ManagementObject binding in bindinglist)
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
                    ManagementObject seletcedfilter = new ManagementObject(@"root\subscription:__EventFilter.Name='" + FilterView.FilterName.ToString() + "'");
                    seletcedfilter.Delete();
                    _FilterCollection.Remove(FilterView);
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
                    ManagementObject selectedconsumer = new ManagementObject(@"root\subscription:" + ConsumerView.ConsumerType + ".Name='" + ConsumerView.ConsumerName + "'");
                    selectedconsumer.Delete();
                    _ConsumerCollection.Remove(ConsumerView);
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
}
