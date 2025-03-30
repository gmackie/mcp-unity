using UnityEngine;
using UnityEditor;

namespace McpUnity.Unity
{
    /// <summary>
    /// Editor window for controlling the MCP Unity Server.
    /// Provides UI for starting/stopping the server and configuring settings.
    /// </summary>
    public class McpUnityEditorWindow : EditorWindow
    {
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _wrappedLabelStyle;
        private int _selectedTab = 0;
        private string[] _tabNames = new string[] { "Server", "Help" };
        private bool _isInitialized = false;
        private string _mcpConfigJson = "";
        private bool _tabsIndentationJson = false;
        
        // Foldout states for Help tab
        private Vector2 _helpScrollPosition;
        private bool _toolsFoldout = true;
        private bool _gameObjectToolsFoldout = true;
        private bool _prefabToolsFoldout = true;
        private bool _sceneToolsFoldout = true;
        private bool _otherToolsFoldout = true;
        private bool _resourcesFoldout = true;
        private bool _gameObjectResourcesFoldout = true;
        private bool _prefabResourcesFoldout = true;
        private bool _sceneResourcesFoldout = true;
        private bool _otherResourcesFoldout = true;

        [MenuItem("Tools/MCP Unity/Server Window", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<McpUnityEditorWindow>("MCP Unity");
            window.minSize = new Vector2(600, 400);
        }

        private void OnGUI()
        {
            InitializeStyles();

            EditorGUILayout.BeginVertical();
            
            // Header
            EditorGUILayout.Space();
            WrappedLabel("MCP Unity", _headerStyle);
            EditorGUILayout.Space();
            
            // Tabs
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
            EditorGUILayout.Space();
            
            switch (_selectedTab)
            {
                case 0: // Server tab
                    DrawServerTab();
                    break;
                case 1: // Help tab
                    DrawHelpTab();
                    break;
            }
            
            // Version info at the bottom
            GUILayout.FlexibleSpace();
            WrappedLabel($"MCP Unity v{McpUnitySettings.ServerVersion}", EditorStyles.miniLabel, GUILayout.Width(150));
            
            EditorGUILayout.EndVertical();
        }

        #region Tab Drawing Methods

        private void DrawServerTab()
        {
            EditorGUILayout.BeginVertical("box");
            
            // Server status
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Status:", GUILayout.Width(120));
            
            McpUnitySettings settings = McpUnitySettings.Instance;
            McpUnityServer mcpUnityServer = McpUnityServer.Instance;
            string statusText = mcpUnityServer.IsListening ? "Server Online" : "Server Offline";
            Color statusColor = mcpUnityServer.IsListening  ? Color.green : Color.red;
            
            GUIStyle statusStyle = new GUIStyle(EditorStyles.boldLabel);
            statusStyle.normal.textColor = statusColor;
            
            EditorGUILayout.LabelField(statusText, statusStyle);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Port configuration
            EditorGUILayout.BeginHorizontal();
            int newPort = EditorGUILayout.IntField("Connection Port", settings.Port);
            if (newPort < 1 || newPort > 65536)
            {
                newPort = settings.Port;
                Debug.LogError($"{newPort} is an invalid port number. Please enter a number between 1 and 65535.");
            }
            
            if (newPort != settings.Port)
            {
                settings.Port = newPort;
                settings.SaveSettings();
                mcpUnityServer.StopServer();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Server control buttons
            EditorGUILayout.BeginHorizontal();
            
            // Connect button - enabled only when disconnected
            GUI.enabled = !mcpUnityServer.IsListening;
            if (GUILayout.Button("Start Server", GUILayout.Height(30)))
            {
                mcpUnityServer.StartServer();
            }
            
            // Disconnect button - enabled only when connected
            GUI.enabled = mcpUnityServer.IsListening;
            if (GUILayout.Button("Stop Server", GUILayout.Height(30)))
            {
                mcpUnityServer.StopServer();
            }
            
            Repaint();
            
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Connected Clients", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            
            var clients = mcpUnityServer.GetConnectedClients();
            
            if (clients.Count > 0)
            {
                foreach (var client in clients)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("ID:", GUILayout.Width(50));
                    EditorGUILayout.LabelField(client.ID, EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Name:", GUILayout.Width(50));
                    EditorGUILayout.LabelField(client.Name);
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                }
            }
            else
            {
                EditorGUILayout.LabelField("No clients connected", EditorStyles.label);
            }
                
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            
            // MCP Config generation section
            EditorGUILayout.LabelField("MCP Configuration", EditorStyles.boldLabel);

            var before = _tabsIndentationJson;
            _tabsIndentationJson = EditorGUILayout.Toggle("Use Tabs indentation", _tabsIndentationJson);
            
            if (string.IsNullOrEmpty(_mcpConfigJson) || before != _tabsIndentationJson)
            {
                _mcpConfigJson = McpConfigUtils.GenerateMcpConfigJson(_tabsIndentationJson);
            }
                
            if (GUILayout.Button("Copy to Clipboard", GUILayout.Height(30)))
            {
                EditorGUIUtility.systemCopyBuffer = _mcpConfigJson;
            }
            
            EditorGUILayout.TextArea(_mcpConfigJson, GUILayout.Height(200));

            EditorGUILayout.Space();
            
            if (GUILayout.Button("Configure Windsurf IDE", GUILayout.Height(30)))
            {
                McpConfigUtils.AddToWindsurfIdeConfig(_tabsIndentationJson);
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Configure Cursor IDE", GUILayout.Height(30)))
            {
                McpConfigUtils.AddToCursorIdeConfig();
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Configure Claude Desktop", GUILayout.Height(30)))
            {
                McpConfigUtils.AddToClaudeDesktopConfig(_tabsIndentationJson);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawHelpTab()
        {
            // Begin scroll view
            _helpScrollPosition = EditorGUILayout.BeginScrollView(_helpScrollPosition);
            
            WrappedLabel("About MCP Unity", _subHeaderStyle);
            EditorGUILayout.BeginVertical(_boxStyle);
            WrappedLabel("MCP Unity is a Unity Editor integration of the Model Context Protocol (MCP), which enables standardized communication between AI models and applications.");
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Open MCP Protocol Documentation"))
            {
                Application.OpenURL("https://modelcontextprotocol.io");
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            // Tools Section with foldout
            _toolsFoldout = EditorGUILayout.Foldout(_toolsFoldout, "Available Tools", true, EditorStyles.foldoutHeader);
            if (_toolsFoldout)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                
                // GameObject Management Tools
                _gameObjectToolsFoldout = EditorGUILayout.Foldout(_gameObjectToolsFoldout, "GameObject Management:", true);
                if (_gameObjectToolsFoldout)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    WrappedLabel("select_gameobject", EditorStyles.boldLabel);
                    WrappedLabel("Selects game objects in the Unity hierarchy by path or instance ID");
                    EditorGUILayout.Space();
                    
                    WrappedLabel("update_component", EditorStyles.boldLabel);
                    WrappedLabel("Updates component fields on a GameObject or adds it to the GameObject if it does not contain the component");
                    
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.Space();
                
                // Prefab Management Tools
                _prefabToolsFoldout = EditorGUILayout.Foldout(_prefabToolsFoldout, "Prefab Management:", true);
                if (_prefabToolsFoldout)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    WrappedLabel("create_prefab", EditorStyles.boldLabel);
                    WrappedLabel("Creates prefabs from existing GameObjects");
                    EditorGUILayout.Space();
                    
                    WrappedLabel("instantiate_prefab", EditorStyles.boldLabel);
                    WrappedLabel("Instantiates prefabs into the scene");
                    EditorGUILayout.Space();
                    
                    WrappedLabel("update_prefab", EditorStyles.boldLabel);
                    WrappedLabel("Modifies prefab properties and applies changes");
                    
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.Space();
                
                // Scene Management Tools
                _sceneToolsFoldout = EditorGUILayout.Foldout(_sceneToolsFoldout, "Scene Management:", true);
                if (_sceneToolsFoldout)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    WrappedLabel("create_scene", EditorStyles.boldLabel);
                    WrappedLabel("Creates new scenes from scratch or based on templates");
                    EditorGUILayout.Space();
                    
                    WrappedLabel("load_scene", EditorStyles.boldLabel);
                    WrappedLabel("Loads existing scenes in the editor");
                    EditorGUILayout.Space();
                    
                    WrappedLabel("save_scene", EditorStyles.boldLabel);
                    WrappedLabel("Saves changes to scenes");
                    EditorGUILayout.Space();
                    
                    WrappedLabel("switch_build_scenes", EditorStyles.boldLabel);
                    WrappedLabel("Adds/removes scenes from the build settings and reorders them");
                    
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.Space();
                
                // Other Tools
                _otherToolsFoldout = EditorGUILayout.Foldout(_otherToolsFoldout, "Other Tools:", true);
                if (_otherToolsFoldout)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    WrappedLabel("execute_menu_item", EditorStyles.boldLabel);
                    WrappedLabel("Executes a function that is currently tagged with MenuItem attribute in the project or in the Unity Editor's menu path");
                    EditorGUILayout.Space();
                    
                    WrappedLabel("package_manager", EditorStyles.boldLabel);
                    WrappedLabel("Installs, removes, and updates packages in the Unity Package Manager");
                    EditorGUILayout.Space();
                    
                    WrappedLabel("run_tests", EditorStyles.boldLabel);
                    WrappedLabel("Runs tests using the Unity Test Runner");
                    EditorGUILayout.Space();
                    
                    WrappedLabel("notify_message", EditorStyles.boldLabel);
                    WrappedLabel("Displays messages in the Unity Editor");
                    
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.Space();
            
            // Resources Section with foldout
            _resourcesFoldout = EditorGUILayout.Foldout(_resourcesFoldout, "Available Resources", true, EditorStyles.foldoutHeader);
            if (_resourcesFoldout)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                
                // GameObject Resources
                _gameObjectResourcesFoldout = EditorGUILayout.Foldout(_gameObjectResourcesFoldout, "GameObject Resources:", true);
                if (_gameObjectResourcesFoldout)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    WrappedLabel("get_hierarchy", EditorStyles.boldLabel);
                    WrappedLabel("Retrieves a list of all game objects in the Unity hierarchy");
                    EditorGUILayout.Space();
                    
                    WrappedLabel("get_gameobject", EditorStyles.boldLabel);
                    WrappedLabel("Retrieves detailed information about a specific GameObject by instance ID, including all GameObject components with its serialized properties and fields");
                    
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.Space();
                
                // Prefab Resources
                _prefabResourcesFoldout = EditorGUILayout.Foldout(_prefabResourcesFoldout, "Prefab Resources:", true);
                if (_prefabResourcesFoldout)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    WrappedLabel("get_prefabs", EditorStyles.boldLabel);
                    WrappedLabel("Retrieves a list of all prefabs in the project with optional filtering");
                    
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.Space();
                
                // Scene Resources
                _sceneResourcesFoldout = EditorGUILayout.Foldout(_sceneResourcesFoldout, "Scene Resources:", true);
                if (_sceneResourcesFoldout)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    WrappedLabel("get_scenes", EditorStyles.boldLabel);
                    WrappedLabel("Lists all scenes in the project with optional metadata");
                    EditorGUILayout.Space();
                    
                    WrappedLabel("get_scene_info", EditorStyles.boldLabel);
                    WrappedLabel("Retrieves detailed information about the current scene");
                    
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.Space();
                
                // Other Resources
                _otherResourcesFoldout = EditorGUILayout.Foldout(_otherResourcesFoldout, "Other Resources:", true);
                if (_otherResourcesFoldout)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    WrappedLabel("get_menu_items", EditorStyles.boldLabel);
                    WrappedLabel("Retrieves a list of all available menu items in the Unity Editor");
                    EditorGUILayout.Space();
                    
                    WrappedLabel("get_console_logs", EditorStyles.boldLabel);
                    WrappedLabel("Retrieves a list of all logs from the Unity console");
                    EditorGUILayout.Space();
                    
                    WrappedLabel("get_packages", EditorStyles.boldLabel);
                    WrappedLabel("Retrieves information about installed and available packages from the Unity Package Manager");
                    EditorGUILayout.Space();
                    
                    WrappedLabel("get_assets", EditorStyles.boldLabel);
                    WrappedLabel("Retrieves information about assets in the Unity Asset Database");
                    EditorGUILayout.Space();
                    
                    WrappedLabel("get_tests", EditorStyles.boldLabel);
                    WrappedLabel("Retrieves information about tests in the Unity Test Runner");
                    
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUILayout.EndVertical();
            }
            
            // Author information
            EditorGUILayout.Space();
            WrappedLabel("Author", _subHeaderStyle);
            
            EditorGUILayout.BeginVertical(_boxStyle);
            
            WrappedLabel("Created by CoderGamester", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            WrappedLabel("For issues, feedback, or contributions, please visit:");
            
            // Begin horizontal layout for buttons
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("GitHub: https://github.com/CoderGamester", GUILayout.Height(30)))
            {
                Application.OpenURL("https://github.com/CoderGamester");
            }
            
            if (GUILayout.Button("LinkedIn: Miguel Tomás", GUILayout.Height(30)))
            {
                Application.OpenURL("https://www.linkedin.com/in/miguel-tomas/");
            }
            
            // End horizontal layout
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            // End scroll view
            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region Helper Methods

        private void InitializeStyles()
        {
            if (_isInitialized) return;
            
            // Window title
            titleContent = new GUIContent("MCP Unity");
            
            // Header style
            _headerStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 10, 10)
            };
            
            // Sub-header style
            _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(0, 0, 10, 5)
            };
            
            // Box style
            _boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(0, 0, 10, 10)
            };
            
            // Wrapped label style for text that needs to wrap
            _wrappedLabelStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                richText = true
            };
            
            _isInitialized = true;
        }
        
        /// <summary>
        /// Creates a label with text that properly wraps based on available width
        /// </summary>
        /// <param name="text">The text to display</param>
        /// <param name="style">Optional style override (wordWrap will be forced true)</param>
        /// <param name="options">Layout options</param>
        private void WrappedLabel(string text, GUIStyle style = null, params GUILayoutOption[] options)
        {
            if (style == null)
            {
                // Use our predefined wrapped label style
                EditorGUILayout.LabelField(text, _wrappedLabelStyle, options);
                return;
            }
            
            // Create a temporary style with wordWrap enabled based on the provided style
            GUIStyle wrappedStyle = new GUIStyle(style)
            {
                wordWrap = true
            };
            
            EditorGUILayout.LabelField(text, wrappedStyle, options);
        }
        
        #endregion
    }
}
