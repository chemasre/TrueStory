using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace TrueStory
{
    public class Themes
    {
        public bool DarkMode
        {
            set
            {
                SetDarkMode(value);

                window.Settings.SetInt("DarkMode", value ? 1 : 0);
            }
        }

        List<string> darkThemeResources = new()
        {
            "DarkMode_TrueEditorButtonNeutral",
            "DarkMode_TrueEditorButtonAccept",
            "DarkMode_TrueEditorButtonCancel",
            "DarkMode_TrueEditorBackground",
            "DarkMode_TrueEditorEditableTextBackground",
            "DarkMode_TrueEditorNonEditableTextBackground",
            "DarkMode_TrueStoryMenuText",
            "DarkMode_TrueEditorPanelTitle",
            "DarkMode_TrueEditorMenu",
            "DarkMode_TrueStoryPanelTitleText",
            "DarkMode_TrueStoryInfobarText",
            "DarkMode_TrueStoryNonEditableText",
            "DarkMode_TrueStoryButtonsText",
            "DarkMode_TrueStoryTabItem",
            "DarkMode_TrueStoryTabItemText",
            "DarkMode_TrueStoryTabControlBackground",
            "DarkMode_TrueStoryEditableText"
        };

        List<string> lightThemeResources = new()
        {
            "LightMode_TrueEditorButtonNeutral",
            "LightMode_TrueEditorButtonAccept",
            "LightMode_TrueEditorButtonCancel",
            "LightMode_TrueEditorBackground",
            "LightMode_TrueEditorEditableTextBackground",
            "LightMode_TrueEditorNonEditableTextBackground",
            "LightMode_TrueStoryMenuText",
            "LightMode_TrueEditorPanelTitle",
            "LightMode_TrueEditorMenu",
            "LightMode_TrueStoryPanelTitleText",
            "LightMode_TrueStoryInfobarText",
            "LightMode_TrueStoryNonEditableText",
            "LightMode_TrueStoryButtonsText",
            "LightMode_TrueStoryTabItem",
            "LightMode_TrueStoryTabItemText",
            "LightMode_TrueStoryTabControlBackground",
            "LightMode_TrueStoryEditableText"
        };

        List<string> currentThemeResources = new()
        {
            "TrueEditorButtonNeutral",
            "TrueEditorButtonAccept",
            "TrueEditorButtonCancel",
            "TrueEditorBackground",
            "TrueEditorEditableTextBackground",
            "TrueEditorNonEditableTextBackground",
            "TrueStoryMenuText",
            "TrueEditorPanelTitle",
            "TrueEditorMenu",
            "TrueStoryPanelTitleText",
            "TrueStoryInfobarText",
            "TrueStoryNonEditableText",
            "TrueStoryButtonsText",
            "TrueStoryTabItem",
            "TrueStoryTabItemText",
            "TrueStoryTabControlBackground",
            "TrueStoryEditableText"
        };

        Dictionary<string, Brush> darkThemeBrushes = new();
        Dictionary<string, Brush> lightThemeBrushes = new();

        MainWindow window;

        public Themes(MainWindow _window)
        {
            foreach (string r in darkThemeResources)
            {
                darkThemeBrushes.Add(r, ((Brush)App.Current.FindResource(r)).Clone());
            }

            foreach (string r in lightThemeResources)
            {
                lightThemeBrushes.Add(r, ((Brush)App.Current.FindResource(r)).Clone());
            }

            SetDarkMode(_window.Settings.GetInt("DarkMode") != 0);

            window = _window;

        }

        private void SetDarkMode(bool value)
        {
            for (int i = 0; i < currentThemeResources.Count; i++)
            {
                App.Current.Resources[currentThemeResources[i]] = value ? darkThemeBrushes[darkThemeResources[i]] : lightThemeBrushes[lightThemeResources[i]];
            }
        }
    }
}
