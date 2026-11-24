using System;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TrueStory
{


    public class CommandConsole
    {
        public event OnExecuteCommandHandler? OnExecuteCommand;
        public event OnCommandExecutedHandler? OnCommandExecuted;

        public delegate void OnExecuteCommandHandler(string command);
        public delegate void OnCommandExecutedHandler(string command, string output);

        public Story? Story { get { return story; } set { story = value; OnStoryChanged(); } }

        Process? process;

        private void OnStoryChanged()
        {
            if (story == null)
            {
                textBox.IsEnabled = false;
                textBox.Visibility = Visibility.Collapsed;
                label.Visibility = Visibility.Collapsed;

                if (process != null)
                {
                    StopProcess();
                }
            }
            else
            {
                textBox.IsEnabled = story.Header.commandConsoleEnabled;
                textBox.Visibility = story.Header.commandConsoleEnabled ? Visibility.Visible : Visibility.Collapsed;
                label.Visibility = story.Header.commandConsoleEnabled ? Visibility.Visible : Visibility.Collapsed;

                StartProcess();

            }
        }

        private void StopProcess()
        {
            Debug.Assert(process != null);

            process.OutputDataReceived -= OnProcessOutput;
            process.ErrorDataReceived -= OnProcessError;

            process.Exited -= OnProcessExited;

            process.Kill();
            process.Dispose();
            process = null;
        }

        private void StartProcess()
        {
            process = new();
            ProcessStartInfo startInfo = new();
            startInfo.FileName = "cmd.exe";
            startInfo.WorkingDirectory = story.RootPath;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            process.StartInfo = startInfo;

            process.OutputDataReceived += OnProcessOutput;
            process.ErrorDataReceived += OnProcessError;


            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.Exited += OnProcessExited;

            process.EnableRaisingEvents = true;


        }

        private void OnProcessExited(object? sender, EventArgs e)
        {
            StartProcess();
        }

        private void OnProcessError(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                textBox.Dispatcher.Invoke(() =>
                {
                    textBox.AppendText(e.Data);
                    textBox.CaretIndex += e.Data.Length;
                    textBox.AppendText(Environment.NewLine);
                    textBox.CaretIndex += Environment.NewLine.Length;

                    textBox.ScrollToEnd();
                });

            }
        }

        private void OnProcessOutput(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                textBox.Dispatcher.Invoke(() =>
                {
                    textBox.AppendText(e.Data);
                    textBox.CaretIndex += e.Data.Length;
                    textBox.AppendText(Environment.NewLine);
                    textBox.CaretIndex += Environment.NewLine.Length;

                    textBox.ScrollToEnd();
                });


            }
        }

        Story? story;

        TextBox textBox;
        Label label;

        StringBuilder command;

        KeyConverter keyConverter;

        public CommandConsole(TextBox _textBox, Label _label)
        {
            DataObject.AddPastingHandler(_textBox, OnFileContentsPaste);
            DataObject.AddCopyingHandler(_textBox, OnFileContentsCopy);

            _textBox.ContextMenu = null;

            _textBox.PreviewKeyDown += OnKeyDown;
            _textBox.PreviewTextInput += OnTextInput;

            _textBox.IsReadOnly = true;

            textBox = _textBox;
            label = _label;

            command = new();

            keyConverter = new();

        }

        public void Clear()
        {
            command.Clear();
            textBox.Text = Constants.consolePrompt;
            textBox.CaretIndex = Constants.consolePrompt.Length;
        }

        private void OnTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text != Environment.NewLine && e.Text != "\r")
            {
                textBox.AppendText(e.Text);
                textBox.CaretIndex += e.Text.Length;

                command.Append(e.Text);

            }

        }        

        void OnKeyDown(object sender, KeyEventArgs e)
        {
            Debug.Assert(process != null);

            if (e.Key == Key.Return)
            {
                string commandText = command.ToString();

                int previousCaret = textBox.CaretIndex;

                textBox.CaretIndex -= commandText.Length;

                textBox.Text = textBox.Text.Substring(0, textBox.CaretIndex);

                textBox.CaretIndex = previousCaret;

                OnCommandExecuted?.Invoke(commandText, "");

                command.Clear();

                process.StandardInput.WriteLine(commandText);

            }
            else if (e.Key == Key.Back)
            {
                if (command.Length > 0)
                {
                    textBox.Text = textBox.Text.Substring(0, textBox.Text.Length - 1);
                    textBox.CaretIndex = textBox.Text.Length;

                    command.Remove(command.Length - 1, 1);
                }
            }
            else if (e.Key == Key.Space)
            {
                textBox.AppendText(" ");
                textBox.CaretIndex++;

                command.Append(" ");
            }
        }


        void OnFileContentsCopy(object sender, DataObjectCopyingEventArgs e)
        {
            e.CancelCommand();
        }

        void OnFileContentsPaste(object sender, DataObjectPastingEventArgs e)
        {
            e.CancelCommand();
        }


    }
}
