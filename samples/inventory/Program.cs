using SnapCLI;

// Example of simple CLI program 

// this description will be shown on main (root command) help 
[assembly: RootCommand(Description = "Inventory database manager")]

internal class Program
{
    // Here we declare global option
    [Option(Name = "database", Description = "Inventory database file", Aliases = "db", HelpName = "filepath")]
    private static string s_databasePath = "inventory.json";


    [Command(Description = "Add item to the inventory")]
    public static void Add(
        [Argument(Description = "Inventory item name")]
        string item,

        [Argument(Description = "Item quantity to add")]
        int quantity = 1)
    {
        using var inventory = new Inventory(s_databasePath);

        if (!inventory.ContainsKey(item))
            inventory[item] = quantity;
        else
            inventory[item] += quantity;

        Console.WriteLine($"{item}: {inventory[item]}");

    }

    [Command(Description = "Remove item from the inventory")]
    public static int Remove(
        [Option(Description = "Remove all specified items from the inventory")]
        bool all,

        [Argument(Description = "Inventory item name")]
        string item,

        [Argument(Description = "Item quantity to remove")]
        int quantity = 1
        )
    {
        CLI.ParseResult.ValidateMutuallyExclusiveOptionsArguments(["all", "quantity"]);

        using var inventory = new Inventory(s_databasePath);

        if (all)
        {
            inventory[item] = 0;
        }
        else
        {

            if (!inventory.ContainsKey(item) || inventory[item] == 0)
            {
                Console.Error.WriteLine($"Error: There is no {item}(s) in the inventory");
                return 1; // error code
            }

            if (inventory[item] < quantity)
            {
                Console.Error.WriteLine($"Error: Inventory has only {inventory[item]} of {item}");
                return 2; // error code
            }

            inventory[item] -= quantity;
        }

        Console.WriteLine($"{item}: {inventory[item]}");

        return 0; // success
    }

    public enum ListFormat
    {
        Text,
        CSV,
        JSON
    }

    [Command(Description = "List items in the inventory")]
    public static void List(
        [Option(Name = "format", Aliases = "f", Description = "Output format")]
        ListFormat listFormat = ListFormat.Text
        )
    {
        using var inventory = new Inventory(s_databasePath);

        switch (listFormat)
        {
            case ListFormat.Text:
                foreach (var item in inventory)
                    Console.WriteLine($"{item.Key}: {item.Value}");
                if (inventory.Count == 0)
                    Console.WriteLine($"The inventory is empty");
                break;
            case ListFormat.CSV:
                Console.WriteLine($"item,quantity");
                foreach (var item in inventory)
                    Console.WriteLine($"\"{item.Key}\",{item.Value}");
                break;
            case ListFormat.JSON:
                Console.WriteLine(inventory.ToJson());
                break;
        }
    }

    // hidden command will not be shown in help
    [Command(Description = "Dump internal database representation (JSON)", Hidden = true)]
    public static void Dump()
    {
        using var inventory = new Inventory(s_databasePath);
        Console.WriteLine(inventory.ToJson());
    }
}
