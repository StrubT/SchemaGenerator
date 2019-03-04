using System;
using System.Text.RegularExpressions;

namespace StrubT.SchemaGenerator {

	public struct SchemaValue {

		static readonly string Iso8601DatePattern = @"(([+-]\d+)?\d{4}((-\d{2}){0,2}|(\d{2}){0,2}|(-W\d{2}(-\d)?)|(W\d{2}(\d)?)|(-?\d{3}))|--(\d{2}-?\d{2}))";
		static readonly string Iso8601TimePattern = @"(\d{2}((((:\d{2}){2}|(\d{2}){2})(\.\d{3})?)|(:\d{2})?|(\d{2})?)(Z|[+-]\d{2}(:?\d{2})?)?)";
		static readonly string Iso8601DateTimePattern = $"({Iso8601DatePattern}|{Iso8601TimePattern}|{Iso8601DatePattern}T{Iso8601TimePattern})";
		static readonly string Iso8601DurationPattern = $@"(P((?=.)((\d+Y)?(\d+M)?(\d+D)?|{Iso8601DatePattern})(T(?=.)((\d+H)?(\d+M)?(\d+S)?|{Iso8601TimePattern}))?|\d+W))";
		static readonly string Iso8601IntervalPattern = $@"((R\d*/)?({Iso8601DateTimePattern}/{Iso8601DateTimePattern}|{Iso8601DateTimePattern}/{Iso8601DurationPattern}|{Iso8601DurationPattern}/{Iso8601DateTimePattern}|{Iso8601DurationPattern}))";
		static readonly Regex Iso8601Pattern = new Regex($"^({Iso8601DateTimePattern}|{Iso8601IntervalPattern})$", RegexOptions.Compiled);

		//static readonly string[] DateTimeFormats = new[] { "" }; // TODO
		//static readonly string[] TimeSpanFormats = new[] { "" }; // TODO

		public ContentType Type { get; set; }

		public bool? BooleanValue { get; set; }

		public string StringValue { get; set; }

		public decimal? NumericValue { get; set; }

		public DateTime? DateTimeValue { get; set; }

		public TimeSpan? TimeSpanValue { get; set; }

		public int? ArrayLength { get; set; }

		internal static SchemaValue ParseValue(string value) {

			if (string.IsNullOrWhiteSpace(value))
				return new SchemaValue { Type = ContentType.Empty };

			if ("true".Equals(value, StringComparison.InvariantCultureIgnoreCase))
				return new SchemaValue { Type = ContentType.Boolean, BooleanValue = true };
			if ("false".Equals(value, StringComparison.InvariantCultureIgnoreCase))
				return new SchemaValue { Type = ContentType.Boolean, BooleanValue = false };

			if (decimal.TryParse(value, out var @decimal))
				return new SchemaValue { Type = @decimal % 1 == 0 ? ContentType.NumericInteger : ContentType.NumericDecimal, NumericValue = @decimal };

			//if (DateTime.TryParseExact(value, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
			//	return new SchemaValue { Type = ContentType.DateTime, DateTimeValue = dateTime };
			//if (TimeSpan.TryParseExact(value, TimeSpanFormats, CultureInfo.InvariantCulture, TimeSpanStyles.None, out var timeSpan))
			//	return new SchemaValue { Type = ContentType.TimeSpan, TimeSpanValue = timeSpan };
			if (Iso8601Pattern.IsMatch(value))
				return new SchemaValue { Type = ContentType.DateTime };

			return new SchemaValue { Type = ContentType.Default, StringValue = value };
		}
	}

	public enum NodeType { Root, Child, Attribute }

	public enum ContentType {

		String, Default = String,
		NumericInteger, NumericDecimal,
		Boolean,
		DateTime, TimeSpan,

		Object, Array,
		Empty,
		Root
	}
}
