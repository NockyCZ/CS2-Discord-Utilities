using System.Text.RegularExpressions;

namespace DiscordUtilities
{

    public partial class DiscordUtilities
    {
        public static string ReplaceConditions(string condition, replaceData replaceData, replaceDataType dataType)
        {
            if (customConditions.TryGetValue(condition, out var variableCondition) && variableCondition != null && variableCondition.Any())
            {
                foreach (var data in variableCondition)
                {
                    string Value = data.Value;
                    string ValueToCheck = data.ValueToCheck;
                    if (Regex.IsMatch(data.Value, @"\{([^{}]*)\}") || Regex.IsMatch(data.ValueToCheck, @"\{([^{}]*)\}"))
                    {
                        switch (dataType)
                        {
                            case replaceDataType.Server:
                                if (replaceData.Server)
                                {
                                    Value = ReplaceServerDataVariables(data.Value, false);
                                    ValueToCheck = ReplaceServerDataVariables(data.ValueToCheck, false);
                                }
                                break;

                            case replaceDataType.Player:
                                var player = replaceData.Player;
                                if (player != null)
                                {
                                    Value = ReplacePlayerDataVariables(data.Value, player, false, false);
                                    ValueToCheck = ReplacePlayerDataVariables(data.ValueToCheck, player, false, false);
                                }
                                break;

                            case replaceDataType.Target:
                                var target = replaceData.Target;
                                if (target != null)
                                {
                                    Value = ReplacePlayerDataVariables(data.Value, target, true, false);
                                    ValueToCheck = ReplacePlayerDataVariables(data.ValueToCheck, target, true, false);
                                }
                                break;

                            case replaceDataType.DiscordUser:
                                var user = replaceData.DiscordUser;
                                if (user != null)
                                {
                                    Value = ReplaceDiscordUserVariables(user, data.Value, false);
                                    ValueToCheck = ReplaceDiscordUserVariables(user, data.ValueToCheck, false);
                                }
                                break;

                            case replaceDataType.DiscordChannel:
                                var channel = replaceData.DiscordChannel;
                                if (channel != null)
                                {
                                    Value = ReplaceDiscordChannelVariables(channel, data.Value, false);
                                    ValueToCheck = ReplaceDiscordChannelVariables(channel, data.ValueToCheck, false);
                                }
                                break;
                        }
                    }

                    switch (data.Operator)
                    {
                        case "~":
                            if (Value.Contains(ValueToCheck))
                                condition = ReplaceCustomFunctions(data, Value, replaceData, dataType);
                            break;
                        case "==":
                            if (Value.Equals(ValueToCheck))
                                condition = ReplaceCustomFunctions(data, Value, replaceData, dataType);
                            break;
                        case "!=":
                            if (Value != ValueToCheck)
                                condition = ReplaceCustomFunctions(data, Value, replaceData, dataType);
                            break;
                        case ">=":
                            if (IsComparable(Value, ValueToCheck) && Comparer(Value, ValueToCheck) >= 0)
                                condition = ReplaceCustomFunctions(data, Value, replaceData, dataType);
                            break;
                        case "<=":
                            if (IsComparable(Value, ValueToCheck) && Comparer(Value, ValueToCheck) <= 0)
                                condition = ReplaceCustomFunctions(data, Value, replaceData, dataType);
                            break;
                        case ">":
                            if (IsComparable(Value, ValueToCheck) && Comparer(Value, ValueToCheck) > 0)
                                condition = ReplaceCustomFunctions(data, Value, replaceData, dataType);
                            break;
                        case "<":
                            if (IsComparable(Value, ValueToCheck) && Comparer(Value, ValueToCheck) < 0)
                                condition = ReplaceCustomFunctions(data, Value, replaceData, dataType);
                            break;
                        default:
                            break;
                    }
                }
            }
            return condition;
        }

        private static bool IsComparable(string value1, string value2)
        {
            return (IsNumber(value1) && IsNumber(value2)) || (!IsNumber(value1) && !IsNumber(value2));
        }

        private static int Comparer(string value1, string value2)
        {
            if (IsNumber(value1) && IsNumber(value2))
            {
                int intValue1 = int.Parse(value1);
                int intValue2 = int.Parse(value2);
                return intValue1.CompareTo(intValue2);
            }
            else
            {
                return value1.Length.CompareTo(value2.Length);
            }
        }

        private static bool IsNumber(string value)
        {
            return int.TryParse(value, out _);
        }

        private static string ReplaceCustomFunctions(ConditionData data, string input, replaceData replaceData, replaceDataType dataType)
        {
            if (data.ReplacementValue.Contains("{Replace("))
                input = ReplaceFunction(input, data.ReplacementValue);
            else
                input = input.Replace(input, data.ReplacementValue);

            if (Regex.IsMatch(input, @"\{([^{}]*)\}"))// || Regex.IsMatch(input, @"\[([^\[\]]*)\]"))
            {
                switch (dataType)
                {
                    case replaceDataType.Server:
                        if (replaceData.Server)
                            input = ReplaceServerDataVariables(input);
                        break;

                    case replaceDataType.Player:
                        var player = replaceData.Player;
                        if (player != null)
                            input = ReplacePlayerDataVariables(input, player);
                        break;

                    case replaceDataType.Target:
                        var target = replaceData.Target;
                        if (target != null)
                            input = ReplacePlayerDataVariables(input, target, true);
                        break;

                    case replaceDataType.DiscordUser:
                        var user = replaceData.DiscordUser;
                        if (user != null)
                            input = ReplaceDiscordUserVariables(user, input);
                        break;

                    case replaceDataType.DiscordChannel:
                        var channel = replaceData.DiscordChannel;
                        if (channel != null)
                            input = ReplaceDiscordChannelVariables(channel, input);
                        break;
                }
            }
            return input;
        }

        private static string ReplaceFunction(string originalText, string input)
        {
            int startSecond = input.IndexOf(")(") + ")(".Length;
            string firstString = input.Substring("{Replace(".Length, input.IndexOf(")") - "{Replace(".Length);
            string secondString = input.Substring(startSecond, input.LastIndexOf(")") - startSecond);
            return originalText.Replace(firstString, secondString);
        }
    }
}