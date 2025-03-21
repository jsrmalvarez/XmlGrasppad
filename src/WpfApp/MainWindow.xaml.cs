using LovettSoftware.Utilities;
using ModernWpf;
using ModernWpf.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using XmlNotepad.Utilities;

namespace XmlNotepad
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IServiceProvider
    {
        UndoManager undoManager = new UndoManager(1000);
        Settings settings;
        Updater updater;
        DelayedActions delayedActions;
        XmlCache model;
        XmlIntellisenseProvider xip;
        HelpProvider helpProvider = new HelpProvider();
        RecentFiles recentFiles = new RecentFiles();
        RecentFilesComboBox recentFilesCombo;
        bool initialized;
        ResourceManager resourceManager = new ResourceManager("XmlNotepad.Resources", typeof(MainWindow).Assembly);

        public MainWindow()
        {
            this.Visibility = Visibility.Hidden;
            this.settings = Settings.Instance;

            this.settings["SchemaCache"] = new SchemaCache(this);
            this.settings.StartupPath = System.IO.Path.GetDirectoryName(Application.Current.StartupUri.LocalPath);
            this.settings.ExecutablePath = Application.Current.StartupUri.LocalPath;

            delayedActions = new DelayedActions((action) =>
            {
                this.Dispatcher.Invoke(action);
            });

            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
            this.SizeChanged += OnWindowSizeChanged;
            this.LocationChanged += OnWindowLocationChanged;

            this.recentFiles.RecentFileSelected += OnRecentFileSelected;
            this.recentFiles.RecentFilesChanged += OnRecentFilesChanged;
            this.recentFilesCombo = new RecentFilesComboBox(this.recentFiles, this.ComboBoxAddress);

            this.RestoreSettings();
            this.initialized = true;

            ApplyLocalization();
        }
        
        // Handle changes to the recent files list
        private void OnRecentFilesChanged(object sender, EventArgs e)
        {
            // Update the Recent Files menu with the new list
            UpdateRecentFilesMenu();

            // Save the list to settings
            this.settings["RecentFiles"] = this.recentFiles.GetFiles();
        }
        
        // Update the Recent Files menu with the current list of files
        private void UpdateRecentFilesMenu()
        {
            RecentFilesMenu.Items.Clear();
            
            Uri[] files = this.recentFiles.GetFiles();
            if (files != null && files.Length > 0)
            {
                foreach (Uri uri in files)
                {
                    System.Windows.Controls.MenuItem item = new System.Windows.Controls.MenuItem();
                    item.Header = System.IO.Path.GetFileName(uri.LocalPath);
                    item.ToolTip = uri.LocalPath;
                    item.Click += (s, e) => LoadFile(uri.LocalPath);
                    RecentFilesMenu.Items.Add(item);
                }
            }
            else
            {
                // Add a disabled item if there are no recent files
                System.Windows.Controls.MenuItem item = new System.Windows.Controls.MenuItem();
                item.Header = "No recent files";
                item.IsEnabled = false;
                RecentFilesMenu.Items.Add(item);
            }
        }

        private void RestoreSettings()
        {
            object value = this.settings["WindowBounds"];
            if (value is Rect r && !r.IsEmpty)
            {
                // make sure it is visible on the user's current screen configuration.
                var bounds = new Rect(
                    XamlExtensions.ConvertFromDeviceIndependentPixels(r.X),
                    XamlExtensions.ConvertFromDeviceIndependentPixels(r.Y),
                    XamlExtensions.ConvertFromDeviceIndependentPixels(r.Width),
                    XamlExtensions.ConvertFromDeviceIndependentPixels(r.Height));
                var virtualScreen = new Rect(SystemParameters.VirtualScreenLeft,
                    SystemParameters.VirtualScreenTop,
                    SystemParameters.VirtualScreenWidth,
                    SystemParameters.VirtualScreenHeight);
                if (virtualScreen.Contains(bounds))
                {
                    this.Left = XamlExtensions.ConvertToDeviceIndependentPixels((int)bounds.X);
                    this.Top = XamlExtensions.ConvertToDeviceIndependentPixels((int)bounds.Y);
                    this.Width = XamlExtensions.ConvertToDeviceIndependentPixels((int)bounds.Width);
                    this.Height = XamlExtensions.ConvertToDeviceIndependentPixels((int)bounds.Height);
                }
            }

            UpdateTheme();

            this.recentFiles.SetFiles(this.settings["RecentFiles"] as Uri[]);

            this.Visibility = Visibility.Visible;
        }

        private void UpdateTheme()
        {
            if (this.settings["Theme"] is ColorTheme theme)
            {
                switch (theme)
                {
                    case ColorTheme.Light:
                        ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                        break;
                    case ColorTheme.Dark:
                        ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                        break;
                }
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.updater = new Updater(this.settings, this.delayedActions);
            this.updater.Title = this.Title;
            this.updater.UpdateAvailable += OnUpdateAvailable;
        }

        private void OnWindowLocationChanged(object sender, EventArgs e)
        {
            if (this.initialized)
            {
                this.settings["WindowBounds"] = this.RestoreBounds;
            }
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.initialized)
            {
                this.settings["WindowBounds"] = this.RestoreBounds;
            }
        }

        private void OnUpdateAvailable(object sender, UpdateStatus e)
        {
            // show UI
            Debug.WriteLine("New version available: " + e.Latest.ToString());
        }

        private void OnDarkTheme(object sender, RoutedEventArgs e)
        {
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            this.settings["Theme"] = ColorTheme.Dark;
        }

        private void OnLightTheme(object sender, RoutedEventArgs e)
        {
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
            this.settings["Theme"] = ColorTheme.Light;

        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
        }

        private void OnExit(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        private void OnNewWindow(object sender, RoutedEventArgs e)
        {
            // hmmm, this is problematic because the new window needs a new XmlCache, and SchemaCache and
            // so on, so better to start a new process here.
            ProcessStartInfo info = new ProcessStartInfo()
            {
                FileName = this.GetType().Assembly.Location,
                Arguments = string.Join(" ", Environment.GetCommandLineArgs()),
                WorkingDirectory = Directory.GetCurrentDirectory()
            };

            Process.Start(info);
        }

        public object GetService(Type service)
        {
            if (service == typeof(UndoManager))
            {
                return this.undoManager;
            }
            else if (service == typeof(SchemaCache))
            {
                return this.settings["SchemaCache"];
            }
            //else if (service == typeof(TreeView))
            //{
            //    XmlTreeView view = (XmlTreeView)GetService(typeof(XmlTreeView));
            //    return view.TreeView;
            //}
            //else if (service == typeof(XmlTreeView))
            //{
            //    if (this.xmlTreeView1 == null)
            //    {
            //        this.xmlTreeView1 = this.CreateTreeView();
            //    }
            //    return this.xmlTreeView1;
            //}
            else if (service == typeof(XmlCache))
            {
                if (null == this.model)
                {
                    this.model = new XmlCache((IServiceProvider)this, (SchemaCache)this.settings["SchemaCache"], this.delayedActions);
                }
                return this.model;
            }
            else if (service == typeof(Settings))
            {
                return this.settings;
            }
            else if (service == typeof(IIntellisenseProvider))
            {
                if (this.xip == null) this.xip = new XmlIntellisenseProvider(this.model);
                return this.xip;
            }
            else if (service == typeof(HelpProvider))
            {
                return this.helpProvider;
            }
            //else if (service == typeof(WebProxyService))
            //{
            //    if (this._proxyService == null)
            //        this._proxyService = new WebProxyService((IServiceProvider)this);
            //    return this._proxyService;
            //}
            else if (service == typeof(DelayedActions))
            {
                return this.delayedActions;
            }
            return null;
        }

        private void OnComboBoxAddress_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                var path = this.ComboBoxAddress.Text;
                if (!string.IsNullOrEmpty(path))
                {
                    try
                    {
                        LoadFile(path);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error Parsing Filename", MessageBoxButton.OK, MessageBoxImage.Error);
                        ComboBoxAddress.Focus();
                    }
                }
            }
        }

        private void LoadFile(string path)
        {
            try
            {
                var model = (XmlCache)GetService(typeof(XmlCache));
                model.Load(path);
                
                // Update the UI with the loaded XML content
                if (model.Document != null)
                {
                    // Display XML in the tree view
                    PopulateTreeView(model.Document);
                    
                    // Show raw XML in the text view
                    using (StringWriter sw = new StringWriter())
                    {
                        model.Document.Save(sw);
                        XmlContentView.Text = sw.ToString();
                    }
                    
                    // Add to recent files
                    this.recentFiles.Add(path);
                    
                    // Update window title
                    this.Title = $"XML Grasppad - {Path.GetFileName(path)}";
                    
                    // Show status message
                    StatusMessage.Content = $"Loaded: {path}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading XML file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void PopulateTreeView(System.Xml.XmlDocument document)
        {
            XmlTreeView.Items.Clear();
            
            // Create root item
            TreeViewItem rootItem = CreateTreeViewItem(document.DocumentElement);
            if (rootItem != null)
            {
                XmlTreeView.Items.Add(rootItem);
                rootItem.IsExpanded = true;
            }
        }
        
        private TreeViewItem CreateTreeViewItem(System.Xml.XmlNode node)
        {
            if (node == null) return null;
            
            TreeViewItem item = new TreeViewItem();
            item.Tag = node;
            
            // Configure the item based on node type
            switch (node.NodeType)
            {
                case System.Xml.XmlNodeType.Element:
                    // Element with no attributes
                    if (node.Attributes.Count == 0 && !node.HasChildNodes)
                    {
                        item.Header = $"<{node.Name}/>";
                    }
                    // Element with attributes or children
                    else
                    {
                        item.Header = $"<{node.Name}>";
                        
                        // Add attributes as children
                        foreach (System.Xml.XmlAttribute attr in node.Attributes)
                        {
                            TreeViewItem attrItem = new TreeViewItem();
                            attrItem.Header = $"{attr.Name}=\"{attr.Value}\"";
                            attrItem.Tag = attr;
                            item.Items.Add(attrItem);
                        }
                        
                        // Add child nodes
                        foreach (System.Xml.XmlNode childNode in node.ChildNodes)
                        {
                            TreeViewItem childItem = CreateTreeViewItem(childNode);
                            if (childItem != null)
                            {
                                item.Items.Add(childItem);
                            }
                        }
                    }
                    break;
                    
                case System.Xml.XmlNodeType.Text:
                    item.Header = node.Value;
                    break;
                    
                case System.Xml.XmlNodeType.CDATA:
                    item.Header = $"<![CDATA[{node.Value}]]>";
                    break;
                    
                case System.Xml.XmlNodeType.Comment:
                    item.Header = $"<!--{node.Value}-->";
                    break;
                    
                case System.Xml.XmlNodeType.ProcessingInstruction:
                    item.Header = $"<?{node.Name} {node.Value}?>";
                    break;
                    
                default:
                    item.Header = node.OuterXml;
                    break;
            }
            
            return item;
        }

        private void OnRecentFileSelected(object sender, MostRecentlyUsedEventArgs args)
        {
            if (args.Selection != null)
            {
                this.LoadFile(args.Selection);
            }
        }

        private void ApplyLocalization()
        {
            // Update UI elements with localized strings
            this.Title = resourceManager.GetString("AppTitle");
            MenuFile.Header = resourceManager.GetString("MenuFile");
            MenuExit.Header = resourceManager.GetString("MenuExit");
            // ...update other UI elements...
        }

        private void OnLanguageChanged(object sender, RoutedEventArgs e)
        {
            var selectedLanguage = ((MenuItem)sender).Tag.ToString();
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(selectedLanguage);
            ApplyLocalization();
        }
    }
}
