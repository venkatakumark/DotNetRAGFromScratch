using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyLlmApp.Data;
using MyLlmApp.Models;
using OpenAI.Chat;

public class ToolExecutor
{
    
    // ==========================================
    // Execute Tool
    // ==========================================

    public async Task<string> ExecuteAsync(
        ChatToolCall toolCall)
    {
        // --------------------------------------
        // 1. Validate tool name
        // --------------------------------------

        if (string.IsNullOrWhiteSpace(
            toolCall.FunctionName))
        {
            return "Tool name is missing.";
        }

        // --------------------------------------
        // 2. Validate arguments exist
        // --------------------------------------

        if (string.IsNullOrWhiteSpace(
            toolCall.FunctionArguments.GetType().ToString()))
        {
            return
                $"No arguments were provided for " +
                $"tool '{toolCall.FunctionName}'.";
        }

        // --------------------------------------
        // 3. Parse JSON
        // --------------------------------------

        JsonDocument arguments;

        try
        {
            arguments =
                JsonDocument.Parse(
                    toolCall.FunctionArguments);
        }
        catch (JsonException)
        {
            return
                $"Invalid JSON arguments for " +
                $"tool '{toolCall.FunctionName}'.";
        }

        using (arguments)
        {
            // ----------------------------------
            // 4. Tool allowlist
            // ----------------------------------

            switch (toolCall.FunctionName)
            {
                case "GetWeather":

                    return await ExecuteWeather(
                        arguments.RootElement);

                case "GetCurrentTime":

                    return ExecuteCurrentTime(
                        arguments.RootElement);

                case "GetEmployee":

                    return await ExecuteGetEmployee(
                         arguments.RootElement);        

                default:

                    return
                        $"Tool '{toolCall.FunctionName}' " +
                        $"is not allowed.";
            }
        }
    }

    // ==========================================
    // Weather
    // ==========================================

    private static async Task<string>
        ExecuteWeather(
            JsonElement arguments)
    {
        // --------------------------------------
        // Validate city
        // --------------------------------------

        if (!arguments.TryGetProperty(
                "city",
                out JsonElement cityElement))
        {
            return
                "Missing required argument: city.";
        }

        if (cityElement.ValueKind !=
            JsonValueKind.String)
        {
            return
                "Argument 'city' must be a string.";
        }

        string? city =
            cityElement.GetString();

        if (string.IsNullOrWhiteSpace(city))
        {
            return
                "Argument 'city' cannot be empty.";
        }

        // --------------------------------------
        // Length validation
        // --------------------------------------

        if (city.Length > 100)
        {
            return
                "Argument 'city' is too long.";
        }

        Console.WriteLine(
            $"Executing weather for: {city}");

        // --------------------------------------
        // Call real API
        // --------------------------------------

        using HttpClient httpClient = new();

        string geocodingUrl =
            $"https://geocoding-api.open-meteo.com/v1/search" +
            $"?name={Uri.EscapeDataString(city)}" +
            $"&count=1&language=en&format=json";

        string geocodingJson =
            await httpClient.GetStringAsync(
                geocodingUrl);

        using JsonDocument geocodingDocument =
            JsonDocument.Parse(
                geocodingJson);

        // --------------------------------------
        // Check API response
        // --------------------------------------

        if (!geocodingDocument.RootElement
            .TryGetProperty(
                "results",
                out JsonElement results))
        {
            return
                $"Could not find location '{city}'.";
        }

        if (results.GetArrayLength() == 0)
        {
            return
                $"Could not find location '{city}'.";
        }

        JsonElement location =
            results[0];

        double latitude =
            location
                .GetProperty("latitude")
                .GetDouble();

        double longitude =
            location
                .GetProperty("longitude")
                .GetDouble();

        string locationName =
            location
                .GetProperty("name")
                .GetString()
                ?? city;

        // --------------------------------------
        // Weather API
        // --------------------------------------

        string weatherUrl =
            $"https://api.open-meteo.com/v1/forecast" +
            $"?latitude={latitude}" +
            $"&longitude={longitude}" +
            $"&current=temperature_2m,weather_code" +
            $"&timezone=auto";

        string weatherJson =
            await httpClient.GetStringAsync(
                weatherUrl);

        using JsonDocument weatherDocument =
            JsonDocument.Parse(weatherJson);

        if (!weatherDocument.RootElement
            .TryGetProperty(
                "current",
                out JsonElement current))
        {
            return
                "Weather API did not return current data.";
        }

        double temperature =
            current
                .GetProperty("temperature_2m")
                .GetDouble();

        int weatherCode =
            current
                .GetProperty("weather_code")
                .GetInt32();

        string description =
            weatherCode switch
            {
                0 => "Clear sky",
                1 or 2 or 3 =>
                    "Partly cloudy",

                45 or 48 =>
                    "Foggy",

                51 or 53 or 55 =>
                    "Drizzle",

                61 or 63 or 65 =>
                    "Rain",

                71 or 73 or 75 =>
                    "Snow",

                80 or 81 or 82 =>
                    "Rain showers",

                95 =>
                    "Thunderstorm",

                96 or 99 =>
                    "Thunderstorm with hail",

                _ =>
                    "Unknown weather conditions"
            };

        return
            $"{locationName}: " +
            $"{temperature}°C, " +
            $"{description}";
    }

