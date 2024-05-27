using System.Globalization;
using System.Text;

internal class Program
{
    private static void Main(string[] args)
    {
        var promotions = new List<Promotion>()
        {
            new PromotionNone(1),
            new PromotionCheapestDiscount(2, 0.3),
            new PromotionCheapestDiscount(3, 0.55),
            new PromotionCheapestDiscount(4, 0.8),
            new PromotionCheapestFixedPrice(5, 1)
        };
        promotions.Sort((x, y) => x.CompareTo(y));

        var products = new List<Product> {
            new("Pralka", 3397),
            new("Suszarka", 4291),
            new("Piekarnik", 4469),
            new("Indukcja", 1789),
            new("Okap", 1239),
            new("Zmywarka", 3349),
            new("Lodówka", 3499),
            new("Mikrofala", 2290),
            // new("Telewizor", 8000),
        };
        
        var promotionsWithTotalPrice = GetBestSortedPromotions(products, promotions);
        
        Console.WriteLine($"Total permutations {promotionsWithTotalPrice.Count}");
        foreach (var p in promotionsWithTotalPrice)
        {
            Console.WriteLine($"Permutation [{string.Join(",", p.Item1.Select(x => x.GroupSize))}], total price - {p.Item3:0.##}");
            var formattedPrices = string.Join(" ", p.Item2.Select(np => $"[{string.Join("; ", np.Select(x => x.ToString("0.##")))}]"));
            Console.WriteLine($"{formattedPrices}");
            Console.WriteLine();
        }
    }

    private static List<(List<Promotion>, List<List<double>>, double)> GetBestSortedPromotions(List<Product> products, List<Promotion> promotions)
    {
        // Clone array products and promotions
        products = new List<Product>(products);
        promotions = new List<Promotion>(promotions);
        products.Sort((x, y) => x.Price.CompareTo(y.Price));

        var combinations = new List<PromotionCombination>();
        FindAllSortedCombinations(ref combinations, promotions, products.Count);

        // Only combinations with all used promotions
        combinations = combinations.Where(x => x.Promotions.Count(y => y == promotions[0]) < promotions[1].GroupSize).ToList();
        // Check first for biggest promotions
        combinations.Reverse();

        var promotionsWithTotalPrice = new List<(List<Promotion>, List<List<double>>, double)>();

        foreach (var c in combinations)
        {
            var promotionsPermutations = PermuteWithoutRepetitions(c.Promotions);
            foreach (var promotionPermutation in promotionsPermutations)
            {
                var productsTaken = 0;
                var newPricesList = new List<List<double>>();
                var totalPrice = 0.0;
                foreach (var promotion in promotionPermutation)
                {
                    var productsGroup = products.Skip(productsTaken).Take(promotion.GroupSize);
                    productsTaken += promotion.GroupSize;
                    var newPrices = promotion.CalculateForAscendingPrices(productsGroup).ToList();
                    newPricesList.Add(newPrices);
                    totalPrice += newPrices.Sum();
                }
                promotionsWithTotalPrice.Add((promotionPermutation, newPricesList, totalPrice));
            }
        }

        Console.WriteLine(string.Join(" ", products.Select(x => x.Name.PadRight(10))));
        Console.WriteLine(string.Join(" ", products.Select(x => x.Price.ToString(CultureInfo.InvariantCulture).PadRight(10))));
        promotionsWithTotalPrice.Sort((x, y) => x.Item3.CompareTo(y.Item3));
        return promotionsWithTotalPrice;
    }

    private static void FindAllSortedCombinations(ref List<PromotionCombination> combinations, List<Promotion> promotions, int products)
    {
        var tmpCombination = new PromotionCombination();
        FindAllSortedCombinationsInternal(ref combinations, promotions, products, ref tmpCombination, 0);
    }

    private static void FindAllSortedCombinationsInternal(ref List<PromotionCombination> combinations, List<Promotion> promotions, int products, ref PromotionCombination c, int minPromotionIndex)
    {
        for (var i = minPromotionIndex; i < promotions.Count; i++)
        {
            try
            {

                var promotion = promotions[i];
                c.Add(promotion);

                if (c.Sum > products)
                    return;
                else if (c.Sum == products)
                {
                    combinations.Add(c.Copy());
                    return;
                }

                FindAllSortedCombinationsInternal(ref combinations, promotions, products, ref c, i);
            }
            finally
            {
                c.RemoveLast();
            }
        }
    }

    private static List<List<T>> PermuteWithoutRepetitions<T>(List<T> list)
        where T : class, IComparable<T>
    {
        var permutations = new List<List<T>>();
        PermuteWithoutRepetitionsInternal(permutations, list, list.Count - 1, 0);
        return permutations;
    }

    private static void PermuteWithoutRepetitionsInternal<T>(List<List<T>> permutations, List<T> list, int end, int start)
        where T : class, IComparable<T>
    {
        permutations.Add(new List<T>(list));

        for (int left = end - 1; left >= start; left--)
        {
            for (int right = left + 1; right <= end; right++)
            {
                if (list[left].CompareTo(list[right]) != 0)
                {
                    Swap(ref list, left, right);
                    PermuteWithoutRepetitionsInternal(permutations, list, end, left + 1);
                }
            }

            var firstElement = list[left];
            for (int i = left; i <= end - 1; i++)
                list[i] = list[i + 1];

            list[end] = firstElement;
        }
    }

    static bool Swap<T>(ref List<T> list, int a, int b)
        where T : class
    {
        if (list[a] == list[b]) return false;
        (list[b], list[a]) = (list[a], list[b]);
        return true;
    }
}

public class Product
{
    public string Name { get; }
    public double Price { get; }

    public Product(string name, double price)
    {
        Name = name;
        Price = price;
    }

    public override string ToString() => Name;
}

public abstract class Promotion : IComparable<Promotion>
{
    public int GroupSize { get; }

    public Promotion(int groupSize)
    {
        GroupSize = groupSize;
    }

    public abstract IEnumerable<double> CalculateForAscendingPrices(IEnumerable<Product> products);

    public int CompareTo(Promotion? other) => GroupSize.CompareTo(other!.GroupSize);
}

public class PromotionNone : Promotion
{
    public PromotionNone(int groupSize) : base(groupSize)
    {
    }

    public override IEnumerable<double> CalculateForAscendingPrices(IEnumerable<Product> products) => products.Select(x => x.Price);
}

public class PromotionCheapestDiscount : Promotion
{
    private readonly double _discount;

    public PromotionCheapestDiscount(int groupSize, double discount) : base(groupSize)
    {
        _discount = discount;
    }

    public override IEnumerable<double> CalculateForAscendingPrices(IEnumerable<Product> products)
    {
        bool first = true;
        foreach (var p in products)
        {
            if (first)
            {
                first = false;
                yield return p.Price * (1.0 - _discount);
            }
            else
                yield return p.Price;
        }
    }
}

public class PromotionCheapestFixedPrice : Promotion
{
    private readonly double _fixedPrice;

    public PromotionCheapestFixedPrice(int groupSize, double fixedPrice) : base(groupSize)
    {
        _fixedPrice = fixedPrice;
    }

    public override IEnumerable<double> CalculateForAscendingPrices(IEnumerable<Product> products)
    {
        bool first = true;
        foreach (var p in products)
        {
            if (first)
            {
                first = false;
                yield return _fixedPrice;
            }
            else
                yield return p.Price;


        }
    }
}

public class PromotionCombination
{
    public List<Promotion> Promotions { get; private set; } = new();
    public int Sum { get; private set; }

    public PromotionCombination() { }

    public PromotionCombination Copy() => new()
    {
        Promotions = new(Promotions),
        Sum = Sum
    };

    public void Add(Promotion promotion)
    {
        Promotions.Add(promotion);
        Sum += promotion.GroupSize;
    }

    public void RemoveLast()
    {
        if (Promotions.Count == 0)
            throw new Exception("Trying to remove element from empty combination");
        Sum -= Promotions[^1].GroupSize;
        Promotions.RemoveAt(Promotions.Count - 1);
    }

    public override string ToString() => $"[{string.Join(",", Promotions.Select(x => x.GroupSize))}]";
}
