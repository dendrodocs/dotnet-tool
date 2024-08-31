using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Reflection;

namespace DendroDocs.Tool;

public class JsonValidator
{
    private const string SchemaResource = "DendroDocs.Tool.schema.json";

    public static bool ValidateJson(ref string jsonContent, out IList<string> validationErrors)
    {
        validationErrors = [];

        try
        {
            var schema = JSchema.Parse(LoadSchema());
            var json = JToken.Parse(jsonContent);

            if (json.IsValid(schema, out validationErrors))
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            validationErrors.Add($"Exception during validation: {ex.Message}");
        }

        return false;
    }

    private static string LoadSchema()
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(SchemaResource)!;
        using StreamReader reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
