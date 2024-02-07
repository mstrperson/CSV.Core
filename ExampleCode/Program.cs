// See https://aka.ms/new-console-template for more information

using Csv;
using HashidsNet;
using System.Text;

// something for fun.
Hashids hashids = new Hashids("SomeSaltForYou", 10);

// empty CSV file.
CSV TableData = new CSV();

for(int i = 0; i < 20; i++)
{
    Row row = new Row()
    {
        { "Number", $"{i}" },
        { "HashedNumber", hashids.Encode(i) },
        { "Base64 Hash", Convert.ToBase64String(Encoding.UTF8.GetBytes(hashids.Encode(i))) }
    };
    // add the row to the file.
    TableData.Add(row);
}


// save the file to your computer
TableData.Save(File.OpenWrite("tableData.csv"));

Console.Write("Enter a CSV File Path:  ");
var path = Console.ReadLine()!;

try
{
    // Open a file given a path to a CSV.
    CSV file = new CSV(File.OpenRead(path));

    foreach (var row in file)
    {
        foreach (var header in row.Keys)
        {
            Console.Write($"{header}:{row[header]}\t");
        }
        Console.WriteLine();
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}

// look below for the MathRow definition.
CSV MathTable = new CSV();
for(decimal x = -15; x <= 15; x += 0.01M)
{
    MathTable.Add(new MathRow(x, (x) => x != 0 ? (decimal)(Math.Sin((double)x) / (double)x) : decimal.MaxValue));
}

MathTable.Save("MATH.csv");

// you can also do crazy stuff to Rows....
internal class MathRow : Row
{
    public decimal fx 
    { 
        get => decimal.TryParse(this["f(x)"], out decimal result) ? result : 0M; 
    }
    public decimal x { get => decimal.TryParse(this["x"], out decimal result) ? result : 0M; }

    public MathRow(decimal x, Func<decimal, decimal> f)
    {
        this["x"] = $"{x}";
        this["f(x)"] = $"{f(x)}";
    }
}


