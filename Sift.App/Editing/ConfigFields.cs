using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using Sift.Core.Enums;
using Sift.Core.Interfaces;

namespace Sift.App.Editing
{
    public enum FieldKind
    {
        Integer,
        Boolean,
        Choice,
        Text,
        Unsupported
    }

    public sealed record ConfigField(string Name, string Label, FieldKind Kind, object? Value, int Minimum, int Maximum, IReadOnlyList<object> Options);

    // NOTE: Edits component configs generically through their positional record constructors (and any [Range] on the
    // parameters), so a new component type gets an editor without any UI code
    public static class ConfigFields
    {
        private static readonly string[] NOTE_NAMES = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];

        public static IReadOnlyList<ConfigField> Describe(IComponentConfig config)
        {
            var type = config.GetType();
            var fields = new List<ConfigField>();

            foreach (var parameter in PrimaryConstructor(type).GetParameters())
            {
                var name = parameter.Name!;
                var value = Property(type, name)?.GetValue(config);
                var range = parameter.GetCustomAttribute<RangeAttribute>();
                var parameterType = parameter.ParameterType;

                var kind = parameterType == typeof(int) ? FieldKind.Integer
                    : parameterType == typeof(bool) ? FieldKind.Boolean
                    : parameterType.IsEnum ? FieldKind.Choice
                    : parameterType == typeof(string) ? FieldKind.Text
                    : FieldKind.Unsupported;

                var options = parameterType.IsEnum ? Enum.GetValues(parameterType).Cast<object>().ToList() : [];

                fields.Add(new ConfigField(
                    name,
                    Humanise(name),
                    kind,
                    value,
                    range != null ? Convert.ToInt32(range.Minimum) : -9999,
                    range != null ? Convert.ToInt32(range.Maximum) : 9999,
                    options));
            }

            return fields;
        }

        // NOTE: A copy of the config with one constructor argument changed
        public static IComponentConfig With(IComponentConfig config, string name, object? value)
        {
            var type = config.GetType();
            var constructor = PrimaryConstructor(type);

            var arguments = constructor.GetParameters()
                .Select(parameter => string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase)
                    ? value
                    : Property(type, parameter.Name!)?.GetValue(config))
                .ToArray();

            return (IComponentConfig)constructor.Invoke(arguments);
        }

        // NOTE: Every parameter at its default (or the type's default when it has none), for adding a new component
        public static IComponentConfig CreateDefault(Type configType)
        {
            var constructor = PrimaryConstructor(configType);

            var arguments = constructor.GetParameters()
                .Select(parameter => parameter.HasDefaultValue
                    ? parameter.DefaultValue
                    : parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null)
                .ToArray();

            return (IComponentConfig)constructor.Invoke(arguments);
        }

        public static string Format(ConfigField field, object? value) => value switch
        {
            int note when field.Name == "Note" => $"{NoteName(note)}  ({note})",
            NoteDuration duration => DurationName(duration),
            Enum choice => Humanise(choice.ToString()),
            null => "",
            _ => value.ToString() ?? ""
        };

        public static string NoteName(int note) =>
            NOTE_NAMES[((note % 12) + 12) % 12] + (note / 12 - 1);

        public static string DurationName(NoteDuration duration) => duration switch
        {
            NoteDuration.WholeNote => "1/1",
            NoteDuration.HalfNote => "1/2",
            NoteDuration.QuarterNote => "1/4",
            NoteDuration.EighthNote => "1/8",
            NoteDuration.SixteenthNote => "1/16",
            NoteDuration.ThirtySecondNote => "1/32",
            NoteDuration.SixtyFourthNote => "1/64",
            _ => duration.ToString()
        };

        // NOTE: "SixteenthNote" → "Sixteenth note"
        public static string Humanise(string name)
        {
            var builder = new StringBuilder();

            for (var i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]))
                    builder.Append(' ').Append(char.ToLowerInvariant(name[i]));
                else
                    builder.Append(name[i]);
            }

            return builder.ToString();
        }

        // NOTE: The record's primary constructor is the public one with the most parameters
        private static ConstructorInfo PrimaryConstructor(Type type) =>
            type.GetConstructors().OrderByDescending(x => x.GetParameters().Length).First();

        private static PropertyInfo? Property(Type type, string name) =>
            type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
    }
}
