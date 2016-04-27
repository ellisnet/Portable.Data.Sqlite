using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

using SampleApp.ViewModels;

namespace SampleApp.Views {
    public partial class SqliteTestPage : ContentPage {

        private readonly SqliteTestPageViewModel vm;

        public SqliteTestPage() {
            InitializeComponent();
            BindingContext = vm = new SqliteTestPageViewModel(this);
            uiRunTests.Clicked += async (sender, e) => { await vm.RunTestsCommand(sender, e); };
        }
    }
}
