using System.Collections.Concurrent;
using System.Text;
using FivetranClient;
using FivetranClient.Models;
using Import.Helpers.Fivetran;

namespace Import.ConnectionSupport;

// equivalent of database is group in Fivetran terminology
public class FivetranConnectionSupport : IConnectionSupport
{
    public const string ConnectorTypeCode = "FIVETRAN";
    private record FivetranConnectionDetailsForSelection(string ApiKey, string ApiSecret);

    public object? GetConnectionDetailsForSelection()
    {
        Console.Write("Provide your Fivetran API Key: ");
        var apiKey = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentNullException("API Key cannot be empty.", nameof(apiKey));
        }
        Console.Write("Provide your Fivetran API Secret: ");
        var apiSecret = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(apiSecret))
        {
            throw new ArgumentNullException("API Secret cannot be empty.", nameof(apiSecret));
        }

        return new FivetranConnectionDetailsForSelection(apiKey, apiSecret);
    }

    public object GetConnection(object? connectionDetails, string? selectedToImport)
    {
        if (connectionDetails is not FivetranConnectionDetailsForSelection details)
        {
            throw new ArgumentException("Invalid connection details provided.");
        }

        if (string.IsNullOrWhiteSpace(selectedToImport))
        {
            throw new ArgumentNullException(nameof(selectedToImport), "Selected group ID cannot be null or empty.");
        }

        return new RestApiManagerWrapper(
            new RestApiManager(
                details.ApiKey,
                details.ApiSecret,
                TimeSpan.FromSeconds(40)),
            selectedToImport);
    }

    public void CloseConnection(object? connection)
    {
        switch (connection)
        {
            case RestApiManager restApiManager:
                restApiManager.Dispose();
                break;
            case RestApiManagerWrapper restApiManagerWrapper:
                restApiManagerWrapper.Dispose();
                break;
            default:
                throw new ArgumentException("Invalid connection type provided.");
        }
    }

    public string SelectToImport(object? connectionDetails)
    {
        if (connectionDetails is not FivetranConnectionDetailsForSelection details)
        {
            throw new ArgumentException("Invalid connection details provided.");
        }
        using var restApiManager = new RestApiManager(details.ApiKey, details.ApiSecret, TimeSpan.FromSeconds(40));
        var groups = restApiManager
            .GetGroupsAsync(CancellationToken.None)
            .ToBlockingEnumerable()
            .ToList();
        if (!groups.Any())
        {
            throw new Exception("No groups found in Fivetran account.");
        }

        // bufforing for performance
        var consoleOutputBuffer = new StringBuilder("Available groups in Fivetran account:\n");
        var elementIndex = 1;
        foreach (var group in groups)
        {
            consoleOutputBuffer.AppendLine($"{elementIndex++}. {group.Name} (ID: {group.Id})\n");
        }
        consoleOutputBuffer.Append("Please select a group to import from (by number): ");
        Console.Write(consoleOutputBuffer.ToString());
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input)
            || !int.TryParse(input, out var selectedIndex)
            || selectedIndex < 1
            || selectedIndex > groups.Count)
        {
            throw new ArgumentException("Invalid group selection.");
        }

        var selectedGroup = groups.ElementAt(selectedIndex - 1);
        return selectedGroup.Id;
    }

    public async Task RunImportAsync(object? connection)
    {
        if (connection is not RestApiManagerWrapper restApiManagerWrapper)
        {
            throw new ArgumentException("Invalid connection type provided.");
        }

        var restApiManager = restApiManagerWrapper.RestApiManager;
        var groupId = restApiManagerWrapper.GroupId;

        var connectors = new List<Connector>();

        await foreach (var connector in restApiManager.GetConnectorsAsync(groupId, CancellationToken.None).WithCancellation(CancellationToken.None))
        {
            connectors.Add(connector);
        }

        if (!connectors.Any())
        {
            throw new Exception("No connectors found in the selected group.");
        }

        var mappingsBuffer = new ConcurrentBag<string>();

        await Parallel.ForEachAsync(connectors, async (connector, ct) =>
        {
            var connectorSchemas = await restApiManager
                .GetConnectorSchemasAsync(connector.Id, ct);

            if (connectorSchemas?.Schemas == null)
            {
                return;
            }


            foreach (var schema in connectorSchemas.Schemas)
            {
                if (schema.Value?.Tables == null || !schema.Value.Tables.Any())
                {
                    continue;
                }

                foreach (var table in schema.Value.Tables)
                {
                    mappingsBuffer.Add($"  {connector.Id}: {schema.Key}.{table.Key} -> {schema.Value?.NameInDestination}.{table.Value.NameInDestination}\n");
                }
            }
        });


        var allMappingsBuffer = new StringBuilder("Lineage mappings:\n");

        foreach (var mapping in mappingsBuffer)
        {
            allMappingsBuffer.AppendLine(mapping);
        }

        Console.WriteLine(allMappingsBuffer.ToString());
    }

    public void RunImport(object? connection)
    {
        RunImportAsync(connection).GetAwaiter().GetResult();
    }
}