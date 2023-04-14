using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace OnlineLibraryManagementSystem.Models
{
    public class DateConverter : JsonConverter<DateTime>
    {
        //private string formatDate = "dd/MM/yyyy";
        private string formatDate = "yyyy-MM-dd";
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException();
            }

            string? dateString = reader.GetString();
            if (dateString == null)
            {
                throw new JsonException("Date string cannot be null.");
            }

            return DateTime.ParseExact(dateString, formatDate, CultureInfo.InvariantCulture);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(formatDate));
        }
    }
}
