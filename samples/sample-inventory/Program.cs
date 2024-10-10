using SnapCLI;

// Example of simple CLI program 

// this description will be shown on main (root command) help 
[assembly: RootCommand("Inventory database manager")]

internal class Program
{
    // Here we declare global option
    [Option(name:"database", description:"Inventory database file", aliases:"db", helpName:"filepath")]
    private static string s_databasePath = "inventory.json";
    

    [Command(description:"Add item to the inventory")]
    public static void Add(
        [Argument(description:"Inventory item name")] 
        string item, 

        [Argument(description:"Item quantity to add")] 
        int quantity = 1)
    {
        using var inventory = new Inventory(s_databasePath);

        if (!inventory.ContainsKey(item))
            inventory[item] = quantity;
        else
            inventory[item] += quantity;

        Console.WriteLine($"{item}: {inventory[item]}");

    }

    [Command(description: "Remove item from the inventory")]
    public static int Remove(
        [Option(description:"Remove all specified items from the inventory")]
        bool all,

        [Argument(description:"Inventory item name")]
        string item,

        [Argument(description:"Item quantity to remove")]
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

    [Command(description: "List items in the inventory")]
    public static void List(
        [Option(name:"format", aliases:"f", description:"Output format")]
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
    [Command(description: "Dump internal database representation (JSON)", hidden: true)]
    public static void Dump()
    {
        using var inventory = new Inventory(s_databasePath);
        Console.WriteLine(inventory.ToJson());
    }
}
