using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;

namespace AIHelper
{
	public class ConfigOptionsPage : UIElementDialogPage
	{
		private readonly TextBox _apiKeyTextBox;
		private readonly TextBox _aiModelTextBox;
		private readonly StackPanel _rootPanel;

		public ConfigOptionsPage()
		{
			_rootPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(10) };

			_rootPanel.Children.Add(new TextBlock { Text = "Api Key", Margin = new Thickness(0, 0, 0, 4) });
			_apiKeyTextBox = new TextBox { HorizontalAlignment = HorizontalAlignment.Stretch, MinHeight = 30 };
			_rootPanel.Children.Add(_apiKeyTextBox);

			_rootPanel.Children.Add(new TextBlock { Text = "AI Model", Margin = new Thickness(0, 12, 0, 4) });
			_aiModelTextBox = new TextBox { HorizontalAlignment = HorizontalAlignment.Stretch, MinHeight = 30 };
			_rootPanel.Children.Add(_aiModelTextBox);
		}

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);

            try
            {
#if DEBUG
                string baseDir = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
#else
				string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				string baseDir = Path.Combine(assemblyPath, "Resources");
#endif
                if(!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);

                string jsonPath = Path.Combine(baseDir, "appsettings.json");

                JsonObject root = new JsonObject
                {
                    ["ApiSettings"] = new JsonObject
                    {
                        ["ApiKey"] = _apiKeyTextBox.Text ?? string.Empty,
                        ["AiModel"] = _aiModelTextBox.Text ?? string.Empty
                    }
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                string output = root.ToJsonString(options);
                File.WriteAllText(jsonPath, output);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save Error");
            }
        }

        protected override void OnActivate(CancelEventArgs e)
		{
			base.OnActivate(e);

			try
			{
#if DEBUG
				string baseDir = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
#else
				string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				string baseDir = Path.Combine(assemblyPath, "Resources");
#endif
				string jsonPath = Path.Combine(baseDir, "appsettings.json");

				if (File.Exists(jsonPath))
				{
					string json = File.ReadAllText(jsonPath);
					JsonNode root = JsonNode.Parse(json);
					_apiKeyTextBox.Text = root?["ApiSettings"]?["ApiKey"]?.GetValue<string>() ?? string.Empty;
					_aiModelTextBox.Text = root?["ApiSettings"]?["AiModel"]?.GetValue<string>() ?? string.Empty;
				}
				else
				{
					_apiKeyTextBox.Text = string.Empty;
					_aiModelTextBox.Text = string.Empty;
				}
			}
			catch (Exception ex)
			{
				_apiKeyTextBox.Text = string.Empty;
				_aiModelTextBox.Text = string.Empty;
                MessageBox.Show(ex.Message, "Cancel Error");
            }
		}

        protected override UIElement Child
        {
            get { return _rootPanel; }
        }
    }
}


