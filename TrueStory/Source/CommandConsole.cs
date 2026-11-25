using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

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

        bool processOutputReaderStop = false;
        Thread? processOutputReader;

        bool processErrorReaderStop = false;
        Thread? processErrorReader;

        private void OnStoryChanged()
        {
            if (story == null)
            {
                textBox.Clear();
                textBox.IsEnabled = false;
                textBox.Visibility = Visibility.Collapsed;
                label.Visibility = Visibility.Collapsed;

                if (IsProcessRunning())
                {
                    StopProcess();
                }

            }
            else
            {
                textBox.IsEnabled = story.Header.commandConsoleEnabled;
                textBox.Visibility = story.Header.commandConsoleEnabled ? Visibility.Visible : Visibility.Collapsed;
                label.Visibility = story.Header.commandConsoleEnabled ? Visibility.Visible : Visibility.Collapsed;

                if(story.Header.commandConsoleEnabled)
                {
                    StartProcess();
                }

            }
        }

        private bool IsProcessRunning()
        {
            return (process != null);
        }

        private void StopProcess()
        {
            Debug.Assert(process != null);
            Debug.Assert(processOutputReader != null);
            Debug.Assert(processErrorReader != null);

            process.Exited -= OnProcessExited;
            
            processOutputReaderStop = true;
            processErrorReaderStop = true;

            process.Kill();
            process.Dispose();

            processOutputReader.Join();
            processErrorReader.Join();

            processOutputReaderStop = false;
            processErrorReaderStop = false;

            process = null;

            Console.WriteLine("Process stopped");
        }

        private void PumpStream(StreamReader stream, ref bool stopFlag)
        {
            char[] buffer = new char[1024];

            while(!stopFlag)
            {
                try
                {
                    int readed = stream.Read(buffer);
                    if(readed > 0)
                    {
                        textBox.Dispatcher.Invoke(
                            () =>
                            {
                                textBox.AppendText(new string(buffer, 0, readed));
                                textBox.CaretIndex += readed;

                            });
                    }
                }
                catch(Exception e)
                {
                    // nothing to do
                }

            }
        }

        private void StartProcess()
        {
            Debug.Assert(story != null);

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

            process.Start();

            processOutputReader = new Thread(
                () => {
                    PumpStream(process.StandardOutput, ref processOutputReaderStop);
                });

            processErrorReader = new Thread(
                () => {
                    PumpStream(process.StandardError, ref processErrorReaderStop);
                });

            processOutputReader.Start();
            processErrorReader.Start();

            process.Exited += OnProcessExited;

            process.EnableRaisingEvents = true;

            Console.WriteLine("Console started");

        }

        char[] readBuffer = new char[1024];
        Task<int>? readTask;

        private void OnProcessTimer(object? sender, EventArgs e)
        {
            Debug.Assert(process != null);

            if (readTask != null)
            {
                if (readTask.Status == TaskStatus.RanToCompletion)
                {
                    int readed = readTask.Result;

                    if (readed > 0)
                    {
                        textBox.AppendText(new string(readBuffer, 0, readed));
                        textBox.CaretIndex++;
                    }

                    readTask = process.StandardOutput.ReadAsync(readBuffer, 0, 1024);

                }
            }
            else
            {
                readTask = process.StandardOutput.ReadAsync(readBuffer, 0, 1024);
            }


        }

        private void OnProcessExited(object? sender, EventArgs e)
        {
            StopProcess();

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

        MainWindow window;

        public CommandConsole(MainWindow _window)
        {
            _window.FileEditorTextBox.FontSize = _window.TextSize;
            _window.OnTextSizeChanged += OnTextSizeChanged;


            DataObject.AddPastingHandler(_window.CommandConsoleTextBox, OnFileContentsPaste);
            DataObject.AddCopyingHandler(_window.CommandConsoleTextBox, OnFileContentsCopy);

            _window.CommandConsoleTextBox.ContextMenu = null;

            _window.CommandConsoleTextBox.PreviewKeyDown += OnKeyDown;
            _window.CommandConsoleTextBox.PreviewTextInput += OnTextInput;

            _window.CommandConsoleTextBox.IsReadOnly = true;

            textBox = _window.CommandConsoleTextBox;
            label = _window.CommandConsoleTitleLabel;

            command = new();

            keyConverter = new();

            textBox.FontSize = _window.TextSize;
            _window.OnTextSizeChanged += OnTextSizeChanged;

            window = _window;
        }

        private void OnTextSizeChanged(int size)
        {
            textBox.FontSize = size;
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

                textBox.CaretIndex -= commandText.Length;

                int previousCaret = textBox.CaretIndex;

                textBox.Text = textBox.Text.Substring(0, textBox.CaretIndex);

                textBox.CaretIndex = previousCaret;

                //textBox.AppendText(Environment.NewLine);
                //textBox.CaretIndex += Environment.NewLine.Length;


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
