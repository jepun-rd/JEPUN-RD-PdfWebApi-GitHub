using System.Dynamic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jepun.PDF.WebApi.Common
{
	public static class ServiceHelper
	{
		public static T JsonToObj<T>(this string jsonString) //where T : class
		{
			return JsonSerializer.Deserialize<T>(jsonString);
		}
		public static string ObjToJson(this object obj, bool ignoreNullValues = false)
		{
			string jsonString = "";
			JsonSerializerOptions options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true,  //不區分大小寫的屬性比對
				Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,//它不會對 HTML 敏感的字元（例如、、和）進行換用 <> 。 '&
				DefaultIgnoreCondition = ignoreNullValues ? JsonIgnoreCondition.WhenWritingNull : JsonIgnoreCondition.Never
			};
			jsonString = JsonSerializer.Serialize(obj, options);
			return jsonString;
		}
		
	}

}
