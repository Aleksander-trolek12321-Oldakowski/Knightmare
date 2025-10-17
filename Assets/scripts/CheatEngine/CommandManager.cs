using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Commands
{
    public partial class CommandManager : MonoBehaviour
    {
        [SerializeField] TMP_InputField textField;
        private Dictionary<string, MethodInfo> _commands = new();
        private string _input;
        private void Awake()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                foreach (MethodInfo methodInfo in assembly.GetTypes().SelectMany(classType => classType.GetMethods()))
                {
                    var attributes = methodInfo.GetCustomAttributes<CommandAttribute>().ToList();
                    if (attributes.Count == 0) continue;

                    foreach (CommandAttribute attribute in attributes)
                    {
                        Debug.Log($"{attribute.CommandName} | {methodInfo.Name}");
                        _commands.Add(attribute.CommandName, methodInfo);
                    }
                }
            }

            textField.onSubmit.AddListener(OnSubmit);
        }

        private void OnSubmit(string text)
        {
            _input = text;
            ProcessCommand();
            _input = "";
            textField.text = "";
        }

        private void ProcessCommand()
        {
            Debug.Log("Command Process");

            string[] tokens = _input.Split(' ');
            string[] parameterTokens = tokens.Skip(1).ToArray();

            if (tokens.Length == 0) return;

            if (!_commands.TryGetValue(tokens[0], out var methodInfo))
            {
                Debug.LogError($"Command \"{tokens[0]}\" doesn't exist");
                return;
            }

            ParameterInfo[] parameterInfos = methodInfo.GetParameters();

            if (parameterInfos.Length != parameterTokens.Length)
            {
                Debug.LogError($"Error while handling command \"{tokens[0]}\". Expected {parameterInfos.Length} parameters");
            }

            List<object> invocationParams = new List<object>();
            for (int i = 0; i < parameterInfos.Length; i++)
            {
                var parameterInfo = parameterInfos[i];
                invocationParams.Add(Convert.ChangeType(parameterTokens[i], parameterInfo.ParameterType));
            }

            methodInfo.Invoke(this, invocationParams.ToArray());
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
    }
}
