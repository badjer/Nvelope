﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nvelope;
using System.IO;
using System.Reflection;

namespace Nvelope.IO
{
    public struct CommandArg
    {  
        public string Name;
        public Type Type;
        public bool IsOptional;

        public override string ToString()
        {
            return Name.And(Type.Name).Join(" ")
                + (IsOptional ? "*" : "");
        }
    }

    public static class CommandArgExtensions
    {
        public static bool IsFlag(this CommandArg arg)
        {
            return arg.IsOptional && arg.Type == typeof(bool);
        }
    }

    public struct ParseError
    {
        public CommandArg? Argument;
        public string ArgName;
        public object Value;

        public override string ToString()
        {
            return ArgName;
        }
    }

    public class ParseException : Exception
    {
        public ParseException(IEnumerable<ParseError> errors) : base()
        {
            Errors = errors.ToList();
        }

        public IEnumerable<ParseError> Errors { get; protected set; }
    }

    /// <summary>
    /// Provides methods for implementing a command-line or REPL from C#, and related utilities
    /// </summary>
    public class CommandParser
    {
        /// <summary>
        /// Convert a text command into a dict of arguments, based on the supplied expected arguments.
        /// Throws ParseException if there's a problem
        /// </summary>
        /// <param name="commandText"></param>
        /// <param name="expectedArgs"></param>
        /// <returns></returns>
        public Dictionary<string, object> Parse(string commandText, IEnumerable<CommandArg> expectedArgs = null)
        {
            expectedArgs = SanitizeArgs(expectedArgs);

            var lexed = Lex(commandText);
            var lexErrors = LexErrors(lexed).ToList();
            if (lexErrors.Any())
                throw new ParseException(lexErrors);

            var flags = expectedArgs.Where(a => a.IsFlag()).Select(a => a.Name).ToSet();
            var parsed = ParseArgs(lexed, flags);
            var parseErrors = ParseErrors(parsed).ToList();
            if (parseErrors.Any())
                throw new ParseException(parseErrors);

            var assigned = AssignArgs(parsed, expectedArgs);
            var assignErrors = AssignErrors(assigned, expectedArgs);
            if (assignErrors.Any())
                throw new ParseException(assignErrors);

            var converted = ConvertArgs(assigned, expectedArgs);
            var convertErrors = ConvertErrors(converted, expectedArgs);
            if (convertErrors.Any())
                throw new ParseException(convertErrors);

            return converted.ToDictionary();
        }

        public IEnumerable<CommandArg> SanitizeArgs(IEnumerable<CommandArg> args)
        {
            if (args == null)
                return new CommandArg[] { };

            var nums = 0.Inc().Select(i => i.ToString()).GetEnumerator();
            return args.Select(a => a.Name != null ? a : new CommandArg() { Name = nums.Pop(), IsOptional = a.IsOptional, Type = a.Type })
                .ToList();
        }

        public IEnumerable<string> Lex(string commandText)
        {
            if (commandText.IsNullOrEmpty())
                return new string[] { };

            return commandText.Tokenize("^\\s*(\"[^\"]*\"|[^\\s]+)")
                .Select(s => s.Trim('"'));
        }

