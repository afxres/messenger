using Mikodev.Logger;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Messenger
{
    public partial class App : Application
    {
        public event EventHandler<KeyEventArgs> TextBoxKeyDown;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += (s, arg) =>
            {
                arg.Handled = true;
                MessageBox.Show(arg.Exception.ToString(), "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            };

            void _Close(object sender, EventArgs args)
            {
                Framework.Close();
                Log.Close();
            }

            Exit += _Close;
            SessionEnding += _Close;

            Log.Run(nameof(Messenger) + ".log");
            EventManager.RegisterClassHandler(typeof(TextBox), UIElement.KeyDownEvent, new KeyEventHandler((s, arg) => TextBoxKeyDown?.Invoke(s, arg)));
            Framework.Start();
        }
    }
}
