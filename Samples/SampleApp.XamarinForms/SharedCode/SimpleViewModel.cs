/*
   Copyright 2014 Ellisnet - Jeremy Ellis (jeremy@ellisnet.com)

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

//FILE DATE/REVISION: 2/2/2016

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Xamarin.Forms;

// ReSharper disable once CheckNamespace
public abstract class SimpleViewModel : INotifyPropertyChanged, IDisposable {

    //How to use your class that inherits from this class:

    //Your view-model class that inherits from SimpleViewModel (MyPageViewModel in these examples)
    //  should have a constructor that passes the Page instance to the base constructor, like this:

    //public MyPageViewModel(Page view) : base(view) {}

    //In the MyPage.xaml.cs "code-behind" class for your view, you will need a private  
    //  variable for storing your view-model that inherits from this class - like:

    //private readonly MyPageViewModel vm;

    //Then you need to create an instance of your view-model in the constructor of your code-behind
    //  class and assign it to the view's BindingContext - like:

    //InitializeComponent();
    //BindingContext = vm = new MyPageViewModel(this);

    //Then, still in the constructor, you probably want to connect up your command-calling controls 
    //  to your view-model commands - like:

    //SampleButton.Clicked += async (sender, e) => { await vm.SampleCommand(sender, e); };

    //Here is what that SampleCommand could look like - in your view-model class:

    //public async Task<bool> SampleCommand(object sender, EventArgs e) {
    //    bool answer = await _view.DisplayAlert("Are you sure?", "Are you sure you want to do this?", "Yes", "No");
    //    if (answer) {
    //        doSomeWork();
    //        _sampleProperty = await doSomeOtherWorkAsync();
    //        NotifyPropertyChanged("SampleProperty");
    //    }
    //    return true;
    //}

    //Here is what that SampleProperty could look like - in your view-model class:

    //private bool _sampleProperty;
    //public bool SampleProperty {
    //    get { return _sampleProperty; }
    //    set { _sampleProperty = value; ThisPropertyChanged(); }
    //}

    //And how you could bind to that property in your Xaml page:

    //<Switch IsToggled="{Binding SampleProperty, Mode=TwoWay}"/>

    public event PropertyChangedEventHandler PropertyChanged;

    // ReSharper disable once InconsistentNaming
    protected Page _view;

    protected SimpleViewModel(Page view) {
        _view = view;
    }

    protected virtual void NotifyPropertyChanged(string propertyName) {
        if ((!String.IsNullOrWhiteSpace(propertyName))) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    protected virtual void ThisPropertyChanged([CallerMemberName] string propertyName = "") {
        NotifyPropertyChanged(propertyName);
    }

    public virtual void Dispose() {
        // remove event handlers before setting event to null
        Delegate[] delegates = PropertyChanged?.GetInvocationList();
        if ((delegates?.Length ?? 0) > 0) {
            // ReSharper disable once PossibleNullReferenceException
            foreach (var d in delegates) {
                PropertyChanged -= (PropertyChangedEventHandler)d;
            }
        }
        PropertyChanged = null;
        _view = null;
    }

}
