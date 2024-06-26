using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management.Automation;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GnirehtetCompanion
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Process _process;

        //private String _localPort = "foobar";
        //private String _deviceName = "emulator-5554";
        //private String _server = "156.65.182.110";

        private String _localPort;
        private String _deviceName;
        private String _server;

        private String registryKey;
        private String registryValue;
        private String _serverPersisted;

        private String _exePath = "gnirehtet.exe";
        //private String _workingDir = "D:\\mich\\projects\\android\\gnirehtet_reverse_tunnel";

        private PowerShell _ps;

        public MainWindow()
        {
            InitializeComponent();

            _ps = PowerShell.Create();
            RefreshDevices();

            const string userRoot = "HKEY_CURRENT_USER";
            const string subkey = "GnirehtetCompanion";
            registryKey = "HKEY_CURRENT_USER\\GnirehtetCompanion";
            registryValue = "server";

            var result = Registry.GetValue(registryKey, registryValue, null);
            if (result != null)
            {
                this.ServerTextBox.Text = result.ToString();
            }
        }

        private async void StartTunnel()
        {
            if (_process == null && (_localPort != null && _deviceName != null && _server != null))
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = _exePath,
                    Arguments = $"run -p {_localPort} {_deviceName} -d {_server}", // Add any arguments if needed
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    //WorkingDirectory = _workingDir
                };

                _process = new Process
                {
                    StartInfo = startInfo
                };

                var outputBuilder = new StringBuilder();
                _process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        outputBuilder.AppendLine(e.Data);
                        Dispatcher.Invoke(() => OutputTextBox.Text += e.Data + Environment.NewLine);
                    }
                };

                var errorBuilder = new StringBuilder();
                _process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        errorBuilder.AppendLine(e.Data);
                        Dispatcher.Invoke(() => OutputTextBox.Text += e.Data + Environment.NewLine);
                    }
                };

                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();

                await Task.Run(() => _process.WaitForExit());

                Registry.SetValue(registryKey, registryValue, _server);
            } else
            {
                MessageBox.Show("Tunnel already in progress.");
            }
        }

        private async void StopTunnel()
        {
            if( _process != null )
            {
                try
                {
                    _process.Kill();
                    _process.Dispose();
                } catch (Exception ex)
                {
                    //
                }
                
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = _exePath,
                Arguments = "stop", // Add any arguments if needed
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                //WorkingDirectory = _workingDir
            };

            _process = new Process
            {
                StartInfo = startInfo
            };

            var outputBuilder = new StringBuilder();
            _process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                    Dispatcher.Invoke(() => OutputTextBox.Text += e.Data + Environment.NewLine);
                }
            };

            var errorBuilder = new StringBuilder();
            _process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    errorBuilder.AppendLine(e.Data);
                    Dispatcher.Invoke(() => OutputTextBox.Text += e.Data + Environment.NewLine);
                }
            };

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            await Task.Run(() => _process.WaitForExit());
            _process.Kill();
            _process.Dispose();
            _process = null;
        }

        private void OutputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            OutputTextBox.ScrollToEnd();
        }

        private void HandleDeviceNameChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            _deviceName = textBox.Text;
        }

        private void HandleLocalPortChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            _localPort = textBox.Text;
        }

        private void HandleServerChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            _server = textBox.Text;
        }

        //private void HandleServerChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    ComboBox comboBox = sender as ComboBox;
        //    _server = comboBox.SelectedItem.ToString();
        //    MessageBox.Show(_server);
        //}

        private void HandleStartTunnel(object sender, RoutedEventArgs e)
        {
            StartTunnel();
        }

        private void HandleStopTunnel(object sender, RoutedEventArgs e)
        {
            StopTunnel();
        }

        private void HandleRefreshDevices(object sender, RoutedEventArgs e)
        {
            RefreshDevices();
        }

        private void RefreshDevices()
        {
            _ps.Commands.Clear();
            _ps.AddCommand("Invoke-Expression");
            _ps.AddArgument("adb devices");
            var res = _ps.Invoke();
            if (res.Count > 0)
            {
                DeviceList.Children.Clear();

                for (int i = 1; i < res.Count; i++)
                {

                    var row = res[i].ToString().Split('\t')[0];
                    if (row.Trim().Length > 0)
                    {

                        var deviceBtn = new Button();
                        deviceBtn.Content = row;
                        deviceBtn.ToolTip = row;
                        deviceBtn.Style = Application.Current.FindResource("MaterialDesignFlatButton") as Style;
                        deviceBtn.Click += (s, e) =>
                        {
                            DeviceTextBox.Text = row;

                        };
                        DeviceList.Children.Add(deviceBtn);
                    }

                }
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            StopTunnel();
        }


    }

}