    // ==========================================
    // Current Time
    // ==========================================

    private static string ExecuteCurrentTime(
        JsonElement arguments)
    {
        // --------------------------------------
        // Validate city
        // --------------------------------------

        if (!arguments.TryGetProperty(
                "city",
                out JsonElement cityElement))
        {
            return
                "Missing required argument: city.";
        }

        if (cityElement.ValueKind !=
            JsonValueKind.String)
        {
            return
                "Argument 'city' must be a string.";
        }

        string? city =
            cityElement.GetString();

        if (string.IsNullOrWhiteSpace(city))
        {
            return
                "Argument 'city' cannot be empty.";
        }

        if (city.Length > 100)
        {
            return
                "Argument 'city' is too long.";
        }

        // --------------------------------------
        // Resolve timezone
        // --------------------------------------

        TimeZoneInfo timeZone;

        try
        {
            timeZone =
                city.ToLowerInvariant() switch
                {
                    "hyderabad" or
                    "bangalore" or
                    "bengaluru" or
                    "mumbai" or
                    "delhi" =>
                        TimeZoneInfo
                            .FindSystemTimeZoneById(
                                "India Standard Time"),

                    "london" =>
                        TimeZoneInfo
                            .FindSystemTimeZoneById(
                                "GMT Standard Time"),

                    "new york" =>
                        TimeZoneInfo
                            .FindSystemTimeZoneById(
                                "Eastern Standard Time"),

                    "tokyo" =>
                        TimeZoneInfo
                            .FindSystemTimeZoneById(
                                "Tokyo Standard Time"),

                    _ =>
                        throw new ArgumentException(
                            $"Time zone not configured " +
                            $"for '{city}'.")
                };
        }
        catch (TimeZoneNotFoundException)
        {
            return
                $"Time zone information is not " +
                $"available for '{city}'.";
        }

        // --------------------------------------
        // Get time
        // --------------------------------------

        DateTime localTime =
            TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                timeZone);

        return
            $"{city}: " +
            $"{localTime:yyyy-MM-dd HH:mm:ss}";
    }

    private static async Task<string> ExecuteGetEmployee(
        JsonElement arguments)
    {
        if (!arguments.TryGetProperty(
            "employeeId",
            out JsonElement employeeIdElement))
        {
        return
            "Missing required argument: employeeId.";
        }

        if (employeeIdElement.ValueKind !=
        JsonValueKind.Number)
        {
        return
            "Argument 'employeeId' must be a number.";
        }

        int employeeId =
        employeeIdElement.GetInt32();

        if (employeeId <= 0)
        {
        return
            "Employee ID must be greater than zero.";
        }

        using AppDbContext db = new();

        Employee? employee =
        await db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.Id == employeeId);

        if (employee == null)
        {
        return
            $"Employee {employeeId} was not found.";
        }

        return
        $"Employee ID: {employee.Id}\n" +
        $"Name: {employee.Name}\n" +
        $"Department: {employee.Department}\n" +
        $"Role: {employee.Role}\n" +
        $"Location: {employee.Location}";
    }
}