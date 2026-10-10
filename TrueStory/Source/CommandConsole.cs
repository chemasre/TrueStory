using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TrueStory.Source
{
    public class CommandConsole
    {
        public event OnExecuteCommandHandler? OnExecuteCommand;
        public event OnCommandExecutedHandler? OnCommandExecuted;

        public delegate string OnExecuteCommandHandler(string command);
        public delegate void OnCommandExecutedHandler(string command, string output);

        TextBox textBox;

        StringBuilder command;

        KeyConverter keyConverter;

        public CommandConsole(TextBox _textBox)
        {
            DataObject.AddPastingHandler(_textBox, OnFileContentsPaste);
            DataObject.AddCopyingHandler(_textBox, OnFileContentsCopy);

            _textBox.ContextMenu = null;

            _textBox.PreviewKeyDown += OnKeyDown;
            _textBox.PreviewTextInput += OnTextInput;

            _textBox.IsReadOnly = true;

            _textBox.Text = Constants.consolePrompt;
            _textBox.CaretIndex = Constants.consolePrompt.Length;

            textBox = _textBox;

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
            if (e.Key == Key.Return)
            {
                textBox.AppendText(Environment.NewLine);
                textBox.CaretIndex += Environment.NewLine.Length;

                string output = OnExecuteCommand?.Invoke(command.ToString()) ?? "";
                textBox.AppendText(output);
                textBox.CaretIndex += output.Length;

                textBox.AppendText(Environment.NewLine);
                textBox.CaretIndex += Environment.NewLine.Length;

                textBox.AppendText(Constants.consolePrompt);
                textBox.CaretIndex += Constants.consolePrompt.Length;

                string executed = command.ToString();

                command.Clear();

                OnCommandExecuted?.Invoke(executed, output);


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
