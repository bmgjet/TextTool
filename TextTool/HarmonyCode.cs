/*▄▄▄▄    ███▄ ▄███▓  ▄████  ▄▄▄██▀▀▀▓█████▄▄▄█████▓
 ▓█████▄ ▓██▒▀█▀ ██▒ ██▒ ▀█▒   ▒██   ▓█   ▀▓  ██▒ ▓▒
 ▒██▒ ▄██▓██    ▓██░▒██░▄▄▄░   ░██   ▒███  ▒ ▓██░ ▒░
 ▒██░█▀  ▒██    ▒██ ░▓█  ██▓▓██▄██▓  ▒▓█  ▄░ ▓██▓ ░ 
 ░▓█  ▀█▓▒██▒   ░██▒░▒▓███▀▒ ▓███▒   ░▒████▒ ▒██▒ ░ 
 ░▒▓███▀▒░ ▒░   ░  ░ ░▒   ▒  ▒▓▒▒░   ░░ ▒░ ░ ▒ ░░   
 ▒░▒   ░ ░  ░      ░  ░   ░  ▒ ░▒░    ░ ░  ░   ░    
  ░    ░ ░      ░   ░ ░   ░  ░ ░ ░      ░    ░      
  ░             ░         ░  ░   ░      ░  ░*/
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Text.RegularExpressions;
using System.Collections;

namespace TextTool
{
    public class HarmonyCode : IHarmonyModHooks
    {
        public static HarmonyCode code; //Self Reference
        //Vars
        private Canvas canvas;
        private Toggle toggle;
        private GameObject controlPanel;
        private Font font;
        private Vector3 compass = Vector3.zero;
        private Config config;
        private string ConfigFile;
        private string Status;
        private readonly string Logo =
@"
  _____         _  _____           _ 
 |_   _|____  _| ||_   _|__   ___ | |
   | |/ _ \ \/ / __|| |/ _ \ / _ \| |
   | |  __/>  <| |_ | | (_) | (_) | |
   |_|\___/_/\_\\__||_|\___/ \___/|_|
V1.1.0 by bmgjet";

        private readonly string HelpInfo = @"TextTool Help:
Console Commands:
  texttool.ui     - Opens the TextTool user interface when a new or saved map is open.
  texttool.help   - Prints this help information.
  texttool.reset  - Resets the configuration file to its default settings.

Hotkeys:
  Ctrl+P - Opens the TextTool user interface when a new or saved map is open.
  Ctrl+L - Resets the controls back to their defaults.
  ESC    - Close TextTool user interface if its open.";

        //Lookup table for char to prefabID for that letter
        private readonly Dictionary<char, uint> prefabMap = new Dictionary<char, uint>
        {
            { 'A', 2668510687 },
            { 'B', 491222911 },
            { 'C', 3358379765 },
            { 'D', 3940114902 },
            { 'E', 3690090286 },
            { 'F', 1955124163 },
            { 'G', 1978595701 },
            { 'H', 3036864937 },
            { 'I', 1218970215 },
            { 'J', 3844406919 },
            { 'K', 3481260182 },
            { 'L', 1554136027 },
            { 'M', 2528602136 },
            { 'N', 3709867063 },
            { 'O', 2489834613 },
            { 'P', 775854739 },
            { 'Q', 4105307909 },
            { 'R', 638347284 },
            { 'S', 3782502415 },
            { 'T', 1027880868 },
            { 'U', 4129468480 },
            { 'V', 2620585288 },
            { 'W', 1666935529 },
            { 'X', 2467298332 },
            { 'Y', 3679731434 },
            { 'Z', 1697319372 },
            { '0', 2740687181 },
            { '1', 1865922381 },
            { '2', 4292972501 },
            { '3', 3389230713 },
            { '4', 2256628427 },
            { '5', 3370834221 },
            { '6', 3339647907 },
            { '7', 413403103 },
            { '8', 1963342849 },
            { '9', 1024265129 }
        };

