using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static System.Net.Mime.MediaTypeNames;

namespace TrueStory
{
    public class FileEditor
    {
        public event OnContentsChangedHandler? OnContentsChanged;

        public TextBox TextBox { get { return textBox;  } }
        public bool IsFileLoaded { get { return loaded; } }

        public struct ContentChange
        {
            public int Offset;
            public int Added;
            public string AddedText;
            public int Removed;
            public long DeltaTime;
        }

        public delegate void OnContentsChangedHandler(ContentChange c);

        MainWindow window;
        TextBox textBox;

        Label infoBar;

        bool ignoreChanges;

        long lastChangeTime;

        bool loaded;

        public FileEditor(MainWindow _window)
        {
            DataObject.AddPastingHandler(_window.FileEditorTextBox, OnFileContentsPaste);
            DataObject.AddCopyingHandler(_window.FileEditorTextBox, OnFileContentsCopy);

            _window.FileEditorTextBox.ContextMenu = null;

            _window.FileEditorTextBox.TextChanged += OnTextBoxTextChanged;
            _window.FileEditorTextBox.PreviewKeyDown += OnKeyDown;
            _window.FileEditorTextBox.SelectionChanged += OnSelectionChanged;

            textBox = _window.FileEditorTextBox;
            infoBar = _window.FileEditorInfoBarLabel;

            _window.CutMenuAction.Click += OnMenuCut;
            _window.CopyMenuAction.Click += OnMenuCopy;
            _window.PasteMenuAction.Click += OnMenuPaste;
            _window.GotoLineMenuAction.Click += OnMenuGotoLine;

            window = _window;

            loaded = false;

            UpdateInfoBar();


            UpdateEditMenu();
        }

        private void OnMenuGotoLine(object sender, RoutedEventArgs e)
        {
            DialogGotoLine d = new();
            d.Message.Text = String.Format("Ir a línea ({0:0} - {1:0})", 1, textBox.LineCount);
            d.MaxLines = textBox.LineCount;
            d.Input.Text = "1";

            bool? result = d.ShowDialog();

            if (result.GetValueOrDefault())
            {
                textBox.CaretIndex = textBox.GetCharacterIndexFromLineIndex(d.Line);
                textBox.ScrollToLine(d.Line);
            }
        }

        void OnMenuPaste(object sender, RoutedEventArgs e)
        {
            DoPaste();
        }

        void OnMenuCopy(object sender, RoutedEventArgs e)
        {
            DoCopyOrCut(false);
        }

        void OnMenuCut(object sender, RoutedEventArgs e)
        {
            DoCopyOrCut(true);
        }

        public void Load(string text)
        {
            ignoreChanges = true;
            textBox.Text = text;
            ignoreChanges = false;

            lastChangeTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            loaded = true;

            UpdateInfoBar();
            UpdateEditMenu();
        }

        public void Unload()
        {
            ignoreChanges = true;
            textBox.Text = "";
            ignoreChanges = false;

            loaded = false;

            UpdateInfoBar();
            UpdateEditMenu();
        }

        void UpdateInfoBar()
        {
            if (loaded)
            {
                int line = textBox.GetLineIndexFromCharacterIndex(textBox.CaretIndex);
                int count = textBox.LineCount;
                int column = textBox.CaretIndex - textBox.GetCharacterIndexFromLineIndex(line);
                infoBar.Content = String.Format("Línea {0:0} Columna {1:0} Total líneas {2:0}", line + 1, column + 1, count);
            }
            else
            {
                infoBar.Content = "Barra de información";
            }
        }

        void DoCopyOrCut(bool isCut)
        {
            SharedClipboard.Content = textBox.SelectedText;
            if (isCut) { if (textBox.SelectionLength > 0) { textBox.SelectedText = ""; }  }
        }

        void DoPaste()
        {
            if (SharedClipboard.Content != null)
            {
                if (textBox.SelectionLength > 0)
                {
                    textBox.SelectedText = SharedClipboard.Content;
                }
                else
                {
                    int p = textBox.CaretIndex;
                    textBox.Text = textBox.Text.Insert(textBox.CaretIndex, SharedClipboard.Content);
                    textBox.CaretIndex = p + SharedClipboard.Content.Length;
                }
            }
        }

