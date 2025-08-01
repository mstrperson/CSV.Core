# Csv.CSV Class Documentation

## Overview

The `Csv.CSV` class provides a flexible, in-memory representation of a CSV (Comma-Separated Values) document. It supports reading, writing, querying, and manipulating tabular data, with robust handling for quoted values, custom delimiters, and dynamic columns. The class is designed for .NET 9 and C# 13.0.

---

## Key Features

- **Read/Write CSV**: Load from and save to files or streams, with support for custom delimiters.
- **Dynamic Columns**: Columns are inferred from the union of all row keys.
- **Row Access**: Index rows by number, column name, or key-value pairs.
- **Querying**: Filter rows by values or regular expressions.
- **Export**: Convert to JSON, HTML table, or C# record code.
- **Type Inference**: Guess column data types (int, double, DateTime, string).
- **Bulk Operations**: Add, remove, or compare rows and tables.

---

## Usage Example

---

## Constructors

- `CSV()`: Initializes an empty CSV.
- `CSV(string heading)`: Initializes with a heading.
- `CSV(IEnumerable<Row> data)`: Initializes from a collection of rows.
- `CSV(Stream inputStream, Delimeter delimeter = default)`: Loads from a stream.

---

## Factory Methods

- `static CSV Open(string fileName, Delimeter delimeter = default)`
- `static CSV Open(Stream stream, Delimeter delimeter = default)`
- `static CSV CreateFrom(IEnumerable<Row> rows)`

---

## Properties

- `string Heading`  
  Optional heading/title for the table.

- `List<string> AllKeys`  
  List of all column headers (union of all row keys).

- `int ColCount`  
  Number of columns.

- `int RowCount`  
  Number of rows.

- `string JsonString`  
  JSON representation of the table.

---

## Indexers

- `Row this[int index]`  
  Access row by index.

- `List<string> this[string column]`  
  Get all values in a column.

- `Row this[string column, string value]`  
  Get the first row where `row[column] == value`.

- `CSV this[Row primaryKey]`  
  Get all rows matching the key-value pairs in `primaryKey`.

- `CSV this[Dictionary<string, Regex> primaryKey]`  
  Get all rows matching the regex patterns for each column.

---

## Methods

- `void Add(Row row)`  
  Add a row.

- `void Add(CSV other)`  
  Add all rows from another CSV.

- `void Remove(Row row)`  
  Remove all rows matching the given row's key-value pairs.

- `bool Contains(Row row)`  
  Check if a row exists.

- `CSV NotIn(CSV other)`  
  Get rows not present in another CSV.

- `List<string> GetColumn(string header)`  
  Get all values in a column.

- `Row GetRow(string header, string key)`  
  Get the first row where `row[header] == key`.

- `bool ContainsNulls(string column)`  
  Check if a column contains "null" values.

- `string ToCSharpRecordCode(string typeName, string accessLevel = "public")`  
  Generate a C# record definition for the table.

- `Type GetDataType(string column)`  
  Infer the .NET type for a column.

- `string GuessMySqlDataType(string column)`  
  Guess the MySQL data type for a column.

- `void Save(string fileName, Delimeter delimeter = default)`  
  Save to a file.

- `void Save(Stream output, Delimeter delimeter = default, bool leaveStreamOpen = false)`  
  Save to a stream.

- `string HtmlTable(string tableCssClass = "", string headerRowCssClass = "", string rowCssClass = "")`  
  Export as an HTML table.

---

## Delimeter Support

The `Delimeter` struct supports:
- Comma (`,`)
- Semicolon (`;`)
- Tab (`\t`)
- Tilde (`~`)

You can specify a delimiter when reading or writing CSV files.

---

## Row Class

Each row is a `Row` object, which extends `Dictionary<string, string>`.  
- Access values by column name: `row["ColumnName"]`
- Converts to JSON via `row.JsonString`

---

## LINQ Support

