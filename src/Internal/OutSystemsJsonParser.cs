using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OutSystems.EmbeddingService.Internal;

public static class OutSystemsJsonParser
{
    /// <summary>
    /// Mengecek apakah payload JSON memiliki struktur khas eSpace / Module OutSystems.
    /// </summary>
    public static bool IsOutSystemsJson(string jsonText)
    {
        if (string.IsNullOrWhiteSpace(jsonText)) return false;

        try
        {
            using var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return false;

            return root.TryGetProperty("ModuleType", out _) ||
                   root.TryGetProperty("UserProviderEspace", out _) ||
                   root.TryGetProperty("Actions", out _) ||
                   root.TryGetProperty("Structures", out _) ||
                   root.TryGetProperty("Entities", out _) ||
                   root.TryGetProperty("ServiceAPIs", out _) ||
                   root.TryGetProperty("Processes", out _);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Mengekstrak dan memformat setiap elemen OutSystems menjadi list chunk semantik Markdown.
    /// </summary>
    public static List<(string DocumentSuffix, string FormattedText, string ElementType)> ParseToSemanticChunks(string jsonText)
    {
        var result = new List<(string DocumentSuffix, string FormattedText, string ElementType)>();
        if (string.IsNullOrWhiteSpace(jsonText)) return result;

        JsonNode? rootNode;
        try
        {
            rootNode = JsonNode.Parse(jsonText);
        }
        catch
        {
            return result;
        }

        if (rootNode is not JsonObject root) return result;

        string moduleName = root["Name"]?.ToString() ?? "UnknownModule";
        string moduleType = root["ModuleType"]?.ToString() ?? "Module";

        // 1. Parse Server Actions / Actions
        if (root["Actions"]?["Action"] is JsonArray actions)
        {
            foreach (var act in actions)
            {
                if (act == null) continue;
                string actionName = act["Name"]?.ToString() ?? "UnnamedAction";
                string isPublic = act["Public"]?.ToString() ?? "No";
                string description = act["Description"]?.ToString() ?? string.Empty;
                string folder = act["Folder"]?.ToString() ?? string.Empty;

                var sb = new StringBuilder();
                sb.AppendLine("[OutSystems Server Action]");
                sb.AppendLine($"Module: {moduleName} ({moduleType})");
                sb.AppendLine($"Action Name: {actionName}");
                sb.AppendLine($"Is Public: {isPublic}");
                if (!string.IsNullOrWhiteSpace(folder))
                    sb.AppendLine($"Folder: {folder}");
                if (!string.IsNullOrWhiteSpace(description))
                    sb.AppendLine($"Description: {description}");

                // Input Parameters
                if (act["InputParameters"]?["InputParameter"] is JsonArray inParams && inParams.Count > 0)
                {
                    sb.AppendLine("\nInput Parameters:");
                    foreach (var p in inParams)
                    {
                        if (p == null) continue;
                        string pName = p["Name"]?.ToString() ?? "";
                        string pType = p["DataType"]?.ToString() ?? "Text";
                        string pMandatory = p["IsMandatory"]?.ToString() ?? "No";
                        string pDesc = p["Description"]?.ToString() ?? "";
                        sb.AppendLine($"- {pName} (DataType: {pType}, Mandatory: {pMandatory})" +
                                      (!string.IsNullOrWhiteSpace(pDesc) ? $" : {pDesc}" : ""));
                    }
                }

                // Output Parameters
                if (act["OutputParameters"]?["OutputParameter"] is JsonArray outParams && outParams.Count > 0)
                {
                    sb.AppendLine("\nOutput Parameters:");
                    foreach (var p in outParams)
                    {
                        if (p == null) continue;
                        string pName = p["Name"]?.ToString() ?? "";
                        string pType = p["DataType"]?.ToString() ?? "Text";
                        string pDesc = p["Description"]?.ToString() ?? "";
                        sb.AppendLine($"- {pName} (DataType: {pType})" +
                                      (!string.IsNullOrWhiteSpace(pDesc) ? $" : {pDesc}" : ""));
                    }
                }

                result.Add(($"action-{actionName}", sb.ToString().Trim(), "ServerAction"));
            }
        }

        // 2. Parse Entities (Database Tables)
        if (root["Entities"]?["Entity"] is JsonArray entities)
        {
            foreach (var ent in entities)
            {
                if (ent == null) continue;
                string entityName = ent["Name"]?.ToString() ?? "UnnamedEntity";
                string isPublic = ent["Public"]?.ToString() ?? "No";
                string description = ent["Description"]?.ToString() ?? string.Empty;

                var sb = new StringBuilder();
                sb.AppendLine("[OutSystems Database Entity]");
                sb.AppendLine($"Module: {moduleName}");
                sb.AppendLine($"Entity Name: {entityName}");
                sb.AppendLine($"Is Public: {isPublic}");
                if (!string.IsNullOrWhiteSpace(description))
                    sb.AppendLine($"Description: {description}");

                if (ent["Attributes"]?["Attribute"] is JsonArray attrs && attrs.Count > 0)
                {
                    sb.AppendLine("\nAttributes / Columns:");
                    foreach (var a in attrs)
                    {
                        if (a == null) continue;
                        string aName = a["Name"]?.ToString() ?? "";
                        string aType = a["DataType"]?.ToString() ?? "Text";
                        string isMandatory = a["IsMandatory"]?.ToString() ?? "No";
                        string isIdentifier = a["IsIdentifier"]?.ToString() ?? "No";
                        sb.AppendLine($"- {aName} (DataType: {aType}, IsId: {isIdentifier}, Mandatory: {isMandatory})");
                    }
                }

                result.Add(($"entity-{entityName}", sb.ToString().Trim(), "Entity"));
            }
        }

        // 3. Parse Structures (Data Contracts / DTOs)
        if (root["Structures"]?["Structure"] is JsonArray structures)
        {
            foreach (var st in structures)
            {
                if (st == null) continue;
                string structName = st["Name"]?.ToString() ?? "UnnamedStructure";
                string isPublic = st["Public"]?.ToString() ?? "No";
                string description = st["Description"]?.ToString() ?? string.Empty;

                var sb = new StringBuilder();
                sb.AppendLine("[OutSystems Structure / DTO]");
                sb.AppendLine($"Module: {moduleName}");
                sb.AppendLine($"Structure Name: {structName}");
                sb.AppendLine($"Is Public: {isPublic}");
                if (!string.IsNullOrWhiteSpace(description))
                    sb.AppendLine($"Description: {description}");

                if (st["Attributes"]?["Attribute"] is JsonArray attrs && attrs.Count > 0)
                {
                    sb.AppendLine("\nFields / Attributes:");
                    foreach (var a in attrs)
                    {
                        if (a == null) continue;
                        string aName = a["Name"]?.ToString() ?? "";
                        string aType = a["DataType"]?.ToString() ?? "Text";
                        string aDesc = a["Description"]?.ToString() ?? "";
                        sb.AppendLine($"- {aName} (DataType: {aType})" +
                                      (!string.IsNullOrWhiteSpace(aDesc) ? $" : {aDesc}" : ""));
                    }
                }

                result.Add(($"structure-{structName}", sb.ToString().Trim(), "Structure"));
            }
        }

        // 4. Parse Service APIs & Service Actions
        var serviceActionArray = root["ServiceAPIMethods"]?["ServiceAction"] as JsonArray
                                 ?? root["ServiceAPIs"]?["ServiceAPI"] as JsonArray
                                 ?? root["ServiceActions"]?["ServiceAction"] as JsonArray;

        if (serviceActionArray != null)
        {
            foreach (var api in serviceActionArray)
            {
                if (api == null) continue;
                string apiName = api["Name"]?.ToString() ?? "UnnamedAPI";
                string description = api["Description"]?.ToString() ?? string.Empty;
                string isPublic = api["Public"]?.ToString() ?? "Yes";

                var sb = new StringBuilder();
                sb.AppendLine("[OutSystems Service Action / API]");
                sb.AppendLine($"Module: {moduleName}");
                sb.AppendLine($"API Name: {apiName}");
                sb.AppendLine($"Is Public: {isPublic}");
                if (!string.IsNullOrWhiteSpace(description))
                    sb.AppendLine($"Description: {description}");

                // Input Parameters
                if (api["InputParameters"]?["InputParameter"] is JsonArray inParams && inParams.Count > 0)
                {
                    sb.AppendLine("\nInput Parameters:");
                    foreach (var p in inParams)
                    {
                        if (p == null) continue;
                        string pName = p["Name"]?.ToString() ?? "";
                        string pType = p["DataType"]?.ToString() ?? "Text";
                        string pMandatory = p["IsMandatory"]?.ToString() ?? "No";
                        string pDesc = p["Description"]?.ToString() ?? "";
                        sb.AppendLine($"- {pName} (DataType: {pType}, Mandatory: {pMandatory})" +
                                      (!string.IsNullOrWhiteSpace(pDesc) ? $" : {pDesc}" : ""));
                    }
                }

                // Output Parameters
                if (api["OutputParameters"]?["OutputParameter"] is JsonArray outParams && outParams.Count > 0)
                {
                    sb.AppendLine("\nOutput Parameters:");
                    foreach (var p in outParams)
                    {
                        if (p == null) continue;
                        string pName = p["Name"]?.ToString() ?? "";
                        string pType = p["DataType"]?.ToString() ?? "Text";
                        string pDesc = p["Description"]?.ToString() ?? "";
                        sb.AppendLine($"- {pName} (DataType: {pType})" +
                                      (!string.IsNullOrWhiteSpace(pDesc) ? $" : {pDesc}" : ""));
                    }
                }

                result.Add(($"serviceapi-{apiName}", sb.ToString().Trim(), "ServiceAPI"));
            }
        }

        // 5. Parse Processes / BPT
        if (root["Processes"]?["Process"] is JsonArray processes)
        {
            foreach (var proc in processes)
            {
                if (proc == null) continue;
                string procName = proc["Name"]?.ToString() ?? "UnnamedProcess";
                string label = proc["Label"]?.ToString() ?? string.Empty;

                var sb = new StringBuilder();
                sb.AppendLine("[OutSystems BPT Process]");
                sb.AppendLine($"Module: {moduleName}");
                sb.AppendLine($"Process Name: {procName}");
                if (!string.IsNullOrWhiteSpace(label))
                    sb.AppendLine($"Label: {label}");

                if (proc["InputParameters"]?["InputParameter"] is JsonArray inParams && inParams.Count > 0)
                {
                    sb.AppendLine("\nInput Parameters:");
                    foreach (var p in inParams)
                    {
                        if (p == null) continue;
                        string pName = p["Name"]?.ToString() ?? "";
                        string pType = p["DataType"]?.ToString() ?? "Text";
                        sb.AppendLine($"- {pName} (DataType: {pType})");
                    }
                }

                result.Add(($"process-{procName}", sb.ToString().Trim(), "Process"));
            }
        }

        return result;
    }
}