        //Lookup table for space size since not all letters are the same width
        private readonly Dictionary<char, float> sizeMap = new Dictionary<char, float>
        {
            { 'A', 0.175f },
            { 'B', 0.175f },
            { 'C', 0.175f },
            { 'D', 0.175f },
            { 'E', 0.175f },
            { 'F', 0.165f },
            { 'G', 0.180f },
            { 'H', 0.195f },
            { 'I', 0.160f },
            { 'J', 0.170f },
            { 'K', 0.180f },
            { 'L', 0.175f },
            { 'M', 0.200f },
            { 'N', 0.175f },
            { 'O', 0.180f },
            { 'P', 0.175f },
            { 'Q', 0.185f },
            { 'R', 0.185f },
            { 'S', 0.170f },
            { 'T', 0.170f },
            { 'U', 0.180f },
            { 'V', 0.175f },
            { 'W', 0.195f },
            { 'X', 0.175f },
            { 'Y', 0.175f },
            { 'Z', 0.170f },
            { '0', 0.170f },
            { '1', 0.160f },
            { '2', 0.175f },
            { '3', 0.175f },
            { '4', 0.175f },
            { '5', 0.175f },
            { '6', 0.165f },
            { '7', 0.165f },
            { '8', 0.175f },
            { '9', 0.175f }
        };
        #region Harmony Hooks
        public void OnLoaded(OnHarmonyModLoadedArgs args)//Plugin Load Hook
        {
            code = this;
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            LoadConfig();
            toggle = MenuManager.Instance.CreateWindowToggle("HarmonyMods//TextTool.png");
            toggle.onValueChanged.AddListener((value) => CreateWindow(value));
        }

        public void OnUnloaded(OnHarmonyModUnloadedArgs args)//Plugin Unload Hook
        {
            if (canvas != null) { code.CloseWindow(); }
            code = null;
        }

        //Hook Keyboard Input
        [HarmonyPatch(typeof(CameraManager), "Update")]
        public class CameraManager_Update
        {
            static bool Prefix(CameraManager __instance)
            {
                try
                {
                    if (__instance.cam == null) { return false; }
                    if (code.canvas != null) //UI Open
                    {
                        if (Keyboard.current.escapeKey.wasPressedThisFrame) //Catch esc key as hotkey
                        {
                            code.CloseWindow();
                            return false; //Block Original Code
                        }
                        if (Keyboard.current.ctrlKey.isPressed && Keyboard.current.lKey.wasPressedThisFrame) //Catch CTRL+L, Reset Layout
                        {
                            if (code.canvas != null) { code.ResetLayout(); }
                        }
                        return true; //Run Normal Code
                    }
                    if (Keyboard.current.ctrlKey.isPressed && Keyboard.current.pKey.wasPressedThisFrame) //CTRL+P keys
                    {
                        code.CreateWindow(true);
                        return false;
                    }
                }
                catch { }
                return true;
            }
        }

        //Catch Console Window Submit
        [HarmonyPatch(typeof(ConsoleWindow), "OnSubmit")]
        public class ConsoleWindow_OnSubmit
        {
            static bool Prefix(ConsoleWindow __instance)
            {
                //Switch case to catch keywords
                switch (__instance.consoleInput.text.ToLower())
                {
                    case "texttool.ui":
                        {
                            try { code.CreateWindow(true); }
                            catch (Exception e) { __instance.Post(e.ToString()); }
                            __instance.consoleInput.text = string.Empty; //Blank console text input
                            return false;
                        }
                    case "texttool.help":
                        {
                            __instance.PostMultiLine(HarmonyCode.code.HelpInfo);
                            __instance.consoleInput.text = string.Empty;
                            return false;
                        }
                    case "texttool.reset":
                        {
                            __instance.Post("Resettings Config file!");
                            HarmonyCode.code.CreateConfig();
                            File.WriteAllText(HarmonyCode.code.ConfigFile, JsonUtility.ToJson(HarmonyCode.code.config, true)); //Save config
                            __instance.consoleInput.text = string.Empty;
                            return false;
                        }
                }
                return true;
            }
        }