The `Csv.CSV` class fully supports LINQ (Language Integrated Query) because it implements `IEnumerable<Row>`. This allows you to use all standard LINQ extension methods (`Where`, `Select`, `OrderBy`, `GroupBy`, etc.) directly on your `CSV` instances and their rows.

### LINQ Usage Examples

#### Filtering Rows
You can filter rows using LINQ's `Where`:
```csharp
using Csv; using System.Linq;
// Load a CSV file 
CSV table = CSV.Open("data.csv");
// Find all rows where the "Status" column is "Active" 
var activeRows = table.Where(row => row["Status"] == "Active");
// Convert the result to a list or a new CSV List<Row> activeList = activeRows.ToList(); CSV activeTable = new CSV(activeRows);
```

#### Projecting Columns

You can select specific columns or transform data:
```csharp
// Get all email addresses from the "Email" column var emails = table.Select(row => row["Email"]).ToList();
```

#### Ordering and Grouping

You can order or group rows by any column:

```csharp
// Order rows by the "LastName" column 
var ordered = table.OrderBy(row => row["LastName"]);
// Group rows by the "Department" column 
var grouped = table.GroupBy(row => row["Department"]);
```

#### Aggregation

You can perform aggregations, such as counting or summing:
```csharp
// Count rows where "Score" > 90 
int highScoreCount = table.Count(row => int.Parse(row["Score"]) > 90);
// Sum a numeric column 
int total = table.Sum(row => int.Parse(row["Amount"]));
```

### Notes

- Each `Row` is a `Dictionary<string, string>`, so you can use dictionary accessors in your LINQ queries.
- You can chain LINQ queries for complex data processing.
- After filtering or projecting, you can create a new `CSV` instance from any `IEnumerable<Row>`.

### Example: Creating a Filtered CSV
```csharp
// Filter and save only rows with non-null "Email" 
var filtered = table.Where(row => 
	!string.IsNullOrWhiteSpace(row["Email"])); 

CSV filteredCsv = new CSV(filtered); 
filteredCsv.Save("filtered.csv");
```
**Summary:**  
The CSV library is designed to work seamlessly with LINQ, enabling expressive, type-safe, and efficient data queries and transformations on tabular data.
### Advanced: Custom Row Types

#### Creating a CSV from DTOs

You can create a new CSV from a collection of DTOs:
```csharp
public record Person(int Id, string Name, string Email) 
{ 
    // Convert from Row to Person public static 
    implicit operator Person(Row row) => 
        new Person( int.TryParse(row["Id"], out var id) ? id : 0, row["Name"], row["Email"] );

    // Convert from Person to Row
    public static implicit operator Row(Person p) => new Row
    {
        ["Id"] = p.Id.ToString(),
        ["Name"] = p.Name,
        ["Email"] = p.Email
    };
}
```

#### Usage in LINQ Queries

You can now use these conversions in LINQ queries for type-safe access:
```csharp
CSV table = CSV.Open("people.csv");

// Convert all rows to Person records 
var people = table.Select(row => (Person)row).ToList();

// Filter and project using DTOs 
var emails = table 
    .Select(row => (Person)row) 
    .Where(person => person.Email.EndsWith("@example.com")) 
    .Select(person => person.Email) 
    .ToList();
```

#### Creating Rows from DTOs

You can also convert DTOs back to `Row` for adding or updating CSV data:
```csharp
var newPerson = new Person(42, "Alice", "alice@example.com"); 
Row row = newPerson; // Implicit conversion
table.Add(row);
```

#### Creating a CSV from DTOs

You can create a new CSV from a collection of DTOs:
```csharp
List<Person> people = GetPeople(); 
CSV csv = [..people.Select(p => (Row)p))];
```

---

**Summary:**  
Using implicit conversions between DTO records and `Row` enables type-safe, expressive, and maintainable code when working with CSV data and LINQ.
