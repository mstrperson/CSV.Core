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

````````

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

## Notes

- Handles quoted values and embedded delimiters.
- Throws `ArgumentOutOfRangeException` for invalid column access.
- Not thread-safe for concurrent modifications.

---

## License

This library is provided as-is, without warranty.  
See the project repository for license details.
