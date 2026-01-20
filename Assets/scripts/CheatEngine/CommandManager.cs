using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Commands
{
    public partial class CommandManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_InputField textField;
        [SerializeField] private TMP_Text suggestionsText;
        [SerializeField] private TMPro.TMP_Text outputText;

        private Dictionary<string, MethodInfo> _commands = new();
        private List<string> _history = new();
        private int _historyIndex = -1;

        private bool _consoleOpen = false;
        private Player _playerRef;
        private PlayerInput _inputActions;

        private void Awake()
        {
            _playerRef = FindObjectOfType<Player>();

            _inputActions = new PlayerInput();
            _inputActions.UI.ToggleConsole.performed += ctx => ToggleConsole();
            _inputActions.UI.Enable();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                foreach (var methodInfo in assembly.GetTypes().SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)))
                {
                    var attrs = methodInfo.GetCustomAttributes<CommandAttribute>().ToList();
                    if (attrs.Count == 0) continue;

                    foreach (var a in attrs)
                    {
                        if (!_commands.ContainsKey(a.CommandName))
                            _commands.Add(a.CommandName, methodInfo);
                        else
                            Debug.LogWarning($"Command {a.CommandName} already registered (" + methodInfo.Name + ")");
                    }
                }
            }

            if (!_commands.ContainsKey("help"))
                _commands.Add("help", GetType().GetMethod(nameof(Cmd_ListCommands), BindingFlags.Instance | BindingFlags.NonPublic));

            if (textField != null)
            {
                textField.onSubmit.AddListener(OnSubmit);
                textField.gameObject.SetActive(false);
            }
            if (suggestionsText != null)
                suggestionsText.text = string.Empty;
            if (outputText != null)
                outputText.text = string.Empty;
        }

        private void Update()
        {
            if (!_consoleOpen) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.upArrowKey.wasPressedThisFrame)
            {
                NavigateHistory(up: true);
            }
            else if (kb.downArrowKey.wasPressedThisFrame)
            {
                NavigateHistory(up: false);
            }
            else if (kb.tabKey.wasPressedThisFrame)
            {
                DoAutocomplete();
            }
            else
            {
                UpdateSuggestions();
            }
        }

        private void ToggleConsole()
        {
            if (_consoleOpen) CloseConsole();
            else OpenConsole();
        }

        private void OpenConsole()
        {
            if (textField == null) return;

            _consoleOpen = true;
            textField.gameObject.SetActive(true);

            EventSystem.current.SetSelectedGameObject(textField.gameObject);
            textField.text = string.Empty;
            textField.ActivateInputField();
            textField.Select();

            if (_playerRef != null && _playerRef.playerInputActions != null)
                _playerRef.playerInputActions.Player.Disable();

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void CloseConsole()
        {
            if (textField == null) return;

            _consoleOpen = false;

            textField.DeactivateInputField();
            EventSystem.current.SetSelectedGameObject(null);
            textField.gameObject.SetActive(false);

            if (_playerRef != null && _playerRef.playerInputActions != null)
                _playerRef.playerInputActions.Player.Enable();

            Cursor.visible = false;
        }

        private void ProcessCommand(string input)
        {
            Debug.Log($"Processing command: {input}");
            var tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) return;

            var cmd = tokens[0];
            var args = tokens.Skip(1).ToArray();

            if (!_commands.TryGetValue(cmd, out var method))
            {
                Debug.LogError($"Command '{cmd}' doesn't exist");
                return;
            }

            var parameters = method.GetParameters();

            bool lastIsStringAndCatchAll = parameters.Length > 0 && parameters[parameters.Length - 1].ParameterType == typeof(string);

            if (!lastIsStringAndCatchAll && args.Length != parameters.Length)
            {
                Debug.LogError($"Command '{cmd}' expects {parameters.Length} parameters but got {args.Length}");
                return;
            }
            if (lastIsStringAndCatchAll && args.Length < parameters.Length - 1)
            {
                Debug.LogError($"Command '{cmd}' expects at least {parameters.Length - 1} parameters but got {args.Length}");
                return;
            }

            object target = null;
            if (!method.IsStatic)
            {
                var declType = method.DeclaringType;
                var component = FindObjectOfType(declType);
                if (component != null)
                    target = component;
                else
                {
                    try { target = Activator.CreateInstance(declType); }
                    catch { Debug.LogWarning($"Could not create instance of {declType}. Method must be static or on a scene component."); }
                }
            }

            var invokeParams = new object[parameters.Length];
            try
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    var p = parameters[i];

                    if (i == parameters.Length - 1 && lastIsStringAndCatchAll)
                    {
                        if (args.Length >= i + 1)
                        {
                            var rest = args.Skip(i).ToArray();
                            invokeParams[i] = string.Join(" ", rest);
                        }
                        else
                        {
                            invokeParams[i] = string.Empty;
                        }
                    }
                    else
                    {
                        invokeParams[i] = Convert.ChangeType(args[i], p.ParameterType);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Parameter conversion failed: {ex.Message}");
                return;
            }

            try
            {
                method.Invoke(target, invokeParams);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error invoking command {cmd}: {ex}");
            }
        }


        private void NavigateHistory(bool up)
        {
            if (_history.Count == 0) return;

            if (up)
            {
                if (_historyIndex < _history.Count - 1) _historyIndex++;
            }
            else
            {
                if (_historyIndex > -1) _historyIndex--;
            }

            if (_historyIndex == -1)
            {
                textField.text = string.Empty;
            }
            else
            {
                var idx = _history.Count - 1 - _historyIndex;
                if (idx >= 0 && idx < _history.Count)
                    textField.text = _history[idx];
            }

            textField.caretPosition = textField.text.Length;
            textField.ActivateInputField();
        }

        private void DoAutocomplete()
        {
            if (textField == null) return;
            var cur = textField.text ?? string.Empty;
            var tokens = cur.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) return;

            var prefix = tokens[0];
            var matches = _commands.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count == 0) return;

            if (matches.Count == 1)
            {
                var completion = matches[0];
                textField.text = completion + " ";
            }
            else
            {
                string common = LongestCommonPrefix(matches.ToArray());
                if (common.Length > prefix.Length)
                    textField.text = common;
                if (suggestionsText != null)
                    suggestionsText.text = string.Join(", ", matches.Take(12));
            }

            textField.caretPosition = textField.text.Length;
            textField.ActivateInputField();
        }

        private void UpdateSuggestions()
        {
            if (suggestionsText == null || textField == null) return;
            var cur = textField.text ?? string.Empty;
            var tokens = cur.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                suggestionsText.text = string.Empty;
                return;
            }
            var prefix = tokens[0];
            var matches = _commands.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).Take(12).ToList();
            suggestionsText.text = matches.Count == 0 ? string.Empty : string.Join(", ", matches);
        }

        private string LongestCommonPrefix(string[] strs)
        {
            if (strs == null || strs.Length == 0) return string.Empty;
            Array.Sort(strs);
            string first = strs[0];
            string last = strs[strs.Length - 1];
            int i = 0;
            while (i < first.Length && i < last.Length && first[i] == last[i]) i++;
            return first.Substring(0, i);
        }

        private void WriteOutput(string text, bool append = false)
        {
            Debug.Log("WRITE OUTPUT CALLED: " + text);

            if (outputText == null)
            {
                Debug.LogError("outputText IS NULL!");
                return;
            }

            if (append)
                outputText.text += ", " + text;
            else
                outputText.text = text;
        }

        private void OnSubmit(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                textField.text = string.Empty;
                textField.ActivateInputField();
                return;
            }

            _history.Add(text);
            _historyIndex = -1;

            WriteOutput($"> {text}", append: true);

            ProcessCommand(text);

            textField.text = string.Empty;            
            textField.ActivateInputField();
        }

        [Command("AddMoney", "Description1")]
        public void AddMoneyCommand(int amount)
        {
            CoinManager.Instance.AddCoins(amount);
        }

        [Command("RemoveMoney", "Description2")]
        public void RemoveMoneyCommand(int amount)
        {
            CoinManager.Instance.RemoveCoins(amount);
        }

        [Command("AddStats", "Description3")]
        public void AddStatsCommand(float Damage, float Speed, float AttackSpeed)
        {
            Stats.Instance.UpdateStats(Damage, Speed, AttackSpeed);
        }

        [Command("RemoveStats", "Description4")]
        public void RemoveStatsCommand(float Damage, float Speed, float AttackSpeed)
        {
            Stats.Instance.UpdateStats(-Damage, -Speed, -AttackSpeed);
        }

        private ItemDatabase GetItemDatabase()
        {
            var db = Resources.Load<ItemDatabase>("ItemDatabase");
            return db;
        }

        [Command("listcommands", "Description5")]
        private void Cmd_ListCommands()
        {
            var entries = _commands
                .Select(kv =>
                {
                    var name = kv.Key;
                    var mi = kv.Value;
                    var attr = mi.GetCustomAttribute<Commands.CommandAttribute>();
                    var desc = attr != null ? attr.CommandDescription : string.Empty;
                    return $"{name} - {desc}";
                })
                .OrderBy(s => s)
                .ToArray();

            string resultText = entries.Length > 0 ? string.Join(", ", entries) : "Brak zarejestrowanych komend.";
            WriteOutput(resultText);
            if (suggestionsText != null)
                suggestionsText.text = string.Join(", ", _commands.Keys.OrderBy(k => k).Take(12));
        }

        [Command("listallitems", "Description6")]
        private void Cmd_ListAllItems()
        {
            var db = Resources.Load<ItemDatabase>("ItemDatabase");
            string result;
            if (db != null && db.allItems != null && db.allItems.Count > 0)
            {
                var names = db.allItems.Where(x => x != null).Select(i => i.name).OrderBy(n => n).ToArray();
                result = string.Join(", ", names);
            }
            else
            {
                var items = Resources.LoadAll<ItemData>("Items");
                if (items == null || items.Length == 0)
                {
                    result = "Brak ItemData (brak ItemDatabase i Resources/Items).";
                }
                else
                {
                    var names = items.Select(i => i.name).OrderBy(n => n).ToArray();
                    result = string.Join(", ", names);
                }
            }

            WriteOutput(result);
        }

        [Command("AddItem", "Description7")]
        private void Cmd_AddItem(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
            {
                WriteOutput("Podaj nazwę przedmiotu.");
                return;
            }

            var db = Resources.Load<ItemDatabase>("ItemDatabase");
            ItemData item = db != null ? db.GetByName(itemName) : null;

            if (item == null)
                item = Resources.Load<ItemData>($"Items/{itemName}");

            if (item == null)
            {
                WriteOutput($"Item '{itemName}' nie znaleziony (ItemDatabase/Resources).");
                return;
            }

            if (GameData.Instance.collectedItems.Exists(x => x != null && x.name == item.name))
            {
                WriteOutput($"Item '{item.name}' już jest w kolekcji.");
                return;
            }

            GameData.Instance.collectedItems.Add(item);
            if (item.itemSprite != null) GameData.Instance.collectedItemIcons.Add(item.itemSprite);
            GameData.Instance.SaveToDisk();

            if (InventoryUI.Instance != null && item.itemSprite != null)
                InventoryUI.Instance.AddItemToUI(item.itemSprite, item.name, showPopup: true);

            WriteOutput($"Dodano item '{item.name}'.");
        }

        [Command("RemoveItem", "Description8")]
        private void Cmd_RemoveItem(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
            {
                WriteOutput("Podaj nazwę przedmiotu.");
                return;
            }

            int idx = GameData.Instance.collectedItems.FindIndex(x => x != null && x.name == itemName);
            if (idx == -1)
            {
                WriteOutput($"Item '{itemName}' nie znaleziony w kolekcji.");
                return;
            }

            GameData.Instance.collectedItems.RemoveAt(idx);
            if (idx < GameData.Instance.collectedItemIcons.Count)
                GameData.Instance.collectedItemIcons.RemoveAt(idx);

            GameData.Instance.SaveToDisk();

            if (InventoryUI.Instance != null)
                InventoryUI.Instance.RebuildFromGameData();

            WriteOutput($"Usunięto item '{itemName}'.");
        }


    }
}