        public IEnumerable<ParseError> LexErrors(
            IEnumerable<string> tokens)
        {
            yield break;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="flags">The names of fields that don't require a value</param>
        /// <returns></returns>
        public IEnumerable<KeyValuePair<string, string>> ParseArgs(IEnumerable<string> tokens, IEnumerable<string> flags)
        {
            if (!tokens.Any())
                return new KeyValuePair<string,string>[]{};

            // If first is a switchname, see if the second is value. If so, take that too
            var firstName = IsName(tokens.First());
            // If the first token is a name, that's the name, otherwise the name is ""
            var name = firstName ? ToName(tokens.First()) : null;
            var firstFlag = firstName && flags.Contains(name);
            var secondVal = tokens.AtLeast(2) && firstName && !IsName(tokens.Second());
            var secondBool = secondVal && tokens.Second().CanConvertTo<bool>();
            
            // If first token is a name, and the second token is a value, second is the value
            // If first token is a name, and the second token isn't a value, the value is ""
            // If the first token is a value, that's the value
            // IF the first token is a flag, then the value is "", unless the second token is a bool
            string val = null;
            if (firstFlag)
                if(secondBool)
                    val = tokens.Second();
                else
                    val = null;
            else if (firstName)
                if (secondVal)
                    val = tokens.Second();
                else
                    val = null;
            else
                val = tokens.First();

            var numTaken = name.And(val).Count(s => s != null);
            var remainder = tokens.Skip(numTaken);

            return new KeyValuePair<string, string>(name, val)
                .And(ParseArgs(remainder, flags));
        }

        public IEnumerable<ParseError> ParseErrors(
            IEnumerable<KeyValuePair<string, string>> parsedArgs)
        {
            yield break;
        }

        public IEnumerable<KeyValuePair<string,string>> AssignArgs(
            IEnumerable<KeyValuePair<string,string>> parsedArgs, IEnumerable<CommandArg> expectedArgs)
        {
            // We assume here that all of the non-optional args come first in the list, otherwise
            // wierd assignment outcomes may result.

            // An infinite sequence of arg names to assign to the args that have no names
            var reqArgNames = expectedArgs.Select(a => a.Name);
            var argNames = reqArgNames
                .And(0.Inc().Select(i => i.ToString()).Except(reqArgNames)) // Start at 0, but don't use any names that requiredArgs have
                .GetEnumerator();

            foreach(var kv in parsedArgs)
                if(kv.Key == null)
                    yield return new KeyValuePair<string,string>(argNames.Pop(), kv.Value);
                else
                    yield return kv;
        }

        public IEnumerable<ParseError> AssignErrors(
            IEnumerable<KeyValuePair<string, string>> parsedArgs, IEnumerable<CommandArg> expectedArgs)
        {
            if (!expectedArgs.Any())
                yield break;

            var required = expectedArgs.Where(a => !a.IsOptional).Select(a => a.Name).ToSet();
            var supplied = parsedArgs.Select(kv => kv.Key).ToSet();
            var missing = required.Except(supplied);
            foreach (var mi in missing)
            {
                var arg = expectedArgs.Single(a => a.Name == mi);
                yield return new ParseError() { Argument = arg, ArgName = mi };
            }

            var allowed = expectedArgs.Select(a => a.Name).ToSet();
            var invalid = supplied.Except(allowed);
            foreach (var i in invalid)
            {
                var val = parsedArgs.Single(kv => kv.Key == i).Value;
                yield return new ParseError() { ArgName = i, Value = val };
            }
        }

        public IEnumerable<KeyValuePair<string, object>> ConvertArgs(
            IEnumerable<KeyValuePair<string, string>> parsedArgs, IEnumerable<CommandArg> expectedArgs)
        {
            foreach (var kv in parsedArgs)
            {
                var arg = expectedArgs.Where(c => c.Name == kv.Key);
                var type = arg.Select(a => a.Type).FirstOr(typeof(string));
                var isOptional = arg.Select(a => a.IsOptional).FirstOr(true);
                var value = kv.Value == null? null : kv.Value.ConvertAs(type);

                // Special case: if the arg is a list of strings, we can convert that
                if(!kv.Value.IsNullOrEmpty() && type == typeof(IEnumerable<string>) || type == typeof(List<string>))
                    value = kv.Value.Tokenize("^\\,*(\"[^\"]*\"|[^\\,]+)").Select(s => s.Trim('"'));

                // Special case: if the arg is optional and a bool, and no value is specified, we treat
                // it as a flag - just by being present it assumes the value true
                // If there's no corresponding arg, and the value is null, it's also a flag
                var isFlag = (isOptional && type == typeof(bool) && value == null) || (!arg.Any() && value == null);
                if(isFlag)
                    value = true;

                yield return new KeyValuePair<string, object>(kv.Key, value);
            }
        }

        public IEnumerable<ParseError> ConvertErrors(
            IEnumerable<KeyValuePair<string, object>> convertedArgs, IEnumerable<CommandArg> expectedArgs)
        {
            if (!expectedArgs.Any())
                yield break;

            // Check the type of each argument
            foreach (var kv in convertedArgs)
            {
                var arg = expectedArgs.Single(a => a.Name == kv.Key);
                if (kv.Value != null && !(arg.Type.IsAssignableFrom(kv.Value.GetType())))
                    yield return new ParseError() { Argument = arg, ArgName = kv.Key, Value = kv.Value };

                // If the argument is required, and it's null, that's an error too
                if (!arg.IsOptional && kv.Value == null)
                    yield return new ParseError() { Argument = arg, ArgName = kv.Key, Value = kv.Value };
            }
        }

        public bool IsName(string token)
        {
            return token.StartsWith("-");
        }

        public string ToName(string token)
        {
            return token.TrimStart('-');
        }
    }
}