        //Hook ConsoleWindow Load
        [HarmonyPatch(typeof(ConsoleWindow), "Startup")]
        public class ConsoleWindow_Startup
        {
            static void Postfix(ConsoleWindow __instance)
            {
                try
                {
                    __instance.PostMultiLine(code.Logo);//Post logo to console window
                    if (!string.IsNullOrEmpty(code.Status))
                    {
                        __instance.Post(code.Status);
                        __instance.consoleInput.text = "";
                        __instance.consoleInput.ActivateInputField();
                        code.Status = null;
                    }
                }
                catch { }
            }
        }
        #endregion

        #region Config
        //Config File Layout And Defaults
        class Config
        {
            public Vector2 ButtonScale = Vector2.zero;
            public Vector2 ButtonPosition = Vector3.zero;
        }

        void LoadConfig()
        {
            string ConfigPath = Path.Combine(AccessTools.Field(typeof(HarmonyLoader), "modPath").GetValue(null).ToString(), "HarmonyConfig");
            if (!Directory.Exists(ConfigPath)) { Directory.CreateDirectory(ConfigPath); }
            ConfigFile = Path.Combine(ConfigPath, "TextTool.json"); //Config File Name
            Status = "[TextTool] ";
            if (File.Exists(ConfigFile))
            {
                try
                {
                    config = JsonUtility.FromJson<Config>(File.ReadAllText(ConfigFile));
                    Status += "Config Loaded";
                }
                catch
                {
                    CreateConfig();
                    Status += "Error Loading Config";
                }
            }
            else
            {
                CreateConfig();
                File.WriteAllText(ConfigFile, JsonUtility.ToJson(config, true)); //Save config
                Status += "Created New Config";
            }
            //Output to console window if its loaded.
            if (ConsoleWindow.Instance != null)
            {
                ConsoleWindow.Instance.PostMultiLine(Logo);
                ConsoleWindow.Instance.Post(Status);
                ConsoleWindow.Instance.consoleInput.text = "";
                ConsoleWindow.Instance.consoleInput.ActivateInputField();
                Status = null;
            }
        }

        void CreateConfig()
        {
            // Create Default Config File
            config = new Config();
        }
        #endregion

        #region Methods
        IEnumerator CreatePrefab(string inputText)
        {
            LoadScreen.Instance.isEnabled = false; //Unlock controls so scene can update.
            float length = ((inputText.Length * 0.17f) / 2) * -1; //Offset center
            Vector3 position = (CameraManager.Instance.position - PrefabManager.PrefabParent.position) + new Vector3(length, 0, 1f); //Slightly Infront
            Vector3 rotation = new Vector3(0, 180, 0); //Always face north since only one side is visible on letter prefabs.
            Vector3 Space = new Vector3(0.18f, 0, 0); //Gap between letters
            yield return new WaitForSeconds(0.1f); //Wait 100ms for Scene to update from unlocking controls
            foreach (char c in inputText)
            {
                //Start spawning Letter Prefabs
                try
                {
                    position.x += PrefabSize(c);// Adjust for spacing between prefabs
                    if (Char.IsLetterOrDigit(c))
                    {
                        uint id = SpawnPrefab(c); //Lookup PrefabID
                        if (id != 0)
                        {
                            try
                            {
                                //Create Prefab
                                var go = PrefabManager.Load(id);
                                GameObject gameObject = UnityEngine.Object.Instantiate(go, PrefabManager.PrefabParent);
                                Transform transform = gameObject.transform;
                                transform.SetLocalPositionAndRotation(position, Quaternion.Euler(rotation));
                                transform.localScale = Vector3.one;
                                gameObject.name = go.name;
                                try { gameObject.SetActive(value: true); }
                                catch (Exception ex) { UnityEngine.Debug.LogError("Failed to activate " + gameObject.name + ": " + ex.Message); }
                            }
                            catch (Exception ex2) { UnityEngine.Debug.LogError("Invalid prefab: " + ex2.Message); }
                        }
                    }
                }
                catch { }
            }
            yield return new WaitForSeconds(0.1f); //Wait 100ms for scene to update
            CameraManager.Instance.SetCameraPosition(CameraManager.Instance.position); //Set camera position to force screen update
            LoadScreen.Instance.isEnabled = true; //Lock controls again
            yield break;
        }

