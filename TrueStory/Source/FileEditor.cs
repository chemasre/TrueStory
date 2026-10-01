using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TrueStory
{
    public class FileEditor
    {
        public event OnContentsChangedHandler? OnContentsChanged;

        public struct ContentChange
        {
            public int Offset;
            public int Added;
            public string AddedText;
            public int Removed;
        }

        public delegate void OnContentsChangedHandler(ContentChange c);

        TextBox textBox;

        Label infoBar;

        bool ignoreChanges;

        public FileEditor(TextBox _textBox, Label _infoBar)
        {
            DataObject.AddPastingHandler(_textBox, OnFileContentsPaste);
            DataObject.AddCopyingHandler(_textBox, OnFileContentsCopy);

            _textBox.ContextMenu = null;

            _textBox.TextChanged += OnTextBoxTextChanged;
            _textBox.PreviewKeyDown += OnKeyDown;
            _textBox.SelectionChanged += OnSelectionChanged;

            textBox = _textBox;

            infoBar = _infoBar;

            ClearInfoBar();
        }

        public void Load(string text)
        {
            ignoreChanges = true;
            textBox.Text = text;
            ignoreChanges = false;

            UpdateInfoBar();
        }

        public void Clear()
        {
            ignoreChanges = true;
            textBox.Text = "";
            ignoreChanges = false;

            ClearInfoBar();
        }

        void ClearInfoBar()
        {
            infoBar.Content = "No file loaded";
        }

        void UpdateInfoBar()
        {
            int line = textBox.GetLineIndexFromCharacterIndex(textBox.CaretIndex);
            int count = textBox.LineCount;
            int column = textBox.CaretIndex - textBox.GetCharacterIndexFromLineIndex(line);
            infoBar.Content = String.Format("Line {0:0} Column {1:0} Total lines {2:0}", line, column, count);
        }

        void OnFileContentsCopy(object sender, DataObjectCopyingEventArgs e)
        {
            bool isText = e.DataObject.GetDataPresent(DataFormats.UnicodeText, true);

            if (isText)
            {
                string text = (string)e.DataObject.GetData(DataFormats.UnicodeText);

                Console.WriteLine("Detected copy: " + text);

                PrivateClipboard.content = text;

                if (Keyboard.IsKeyDown(Key.X))
                {
                    if (textBox.SelectionLength > 0) { textBox.SelectedText = ""; }
                }

            }
            else
            {
                Console.WriteLine("Unrecognized copy");
            }

            e.CancelCommand();
            Console.WriteLine("Copy cancelled");

        }

        void OnFileContentsPaste(object sender, DataObjectPastingEventArgs e)
        {
            bool isText = e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, true);

            if (isText)
            {
                string text = (string)e.SourceDataObject.GetData(DataFormats.UnicodeText);
                e.FormatToApply = DataFormats.Text;

                Console.WriteLine("Detected paste: " + text);


                if (PrivateClipboard.content != null)
                {
                    if (textBox.SelectionLength > 0)
                    {
                        textBox.SelectedText = text;
                    }
                    else
                    {
                        int p = textBox.CaretIndex;
                        textBox.Text = textBox.Text.Insert(textBox.CaretIndex, PrivateClipboard.content);
                        textBox.CaretIndex = p + PrivateClipboard.content.Length;
                    }
                }



            }
            else
            {
                Console.WriteLine("Unrecognized paste");
            }

            e.CancelCommand();
            Console.WriteLine("Paste cancelled");
        }

        void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (ignoreChanges) { return;  }

            foreach (TextChange c in e.Changes)
            {
                Console.WriteLine("Text changed at " + c.Offset + ": added length " + c.AddedLength + ", removed length " + c.RemovedLength);
            }

            Debug.Assert(e.Changes.Count == 1);

            if (e.Changes.Count == 1)
            {
                TextChange c = e.Changes.First();
                string text = "";
                if (c.AddedLength > 0 && c.RemovedLength == 0)
                {
                    text = textBox.Text.Substring(c.Offset, c.AddedLength);
                    Console.WriteLine("Added text " + text);

                    
                }

                ContentChange cc = new();
                cc.Offset = c.Offset;
                cc.Added = c.AddedLength;
                cc.AddedText = text;
                cc.Removed = c.RemovedLength;

                OnContentsChanged?.Invoke(cc);
            }


        }

        void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                int p = textBox.CaretIndex;
                textBox.Text = textBox.Text.Insert(p, Constants.tabAsSpaces);
                textBox.CaretIndex = p + Constants.tabAsSpaces.Length;

                e.Handled = true;
            }
            if (e.Key == Key.Return)
            {
                Console.WriteLine("Detected return");

                int lineIndex = textBox.GetLineIndexFromCharacterIndex(textBox.CaretIndex);
                string lineText = textBox.GetLineText(lineIndex);

                int lastTabEquivalent = FindLastTabEquivalent(lineText);

                string? tabPrefix = null;
                if (lastTabEquivalent >= 0) { tabPrefix = lineText.Substring(0, lastTabEquivalent); }

                int p = textBox.CaretIndex;
                string newLineText = Environment.NewLine + (tabPrefix ?? "");
                textBox.Text = textBox.Text.Insert(p, newLineText);
                textBox.CaretIndex = p + newLineText.Length;

                e.Handled = true;
            }
        }

        int FindLastTabEquivalent(string lineText)
        {
            int i = 0;
            bool found = false;
            int lastTabEquivalent = -1;

            while (i < lineText.Length && !found)
            {
                int j = 0;
                bool isTabEquivalent = false;
                while (j < Constants.tabEquivalents.Length && !isTabEquivalent)
                {
                    if (lineText[i] == Constants.tabEquivalents[j]) { isTabEquivalent = true; }
                    else { j++; }
                }

                if (!isTabEquivalent) { found = true; lastTabEquivalent = i; }
                else
                {
                    i++;
                }
            }

            return lastTabEquivalent;
        }

        void OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateInfoBar();
        }
    }
}
