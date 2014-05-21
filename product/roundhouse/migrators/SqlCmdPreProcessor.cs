namespace roundhouse.migrators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    public class SqlCmdPreProcessor
    {
        private const string COMMENT = "-- ";

        private const string VARIABLE_TEMPLATE = "$({0})";

        private List<Regex> commands = new List<Regex>()
                                                  {
                                                      new Regex(@"\s*:ED", RegexOptions.IgnoreCase),
                                                      new Regex(@"\s*:Error", RegexOptions.IgnoreCase),
                                                      new Regex(@"\s*:!!", RegexOptions.IgnoreCase),
                                                      new Regex(@"\s*:Perftrace", RegexOptions.IgnoreCase),
                                                      new Regex(@"\s*:Quit", RegexOptions.IgnoreCase),
                                                      new Regex(@"\s*:Exit", RegexOptions.IgnoreCase),
                                                      new Regex(@"\s*:Help", RegexOptions.IgnoreCase),
                                                      new Regex(@"\s*:XML", RegexOptions.IgnoreCase),
                                                      new Regex(@"\s*:r", RegexOptions.IgnoreCase),
                                                      new Regex(@"\s*:ServerList", RegexOptions.IgnoreCase),
                                                      new Regex(@"\s*:Listvar", RegexOptions.IgnoreCase),
                                                      new Regex(@"\s*:On Error", RegexOptions.IgnoreCase),
                                                      new Regex(@"\s*:Setvar", RegexOptions.IgnoreCase)
                                                  };

        private Dictionary<string, string> variables = new Dictionary<string, string>();

        public string Process(string input)
        {
            string[] lines = input.Replace(Environment.NewLine, "\n").Split('\n');
            StringBuilder output = new StringBuilder(input.Length);
            
            foreach (var line in lines)
            {
                this.ScanForVariable(line);
                output.AppendLine(this.ProcessLine(line));
            }

            return output.ToString();
        }

        private string ProcessLine(string line)
        {
            string output = null;
            if (line.TrimStart().StartsWith(":"))
            {
                if (this.commands.Any(regex => regex.IsMatch(line)))
                {
                    output = this.CommentOutLine(line);
                }
                else
                {
                    throw new Exception(string.Format("Unsupported command in line: {0}", line));
                }
            }
            else
            {
                output = this.EvaluateVariables(line);
            }

            return output;
        }

        private string EvaluateVariables(string line)
        {
            return this.variables.Aggregate(line, (current, variable) => current.Replace(string.Format(VARIABLE_TEMPLATE, variable.Key), variable.Value));
        }

        private string CommentOutLine(string line)
        {
            return COMMENT + line;
        }

        private void ScanForVariable(string line)
        {
            KeyValuePair<string, string>? keyValuePair = SqlCmdVariableParser.Parse(line);
            if (keyValuePair != null)
            {
                this.variables.Add(keyValuePair.Value.Key, keyValuePair.Value.Value);
            }
        }
    }

    public static class SqlCmdVariableParser
    {
        private static readonly Regex variableRegex = new Regex("\\s*:Setvar\\s+(\\w+)\\s+\\\"(.+)\\\"\\s*", RegexOptions.IgnoreCase);

        public static KeyValuePair<string, string>? Parse(string input)
        {
            Match variable = variableRegex.Match(input);
            if (variable.Success)
            {
                return new KeyValuePair<string, string>(variable.Groups[1].Value, variable.Groups[2].Value);
            }

            return null;
        }
    }
}
