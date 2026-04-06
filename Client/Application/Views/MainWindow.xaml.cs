using Client.Application.Components;
using Client.Application.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
using System.Collections.Specialized;
using System.Windows.Shapes;

namespace Client.Application.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel mainViewModel;
        private readonly AIConfigViewModel aiConfigViewModel;
        private AIConfig aiConfigView;

        public MainWindow(MainViewModel mainViewModel, AIConfigViewModel aiConfigViewModel)
        {
            InitializeComponent();

            DataContext = mainViewModel;
            this.mainViewModel = mainViewModel;
            this.aiConfigViewModel = aiConfigViewModel;
            aiConfigView = new AIConfig(aiConfigViewModel);

            mainViewModel.ChatMessages.CollectionChanged += AutoScrollList(listBox);
            mainViewModel.ActivityLog.CollectionChanged += AutoScrollList(activityLogList);
        }

        private static NotifyCollectionChangedEventHandler AutoScrollList(ListBox list)
        {
            return (s, e) =>
            {
                if (list.Items.Count > 0)
                {
                    list.ScrollIntoView(list.Items[list.Items.Count - 1]);
                }
            };
        }

        private void AIConfig_Click(object sender, RoutedEventArgs e)
        {
            aiConfigView.Owner = this;
            aiConfigView.ShowDialog();
        }
    }
}
