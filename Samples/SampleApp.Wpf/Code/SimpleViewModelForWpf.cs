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

//FILE DATE/REVISION: 12/29/2015

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

// ReSharper disable once CheckNamespace
public abstract class SimpleViewModel : INotifyPropertyChanged, IDisposable {

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void NotifyPropertyChanged(string propertyName) {
        if ((!String.IsNullOrWhiteSpace(propertyName))) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    protected virtual void ThisPropertyChanged([CallerMemberName] string propertyName = "") {
        NotifyPropertyChanged(propertyName);
    }

    public void Dispose() {
        // remove event handlers before setting event to null
        Delegate[] delegates = PropertyChanged?.GetInvocationList();
        if ((delegates?.Length ?? 0) > 0) {
            // ReSharper disable once PossibleNullReferenceException
            foreach (var d in delegates) {
                PropertyChanged -= (PropertyChangedEventHandler)d;
            }
        }
        PropertyChanged = null;
    }
}

public class SimpleCommand : ICommand, IDisposable {

    private Func<object, bool> _canExecuteFunction; //allows passing an object parameter to function
    private Func<object, bool> _executeFunction; //allows passing an object parameter to function
    private Func<bool> _canExecuteFunctionNoParameter; //no parameter passing
    private Func<bool> _executeFunctionNoParameter; //no parameter passing

    public SimpleCommand(Func<object, bool> canExecuteFunction, Func<object, bool> executeFunction) {
        _canExecuteFunction = canExecuteFunction;
        _executeFunction = executeFunction;
    }

    public SimpleCommand(Func<bool> canExecuteFunctionNoParameter, Func<object, bool> executeFunction) {
        _canExecuteFunctionNoParameter = canExecuteFunctionNoParameter;
        _executeFunction = executeFunction;
    }

    public SimpleCommand(Func<object, bool> canExecuteFunction, Func<bool> executeFunctionNoParameter) {
        _canExecuteFunction = canExecuteFunction;
        _executeFunctionNoParameter = executeFunctionNoParameter;
    }

    public SimpleCommand(Func<bool> canExecuteFunctionNoParameter, Func<bool> executeFunctionNoParameter) {
        _canExecuteFunctionNoParameter = canExecuteFunctionNoParameter;
        _executeFunctionNoParameter = executeFunctionNoParameter;
    }

    public event EventHandler CanExecuteChanged;

    public void RaiseCanExecuteChanged() {
        if (CanExecuteChanged != null) {
            Application.Current.Dispatcher.Invoke(() => { CanExecuteChanged(this, EventArgs.Empty); });
        }
    }

    public bool CanExecute(object parameter = null) {
        return (_executeFunction != null || _executeFunctionNoParameter != null)
            && ((_canExecuteFunction != null && _canExecuteFunction.Invoke(parameter)) 
            || (_canExecuteFunctionNoParameter != null && _canExecuteFunctionNoParameter.Invoke()));
    }

    public void Execute(object parameter = null) {
        if (_executeFunction != null) {
            _executeFunction.Invoke(parameter);
        }
        else {
            _executeFunctionNoParameter?.Invoke();
        }
    }

    public void Dispose() {
        // remove event handlers before setting event to null
        Delegate[] delegates = CanExecuteChanged?.GetInvocationList();
        if ((delegates?.Length ?? 0) > 0) {
            // ReSharper disable once PossibleNullReferenceException
            foreach (var d in delegates) {
                CanExecuteChanged -= (EventHandler)d;
            }
        }
        CanExecuteChanged = null;

        _canExecuteFunction = null;
        _executeFunction = null;
        _canExecuteFunctionNoParameter = null;
        _executeFunctionNoParameter = null;
    }
}

/*

    Notes:
    - You will want to create a class that inherits from this SimpleViewModel for your XAML window -
        e.g. for the MainWindow.xaml window, you might want to create a MainViewModel class that inherits from SimpleViewModel

    - You will want to add the ViewModel classes to the Application.Resources section in the App.xaml file:
        <Application.Resources>
            <!-- Note that since my ViewModels are in a separate 'MyApplication.ViewModels' namespace -
                I needed to add the following namespace reference line in the Application tag above:
                xmlns:viewModels="clr-namespace:MyApplication.ViewModels" -->
            <viewModels:MainViewModel x:Key="MainViewModel" /> <!-- For use with MainWindow.xaml -->
            <viewModels:SecondViewModel x:Key="SecondViewModel" /> <!-- For use with SecondWindow.xaml -->
            <viewModels:ThirdViewModel x:Key="ThirdViewModel" /> <!-- For use with ThirdWindow.xaml -->
        </Application.Resources>

    - In the Window tag at the top of XAML file - MainWindow.xaml in this example - you will need to add the following:
        DataContext="{StaticResource MainViewModel}"
        (e.g. this could go under the 'mc:Ignorable="d"' line)

    - Here are some sample binding expressions:
    <Label x:Name="ErrorMessageLabel" Content="{Binding ErrorMessage}" Visibility="{Binding ErrorMessageVisibility}" />
        <!-- Bound to the ErrorMessage (string) property and ErrorMessageVisibility (Visibility) property on the ViewModel -->

    <ComboBox x:Name="DataOptions" IsEnabled="{Binding IsDataOptionsEnabled}" ItemsSource="{Binding OptionsDictionary}"
        DisplayMemberPath="Value" SelectedValuePath="Key" SelectedValue="{Binding SelectedDataOption, Mode=TwoWay}" />
        <!-- Bound to the IsDataOptionsEnabled (boolean) and OptionsDictionary (Dictionary<int, string>) and SelectedDataOption (int)
            properties on the ViewModel -->
      
    <Checkbox x:Name="OverwriteAll" Content="Overwrite all values?" IsChecked="{Binding IsOverwriteAllChecked, Mode=TwoWay}" />
        <!-- Bound to the IsOverwriteAllChecked (boolean) property on the ViewModel -->


    <Button x:Name="CopyDataButton" Content="Copy Data!" Command="{Binding CopyDataCommand}" />
        <!-- Bound to the CopyDataCommand (SimpleCommand) property on the ViewModel - note that whether the button is enabled or not is
            determined by the value returned by the CanExecute() method on the SimpleCommand -->

    - Declaring a property which is an implementation of SimpleCommand - here is the implementation of the CopyDataCommand referenced above:
        private SimpleCommand _copyDataCommand;
        public SimpleCommand CopyDataCommand {
            get {
                _copyDataCommand = _copyDataCommand ??
                    new SimpleCommand(
                        () => this.SelectedDataOption > 0 && this.IsThereData,
                        () => {
                            Task.Run(() => {
                                this.SetUiEnabled(false); // a function that toggles the "enabled" status of UI elements
                                string message;
                                bool success = this.DoCopyData(out message);
                                copyOperationComplete?.Invoke(success, message);  // invoke any handlers on the copyOperationComplete event
                                this.SetUiEnabled(true); // re-enable the UI
                            });
                            return true;
                    });
                return _copyDataCommand;
            }
        }

*/
