using System.Text.Json;
using System.Text.Json.Serialization;

namespace CarDataApp;

internal static class Program
{
    private const string DefaultInputFileName = "car_data.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static int Main(string[] args)
    {
        string inputPath = ResolveInputPath(args);

        if (!File.Exists(inputPath))
        {
            WriteError($"Input file was not found: {inputPath}");
            Console.WriteLine("Usage: dotnet run -- [path-to-car-data.json]");
            return 1;
        }

        try
        {
            IReadOnlyList<Car> cars = LoadCars(inputPath);

            if (cars.Count == 0)
            {
                WriteError("No car records were found in the input file.");
                return 1;
            }

            List<string> validationErrors = ValidateCars(cars).ToList();
            if (validationErrors.Count > 0)
            {
                WriteError("The input file contains invalid car data:");
                validationErrors.ForEach(error => Console.Error.WriteLine($"  - {error}"));
                return 1;
            }

            PrintReport(cars);
            return 0;
        }
        catch (JsonException ex)
        {
            WriteError($"Unable to parse JSON in '{inputPath}'.");
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        catch (IOException ex)
        {
            WriteError($"Unable to read '{inputPath}'.");
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static string ResolveInputPath(IReadOnlyList<string> args)
    {
        if (args.Count > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            return Path.GetFullPath(args[0]);
        }

        string workingDirectoryPath = Path.Combine(Environment.CurrentDirectory, DefaultInputFileName);
        return File.Exists(workingDirectoryPath)
            ? workingDirectoryPath
            : Path.Combine(AppContext.BaseDirectory, DefaultInputFileName);
    }

    private static IReadOnlyList<Car> LoadCars(string inputPath)
    {
        string json = File.ReadAllText(inputPath);
        return JsonSerializer.Deserialize<List<Car>>(json, JsonOptions) ?? [];
    }

    private static IEnumerable<string> ValidateCars(IReadOnlyList<Car> cars)
    {
        for (int index = 0; index < cars.Count; index++)
        {
            Car car = cars[index];
            string label = $"Car #{index + 1}";

            if (string.IsNullOrWhiteSpace(car.ModelName))
            {
                yield return $"{label}: ModelName is required.";
            }

            if (car.Horsepower <= 0)
            {
                yield return $"{label}: HP must be greater than zero.";
            }

            if (car.WeightKilograms <= 0)
            {
                yield return $"{label}: Weight must be greater than zero.";
            }
        }
    }

    private static void PrintReport(IEnumerable<Car> cars)
    {
        Console.WriteLine("Car Performance List");
        Console.WriteLine(new string('-', 64));
        Console.WriteLine($"{"Model",-24} {"HP",6} {"Weight",12} {"Drive",16}");
        Console.WriteLine(new string('-', 64));

        foreach (Car car in cars)
        {
            Console.WriteLine(
                $"{car.ModelName,-24} {car.Horsepower,6} {car.WeightKilograms,9} kg {GetDriveType(car),16}");
        }
    }

    private static string GetDriveType(Car car) =>
        car.IsAllWheelDrive ? "All-Wheel Drive" : "2WD";

    private static void WriteError(string message) =>
        Console.Error.WriteLine($"Error: {message}");
}

internal sealed record Car
{
    public string ModelName { get; init; } = string.Empty;

    [JsonPropertyName("HP")]
    public int Horsepower { get; init; }

    [JsonPropertyName("Weight")]
    public int WeightKilograms { get; init; }

    [JsonPropertyName("IsAWD")]
    public bool IsAllWheelDrive { get; init; }
}