        void OnFileContentsCopy(object sender, DataObjectCopyingEventArgs e)
        {
            bool isText = e.DataObject.GetDataPresent(DataFormats.UnicodeText, true);

            if (isText)
            {
                string text = (string)e.DataObject.GetData(DataFormats.UnicodeText);

                Utils.Log("Detected copy: " + text);

                DoCopyOrCut(Keyboard.IsKeyDown(Key.X));

                UpdateEditMenu();

            }
            else
            {
                Utils.Log("Unrecognized copy");
            }

            e.CancelCommand();
            Utils.Log("Copy cancelled");

        }

        void OnFileContentsPaste(object sender, DataObjectPastingEventArgs e)
        {
            bool isText = e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, true);

            if (isText)
            {
                string text = (string)e.SourceDataObject.GetData(DataFormats.UnicodeText);
                e.FormatToApply = DataFormats.Text;

                Utils.Log("Detected paste: " + text);


                DoPaste();

            }
            else
            {
                Utils.Log("Unrecognized paste");
            }

            e.CancelCommand();
            Utils.Log("Paste cancelled");
        }

        void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (ignoreChanges) { return;  }

            //Utils.Log("----------------------------------------------------");

            foreach (TextChange c in e.Changes)
            {
                // Utils.Log("Text changed at " + c.Offset + ": added length " + c.AddedLength + ", removed length " + c.RemovedLength);
            }

            Debug.Assert(e.Changes.Count == 1);

            if (e.Changes.Count == 1)
            {
                TextChange c = e.Changes.First();
                string text = "";
                if (c.AddedLength > 0)
                {
                    text = textBox.Text.Substring(c.Offset, c.AddedLength);
                }

                ContentChange cc = new();
                cc.Offset = c.Offset;
                cc.Added = c.AddedLength;
                cc.AddedText = text;
                cc.Removed = c.RemovedLength;

                long time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                cc.DeltaTime = time - lastChangeTime;

                lastChangeTime = time;

                Utils.Log("Change Offset:" + cc.Offset + " Added: " + cc.Added + " Removed: " + cc.Removed + " Text: " + cc.AddedText);

                OnContentsChanged?.Invoke(cc);
            }


        }

        void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                int p = textBox.CaretIndex;

                ignoreChanges = true;
                textBox.Text = textBox.Text.Insert(p, Constants.tabAsSpaces);
                textBox.CaretIndex = p + Constants.tabAsSpaces.Length;
                ignoreChanges = false;

                ContentChange c = new();
                c.Offset = p;
                c.Added = Constants.tabAsSpaces.Length;
                c.AddedText = Constants.tabAsSpaces;
                c.Removed = 0;
                long time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                c.DeltaTime = time - lastChangeTime;

                lastChangeTime = time;

                Utils.Log("Change Offset:" + c.Offset + " Added: " + c.Added + " Removed: " + c.Removed + " Text: " + c.AddedText);


                OnContentsChanged?.Invoke(c);

                e.Handled = true;
            }
            if (e.Key == Key.Return)
            {
                // Utils.Log("Detected return");

                int lineIndex = textBox.GetLineIndexFromCharacterIndex(textBox.CaretIndex);
                string lineText = textBox.GetLineText(lineIndex);

                int lastTabEquivalent = FindLastTabEquivalent(lineText);

                string? tabPrefix = null;
                if (lastTabEquivalent >= 0) { tabPrefix = lineText.Substring(0, lastTabEquivalent); }

                int p = textBox.CaretIndex;
                string newLineText = Environment.NewLine + (tabPrefix ?? "");

                ignoreChanges = true;
                textBox.Text = textBox.Text.Insert(p, newLineText);
                textBox.CaretIndex = p + newLineText.Length;
                ignoreChanges = false;

                ContentChange c = new();
                c.Offset = p;
                c.Added = newLineText.Length;
                c.AddedText = newLineText;
                c.Removed = 0;
                long time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                c.DeltaTime = time - lastChangeTime;

                lastChangeTime = time;

                Utils.Log("Change Offset:" + c.Offset + " Added: " + c.Added + " Removed: " + c.Removed + " Text: " + c.AddedText);

                OnContentsChanged?.Invoke(c);

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
            UpdateEditMenu();
        }

        void UpdateEditMenu()
        {
            if (loaded)
            {
                window.EditMenu.IsEnabled = true;
                window.CutMenuAction.IsEnabled = (textBox.SelectionLength > 0);
                window.CopyMenuAction.IsEnabled = (textBox.SelectionLength > 0);
                window.PasteMenuAction.IsEnabled = (SharedClipboard.Content != null);
                window.GotoLineMenuAction.IsEnabled = true;
            }
            else
            {
                window.EditMenu.IsEnabled = false;
            }
        }
    }
}
