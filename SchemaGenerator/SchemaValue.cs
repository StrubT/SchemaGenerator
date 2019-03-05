using System;
using System.Globalization;

namespace StrubT.SchemaGenerator {

	public struct SchemaValue {

		static readonly string[] DateTimeFormats = new[] {
			"o", // yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffK
			"yyyy'-'MM'-'dd'T'HH':'mmK",
			"yyyy'-'MM'-'dd' 'HH':'mm",
			"yyyy'-'MM'-'dd",
			"r" }; // ddd, dd MMM yyyy HH':'mm':'ss 'GMT'

		static readonly string[] TimeSpanFormats = new[] {
			"c", // [-][d'.']hh':'mm':'ss['.'fffffff]
			"[-][d'.']hh':'mm" };

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

			if (DateTime.TryParseExact(value, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.NoCurrentDateDefault | DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dateTime))
				return new SchemaValue { Type = ContentType.DateTime, DateTimeValue = dateTime };
			if (TimeSpan.TryParseExact(value, TimeSpanFormats, CultureInfo.InvariantCulture, out var timeSpan))
				return new SchemaValue { Type = ContentType.TimeSpan, TimeSpanValue = timeSpan };

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