        uint SpawnPrefab(char inputText)
        {
            if (prefabMap.TryGetValue(inputText, out uint prefabId)) { return prefabId; }
            // Return 0 if no matching prefab is found
            return 0;
        }

        float PrefabSize(char inputText)
        {
            if (sizeMap.TryGetValue(inputText, out float prefabSize)) { return prefabSize; }
            // Return 0 if no matching prefab is found
            return 0.17f;
        }

        bool IsValidInput(string text)
        {
            text = text.Replace("\u00A0", " ").Trim(); // Replace non-breaking space (Unicode: \u00A0)
            foreach (char c in text) { if (!Char.IsLetterOrDigit(c) && c != ' ') { return false; } }
            return true;
        }

        void Scale(bool save)
        {
            if (controlPanel != null)
            {
                RectTransform rect = controlPanel.GetComponent<RectTransform>();
                if (rect != null)
                {
                    if (save)
                    {
                        // Save scale and position
                        config.ButtonScale = rect.sizeDelta;
                        config.ButtonPosition = rect.anchoredPosition;
                        File.WriteAllText(code.ConfigFile, JsonUtility.ToJson(config, true)); // Save config file
                    }
                    else
                    {
                        //Restore scale and position
                        if (code.config.ButtonScale != null && code.config.ButtonScale != Vector2.zero) { rect.sizeDelta = code.config.ButtonScale; }
                        if (code.config.ButtonPosition != null && code.config.ButtonPosition != Vector2.zero) { rect.anchoredPosition = code.config.ButtonPosition; }
                    }
                }
            }
        }

        void CreateWindow(bool togglevalue)
        {
            if (canvas != null || togglevalue == false)
            {
                CloseWindow();
                return;
            }
            code.CreateControlPanel();
        }

        void CloseWindow()
        {
            Scale(true); //Save Scale/position
            if (canvas != null) { GameObject.Destroy(canvas.gameObject); }//Destroy UI Window
            LoadScreen.Instance.isEnabled = false; //Allow Key/Mouse Input On RustMapper
            Compass.Instance.transform.position = compass; //Restore compass position
            if(toggle != null) { toggle.isOn = false; }
        }

        void ResetLayout()
        {
            //Reset to defaults when hot key used
            if (controlPanel != null)
            {
                RectTransform rect = controlPanel.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.sizeDelta = new Vector2(90, 90);
                    rect.anchoredPosition = new Vector2(400, 200);
                    code.config.ButtonScale = rect.sizeDelta;
                    code.config.ButtonPosition = rect.anchoredPosition;
                    File.WriteAllText(code.ConfigFile, JsonUtility.ToJson(code.config, true)); // Save config file
                }
            }
        }
        #endregion

