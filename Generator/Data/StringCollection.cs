using System.Text;

namespace StringsGenerator.Data;

internal class StringCollection
{
    public string[] Strings { get; private set; }
    public byte[][] StringPool { get; private set; }
    public int Length { get; private set; }

    public StringCollection GetStrings()
    {
        Strings = Collection;

        return this;
    }

    public StringCollection ConvertToBytePool()
    {
        StringPool = [.. Strings.Select(s => new UTF8Encoding(false).GetBytes(s))];
        Length = StringPool.Length;

        return this;
    }

    public StringCollection Build()
    {
        return this;
    }

    private static readonly string[] Collection =
    [
        "Apple",
        "Banana is yellow",
        "Cherry is the best",
        "Orange fresh juice",
        "Grapes are sweet",
        "Mango tropical fruit",
        "Pineapple upside down",
        "Strawberry field",
        "Blueberry muffin",
        "Blackberry pie",
        "Watermelon slice",
        "Melon honey dew",
        "Peach blossom",
        "Apricot jam",
        "Plum tart",
        "Pear tree",
        "Kiwi fruit",
        "Lemon sour taste",
        "Lime fresh",
        "Coconut milk",
        "Avocado toast",
        "Pomegranate seeds",
        "Fig tree",
        "Date palm",
        "Raspberry cake",
        "Cranberry sauce",
        "Currant red",
        "Gooseberry jam",
        "Mulberry bush",
        "Papaya salad",
        "Guava juice",
        "Passion fruit",
        "Dragon fruit",
        "Lychee sweet",
        "Durian smell",
        "Jackfruit curry",
        "Starfruit shape",
        "Persimmon ripe",
        "Tangerine peel",
        "Mandarin orange",
        "Nectarine smooth",
        "Olive oil",
        "Tomato soup",
        "Cucumber fresh",
        "Carrot crunch",
        "Potato chips",
        "Onion rings",
        "Garlic bread",
        "Pepper hot",
        "Chili spice",
        "Spinach leaf",
        "Lettuce salad",
        "Broccoli green",
        "Cauliflower rice",
        "Cabbage roll",
        "Radish pickled",
        "Beetroot juice",
        "Zucchini grill",
        "Eggplant parm",
        "Pumpkin pie",
        "Corn cob",
        "Peanut butter",
        "Almond milk",
        "Cashew nut",
        "Walnut cake",
        "Hazelnut spread",
        "Pistachio icecream",
        "Macadamia cookie",
        "Chestnut roast",
        "Coffee bean",
        "Cocoa powder",
        "Sugar cane",
        "Salt sea",
        "Rice bowl",
        "Wheat bread",
        "Barley tea",
        "Oatmeal porridge",
        "Rye bread",
        "Buckwheat noodles",
        "Quinoa salad",
        "Millet grain",
        "Sorghum beer",
        "Soy sauce",
        "Tofu cube",
        "Tempeh slice",
        "Seitan steak",
        "Cheese wheel",
        "Milk shake",
        "Yogurt plain",
        "Butter spread",
        "Cream cheese",
        "Ice cream",
        "Honey jar",
        "Jam spread",
        "Jelly candy",
        "Syrup maple"
    ];
}