        #region UI
        void CreateControlButtons()
        {
            // Create label "Enter Text:"
            GameObject label = new GameObject("Label");
            label.transform.SetParent(controlPanel.transform);
            RectTransform labelRect = label.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.05f, 0.85f);
            labelRect.anchorMax = new Vector2(0.95f, 0.95f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            Text labelText = label.AddComponent<Text>();
            labelText.text = "Enter Text:";
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;
            labelText.font = font;
            labelText.resizeTextForBestFit = true;
            labelText.resizeTextMinSize = 5;
            labelText.resizeTextMaxSize = 30;
            labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            labelText.verticalOverflow = VerticalWrapMode.Overflow;

            // Create TextInput
            GameObject inputFieldGO = new GameObject("InputField");
            inputFieldGO.transform.SetParent(controlPanel.transform);
            RectTransform inputRect = inputFieldGO.AddComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0.05f, 0.72f);
            inputRect.anchorMax = new Vector2(0.95f, 0.84f); // Increased height
            inputRect.offsetMin = Vector2.zero;
            inputRect.offsetMax = Vector2.zero;
            Image inputImage = inputFieldGO.AddComponent<Image>();
            inputImage.color = Color.gray;
            InputField inputField = inputFieldGO.AddComponent<InputField>();
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(inputFieldGO.transform);
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5f, 2f);
            textRect.offsetMax = new Vector2(-5f, -2f);
            Text inputText = textGO.AddComponent<Text>();
            inputText.text = "";
            inputText.color = Color.black;
            inputText.font = font;
            inputText.resizeTextForBestFit = true;
            inputText.resizeTextMinSize = 8;
            inputText.resizeTextMaxSize = 24;
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.horizontalOverflow = HorizontalWrapMode.Overflow;
            inputText.verticalOverflow = VerticalWrapMode.Overflow;
            inputField.textComponent = inputText;
            GameObject placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(inputFieldGO.transform);
            Text placeholderText = placeholderGO.AddComponent<Text>();
            placeholderText.text = "Enter text...";
            placeholderText.color = Color.gray;
            placeholderText.font = font;
            placeholderText.resizeTextForBestFit = true;
            placeholderText.resizeTextMinSize = 8;
            placeholderText.resizeTextMaxSize = 24;
            placeholderText.alignment = TextAnchor.MiddleLeft;
            inputField.placeholder = placeholderText;

            // Add Tooltip
            Tooltip tooltip = inputFieldGO.AddComponent<Tooltip>();
            tooltip.text = "Text entered here will be converted to rusts letters/numbers prefabs.";

            // Create "Create Prefab" Button
            GameObject createButton = new GameObject("CreatePrefab");
            createButton.transform.SetParent(controlPanel.transform);
            RectTransform createBtnRect = createButton.AddComponent<RectTransform>();
            createBtnRect.anchorMin = new Vector2(0.1f, 0.35f);
            createBtnRect.anchorMax = new Vector2(0.9f, 0.55f); 
            createBtnRect.offsetMin = Vector2.zero;
            createBtnRect.offsetMax = Vector2.zero;
            Image createButtonImage = createButton.AddComponent<Image>();
            createButtonImage.color = new Color(0.45f, 0.55f, 0.26f);
            Button createBtnComponent = createButton.AddComponent<Button>();
            createBtnComponent.interactable = false;
            inputField.onValueChanged.AddListener(text => createBtnComponent.interactable = !string.IsNullOrEmpty(text) && IsValidInput(text));
            GameObject createTextGO = new GameObject("Text");
            createTextGO.transform.SetParent(createButton.transform);
            RectTransform createTextRect = createTextGO.AddComponent<RectTransform>();
            createTextRect.anchorMin = Vector2.zero;
            createTextRect.anchorMax = Vector2.one;
            createTextRect.offsetMin = Vector2.zero;
            createTextRect.offsetMax = Vector2.zero;
            Text createButtonText = createTextGO.AddComponent<Text>();
            createButtonText.text = "Create Prefab";
            createButtonText.alignment = TextAnchor.MiddleCenter;
            createButtonText.color = Color.white;
            createButtonText.font = font;
            createButtonText.resizeTextForBestFit = true;
            createButtonText.resizeTextMinSize = 5;
            createButtonText.resizeTextMaxSize = 20;
            createBtnComponent.targetGraphic = createButtonImage;
            createBtnComponent.onClick.AddListener(() => CoroutineManager.Instance.StartRuntimeCoroutine(CreatePrefab(inputField.text.Trim().ToUpper())));

            // Create "Close" Button
            GameObject closeButton = new GameObject("Close");
            closeButton.transform.SetParent(controlPanel.transform);
            RectTransform closeBtnRect = closeButton.AddComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(0.1f, 0.15f);
            closeBtnRect.anchorMax = new Vector2(0.9f, 0.30f); 
            closeBtnRect.offsetMin = Vector2.zero;
            closeBtnRect.offsetMax = Vector2.zero;
            Image closeButtonImage = closeButton.AddComponent<Image>();
            closeButtonImage.color = new Color(0.70f, 0.22f, 0.16f);
            Button closeBtnComponent = closeButton.AddComponent<Button>();
            GameObject closeTextGO = new GameObject("Text");
            closeTextGO.transform.SetParent(closeButton.transform);
            RectTransform closeTextRect = closeTextGO.AddComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;
            Text closeButtonText = closeTextGO.AddComponent<Text>();
            closeButtonText.text = "Close";
            closeButtonText.alignment = TextAnchor.MiddleCenter;
            closeButtonText.color = Color.white;
            closeButtonText.font = font;
            closeButtonText.resizeTextForBestFit = true;
            closeButtonText.resizeTextMinSize = 5;
            closeButtonText.resizeTextMaxSize = 20;
            closeBtnComponent.targetGraphic = closeButtonImage;
            closeBtnComponent.onClick.AddListener(() => CloseWindow());
        }

        void CreateControlPanel()
        {
            if (canvas != null) { GameObject.Destroy(canvas); }
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPhysicalSize;
            canvasObj.AddComponent<GraphicRaycaster>();
            controlPanel = new GameObject("ControlPanel");
            controlPanel.transform.SetParent(canvas.transform);
            RectTransform rect = controlPanel.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120, 90);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(400, 200);
            // Set background color to #2D2C24
            Image panelImage = controlPanel.AddComponent<Image>();
            panelImage.color = new Color(0.17f, 0.17f, 0.14f);
            controlPanel.AddComponent<Draggable>();
            //Create Buttons
            CreateControlButtons();
            CreateScaleHandle();
            Scale(false); //Load Scale
            LoadScreen.Instance.isEnabled = true; //Lock Controls
            if(compass == Vector3.zero){compass = Compass.Instance.transform.position;}
            Compass.Instance.transform.position += (Compass.Instance.transform.up * 500); //Hide Compass
        }

        void CreateScaleHandle()
        {
            // Create Scale Handle (Clone from MenuManager.scaleButton)
            if (MenuManager.Instance != null && MenuManager.Instance.scaleButton != null)
            {
                var scaleHandle = GameObject.Instantiate(MenuManager.Instance.scaleButton.gameObject, controlPanel.transform);
                scaleHandle.name = "ScaleHandle";
                RectTransform rect2 = scaleHandle.GetComponent<RectTransform>();
                rect2.anchorMin = Vector2.zero;
                rect2.anchorMax = Vector2.zero;
                rect2.pivot = new Vector2(0, 0);
                rect2.anchoredPosition = new Vector2(rect2.sizeDelta.x * 0.5f, 0f);
                Image scaleImage = scaleHandle.GetComponent<Image>();
                if (scaleImage != null) { scaleImage.color = MenuManager.Instance.scaleButton.image.color; }
                scaleHandle.AddComponent<DraggableScaler>().target = controlPanel.GetComponent<RectTransform>();
            }
        }
        #endregion

        #region MonoBehaviour


        public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public string text;
            private GameObject tooltipObject;
            private Text tooltipText;
            private InputField inputField;
            private bool isInputSelected = false;

            void Start()
            {
                inputField = GetComponent<InputField>();
                if (inputField != null)
                {
                    inputField.onValueChanged.AddListener(OnTextChanged);
                    AddEventTrigger(inputField.gameObject, EventTriggerType.Select, OnInputSelected);
                    AddEventTrigger(inputField.gameObject, EventTriggerType.Deselect, OnInputDeselected);
                }
                CreateTooltip();
            }

            void CreateTooltip()
            {
                tooltipObject = new GameObject("Tooltip");
                tooltipObject.transform.SetParent(transform.parent);
                tooltipObject.SetActive(false);

                RectTransform rect = tooltipObject.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0, 40);
                rect.sizeDelta = new Vector2(250, 50);

                Image bg = tooltipObject.AddComponent<Image>();
                bg.color = new Color(0, 0, 0, 0.8f);

                GameObject textGO = new GameObject("TooltipText");
                textGO.transform.SetParent(tooltipObject.transform);

                RectTransform textRect = textGO.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(10, 5);
                textRect.offsetMax = new Vector2(-10, -5);

                tooltipText = textGO.AddComponent<Text>();
                tooltipText.text = text;
                tooltipText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                tooltipText.color = Color.white;
                tooltipText.alignment = TextAnchor.MiddleCenter;
                tooltipText.resizeTextForBestFit = true;
                tooltipText.resizeTextMinSize = 10;
                tooltipText.resizeTextMaxSize = 20;

                tooltipObject.SetActive(false);
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (!isInputSelected && string.IsNullOrEmpty(inputField.text))
                {
                    tooltipObject.SetActive(true);
                }
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                tooltipObject.SetActive(false);
            }

            private void OnInputSelected(BaseEventData eventData)
            {
                isInputSelected = true;
                tooltipObject.SetActive(false);
            }

            private void OnInputDeselected(BaseEventData eventData)
            {
                isInputSelected = false;
                if (string.IsNullOrEmpty(inputField.text))
                {
                    tooltipObject.SetActive(true);
                }
            }

            private void OnTextChanged(string text)
            {
                tooltipObject.SetActive(string.IsNullOrEmpty(text) && !isInputSelected);
            }

            private void AddEventTrigger(GameObject obj, EventTriggerType type, System.Action<BaseEventData> callback)
            {
                EventTrigger trigger = obj.GetComponent<EventTrigger>() ?? obj.AddComponent<EventTrigger>();
                EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
                entry.callback.AddListener((eventData) => callback(eventData));
                trigger.triggers.Add(entry);
            }
        }

        public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler
        {
            private RectTransform rectTransform;
            private Vector2 offset;
            private Canvas canvas;

            void Awake()
            {
                rectTransform = GetComponent<RectTransform>();
                canvas = GetComponentInParent<Canvas>(); // Ensure the object is inside a Canvas
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform.parent as RectTransform, eventData.position, canvas.worldCamera, out Vector2 localPoint);
                offset = rectTransform.anchoredPosition - localPoint;
            }

            public void OnDrag(PointerEventData eventData)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform.parent as RectTransform, eventData.position, canvas.worldCamera, out Vector2 localPoint);
                rectTransform.anchoredPosition = localPoint + offset;
            }
        }

        public class DraggableScaler : MonoBehaviour, IDragHandler
        {
            public RectTransform target;
            public Vector2 minSize = new Vector2(70, 70);  // Minimum width and height
            public Vector2 maxSize = new Vector2(250, 250); // Maximum width and height
            public void OnDrag(PointerEventData eventData)
            {
                if (target == null) { return; }
                // Determine the dominant delta (use the most significant movement)
                float delta = Mathf.Max(Mathf.Abs(eventData.delta.x), Mathf.Abs(eventData.delta.y));
                delta *= -1; //Reverse Direction
                if (eventData.delta.x < 0 || eventData.delta.y < 0) { delta = -delta; }
                // Maintain aspect ratio while resizing
                float aspectRatio = target.sizeDelta.x / target.sizeDelta.y;
                Vector2 newSize = target.sizeDelta + new Vector2(delta, delta / aspectRatio);
                // Clamp the size within min and max limits
                newSize.x = Mathf.Clamp(newSize.x, minSize.x, maxSize.x);
                newSize.y = newSize.x / aspectRatio;
                // Ensure y size also respects limits
                if (newSize.y < minSize.y)
                {
                    newSize.y = minSize.y;
                    newSize.x = newSize.y * aspectRatio;
                }
                else if (newSize.y > maxSize.y)
                {
                    newSize.y = maxSize.y;
                    newSize.x = newSize.y * aspectRatio;
                }
                // Apply the clamped size
                target.sizeDelta = newSize;
            }
        }
        #endregion
    }
}