﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;
using System.Runtime.Serialization;
using System.Text;

/// <summary>
/// Summary description for CSV
/// </summary>
namespace Csv;

public struct Delimeter
{
    public static Delimeter Comma = new Delimeter(',');
    public static Delimeter Semicolon = new Delimeter(';');
    public static Delimeter Tab = new Delimeter('\t');
    public static Delimeter Tilde = new Delimeter('~');

    private char _value = ',';

    public Delimeter() => _value = ',';
    private Delimeter(char value) => _value = value;

    public static implicit operator Delimeter(char ch) => ch switch
    {
        ',' => Delimeter.Comma,
        ';' => Delimeter.Semicolon,
        '\t' => Delimeter.Tab,
        '~' => Delimeter.Tilde,
        _ => Delimeter.Comma
    };

    public static implicit operator char(Delimeter value) => value._value == '\0' ? ',' : value._value;

    public override string ToString() => _value == '\0' ? "," : $"{_value}";
}


/// <summary>
/// Just a Dictionary of string, string data.
/// Keys are Column Headers.
/// Values are cell values in this row.
/// </summary>
public class Row : Dictionary<string,string>
{
    public bool Equals(Row other)
    {
        foreach (string key in this.Keys)
        {
            if (!this[key].Equals(other[key]))
                return false;
        }

        foreach (string key in other.Keys)
        {
            if (!this.ContainsKey(key)) 
                return false;
        }   
        
        return true;
    }
    
    /// <summary>
    /// extend the Dictionary Array Index Operator for a CSV Row.
    /// </summary>
    /// <param name="key">row header</param>
    /// <returns></returns>
    public new string this[string key]
    {
        get
        {
            if (this.ContainsKey(key))
                return base[key];
            else
                return "";
        }
        set
        {
            if (this.ContainsKey(key))
                base[key] = value;
            else
                this.Add(key, value);
        }
    }

    public Row() { }
    protected static bool ValueHasEvenNumberOfQuotes(string line)
    {
        bool odd = true;
        foreach(char ch in line)
            if (ch == '"')
                odd = !odd;

        return odd;
    }

    internal static string[] ConsolidateQuotedValues(string[] values, Delimeter delimeter)
    {
        List<string> newValues = new List<string>();

        for (int i = 0; i < values.Length; i++)
        {
            if (ValueHasEvenNumberOfQuotes(values[i]))
            {
                newValues.Add(values[i]);
            }
            else
            {
                string newValue = $"{values[i++]}";
                
                while (i < values.Length && !ValueHasEvenNumberOfQuotes(newValue))
                {
                    newValue += $"{(char)delimeter}{values[i++]}";
                }

                if (i > values.Length && newValue.StartsWith("\""))
                {
                    newValue += "\"";
                }
                else
                    i--;
                
                newValues.Add(newValue);
            }
        }

        return newValues.ToArray();
    }
    public Row(string csvText, string[] headers, Delimeter delimeter = default)
    {
        string[] values = csvText.Split((char) delimeter);
        values = ConsolidateQuotedValues(values, delimeter);
        
        for (int i = 0; i < headers.Length && i < values.Length; i++)
        {
            if (values[i].StartsWith("\"") && values[i].EndsWith("\""))
            {
                values[i] = values[i].Substring(1, values[i].Length - 2);
            }

            while (this.ContainsKey(headers[i])) 
                headers[i] = $"{headers[i]} ";

            this.Add(headers[i], values[i]);
        }
    }

    /// <summary>
    /// get this row's data as a JSON string.
    /// </summary>
    public string JsonString
    {
        get
        {
            bool first = true;
            string json = "{";
            foreach(string key in this.Keys)
            {
                json += string.Format("{2}\"{0}\":\"{1}\"", key, this[key], first?"\n\t": ",\n\t");
                first = false;
            }
            json += "\n}";

            return json;
        }
    }
}

/// <summary>
/// CSV document, a collection of Rows.
/// The CSV's collumn headers are the collected keys of all the rows in the document.
/// </summary>
[DataContract]
public class CSV : IEnumerable<Row>
{
    public static CSV Open(string fileName, Delimeter delimeter = default) => new(File.OpenRead(fileName), delimeter);
    public static CSV Open(Stream stream, Delimeter delimeter = default) => new(stream, delimeter);

    public static CSV CreateFrom(IEnumerable<Row> rows) => new(rows);

    /// <summary>
    /// A title or heading for the table.  If you want one...
    /// </summary>
    [DataMember(Name="heading")]
    public string Heading
    { get; set; }

    [DataMember(Name="data")]
    internal List<Row> _data;

    /// <summary>
    /// Readonly access to all of the rows of this table.
    /// </summary>

    /// <summary>
    /// Gets the Enumerator for iterating over this CSV.
    /// Enumerates the Rows of this CSV.
    /// </summary>
    /// <returns></returns>
    public IEnumerator<Row> GetEnumerator() => ((IEnumerable<Row>)_data).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<Row>)_data).GetEnumerator();

    /// <summary>
    /// Readonly access to the data in this table by row number.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Row this[int index] => this._data[index];

    [IgnoreDataMember]
    public string JsonString
    {
        get
        {
            string json = "[";
            bool first = true;
            foreach (Row row in this)
            {
                json += string.Format("{0}{1}", first ? "\n\t" : ",\n\t", row.JsonString.Replace("\n", "\n\t"));
                first = false;
            }

            json += "\n]";
            return json;
        }
    }
    
    public string HtmlTable(string tableCssClass = "", string headerRowCssClass = "", string rowCssClass = "")
    {
        string html = string.Format("<table{0}>{2}<tr{1}>",
            string.IsNullOrEmpty(tableCssClass) ? "" : string.Format(" class=\"{0}\"", tableCssClass),
            string.IsNullOrEmpty(headerRowCssClass) ? "" : string.Format(" class=\"{0}\"", headerRowCssClass),
            string.IsNullOrEmpty(Heading)? "" : string.Format("<thead>{0}</thead>", Heading));
        foreach(string header in AllKeys)
        {
            html += string.Format("<th>{0}</th>", header);
        }

        html += "</tr>";

        foreach(Row row in this)
        {
            html += string.Format("<tr{0}>", string.IsNullOrEmpty(rowCssClass) ? "" : string.Format(" class=\"{0}\"", rowCssClass));
            foreach(string header in AllKeys)
            {
                html += string.Format("<td>{0}</td>", row[header]);
            }
            html += "</tr>";
        }

        html += "</table>";
        return html;
    }

    /// <summary>
    /// Query this table and find all rows that match a set of values.
    /// </summary>
    /// <param name="primaryKey"></param>
    /// <returns></returns>
    public CSV this[Row primaryKey]
    {
        get
        {
            CSV output = new CSV();
            foreach (Row row in this)
            {
                foreach (string key in primaryKey.Keys)
                {
                    if (!row.ContainsKey(key) || !row[key].Equals(primaryKey[key]))
                    {
                        continue;
                    }
                }

                output.Add(row);
            }

            return output;
        }
    }

    /// <summary>
    /// Query this table and find all rows that match a set of regular expressions.
    /// Use this to find all rows where a data fits a particular set of regular expressions.
    /// for example, find all rows in a contact list where the Phone Number has a 540 area code
    /// and the street address is in a particular town.
    /// </summary>
    /// <param name="primaryKey"></param>
    /// <returns></returns>
    public CSV this[Dictionary<string, Regex> primaryKey]
    {
        get
        {
            CSV output = new CSV();
            foreach (Row row in this)
            {
                foreach (string key in primaryKey.Keys)
                {
                    if (!row.ContainsKey(key) || !primaryKey[key].IsMatch(row[key]))
                    {
                        continue;
                    }
                }

                output.Add(row);
            }

            return output;
        }
    }

    /// <summary>
    /// Quick get Column by name.
    /// </summary>
    /// <param name="column"></param>
    /// <returns></returns>
    public List<string> this[string column] => this.GetColumn(column);

    public bool ContainsNulls(string column)
    {
        List<string> col = this.GetColumn(column);
        foreach (string val in col)
            if (val.ToLowerInvariant().Equals("null"))
                return true;

        return false;
    }

    public string ToCSharpRecordCode(string typeName, string accessLevel = "public") =>
        $"""
        {accessLevel} record {typeName}(
        \t{string.Join($",{Environment.NewLine}\t", AllKeys.Select(k => $"{GetDataType(k).Name} {k}"))});
        """;

    public Type GetDataType(string column) =>
        this.GuessMySqlDataType(column) switch
        {
            "INT" => typeof(int),
            "DOUBLE" => typeof(double),
            "DATETIME" => typeof(DateTime),
            "TEXT" => typeof(string),
            _ => typeof(string)
        };


    public string GuessMySqlDataType(string column)
    {
        string type = "TEXT";
        List<string> col = this.GetColumn(column);

        Regex interger = new Regex(@"^(null|-?\d+)$");
        Regex floating = new Regex(@"^(null|-?\d*\.?\d+)$");

        bool maybeInt = true;
        bool maybeDouble = true;
        bool maybeDateTime = true;

        int longest = 0;

        bool containsNulls = false;

        foreach(string value in col)
        {
            if (value.Length > longest) longest = value.Length;
            
            string lval = value.ToLowerInvariant();
            if (lval.Equals("privacysuppressed")) lval = "null";
            if (lval.Equals("null")) containsNulls = true;

            if (maybeInt && !interger.IsMatch(lval))
                maybeInt = false;

            if (maybeDouble && !floating.IsMatch(lval))
                maybeDouble = false;

            DateTime dt;
            if (maybeDateTime && !lval.Equals("null") && !DateTime.TryParse(value, out dt))
                maybeDateTime = false;
        }

        if (maybeDateTime) type = "DATETIME";
        else if (maybeInt) type = "INT";
        else if (maybeDouble) type = "DOUBLE";

        else if(longest <= 255)
            type = "varchar(255)";

        if (containsNulls)
            type += " NULL";
        else
            type += " NOT NULL";

        return type;
    }

    /// <summary>
    /// Shortcut for GetRow.
    /// </summary>
    /// <param name="column"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Row this[string column, string value] => this.GetRow(column, value);

    /// <summary>
    /// Initialize a blank CSV.
    /// </summary>
    /// <param name="heading"></param>
    public CSV(string heading = "")
    {
        Heading = heading;
        _data = new List<Row>();
        _allKeys = new List<string>();
    }

    /// <summary>
    /// Initialize a CSV from a List of Dictionaries.
    /// </summary>
    public CSV(IEnumerable<Row> data)
    {
        Heading = "";
        _data = data.ToList();
        _allKeys = new List<string>();
    }

    public CSV()
    {
        Heading = "";
        _data = new List<Row>();
        _allKeys = new ();
    }

    protected static bool LineHasEvenNumberOfQuotes(string line)
    {
        bool odd = true;
        foreach(char ch in line)
            if (ch == '"')
                odd = !odd;

        return odd;
    }

    protected static string[] ConsolidateQuotedCsvLines(string[] lines)
    {
        List<string> newLines = new List<string>();

        for (int i = 0; i < lines.Length; i++)
        {
            if (LineHasEvenNumberOfQuotes(lines[i]))
            {
                newLines.Add(lines[i]);
            }
            else
            {
                string newLine = $"{lines[i++]}";
                
                while (i < lines.Length && !LineHasEvenNumberOfQuotes(newLine))
                {
                    newLine += $"{Environment.NewLine}{lines[i++]}";
                }

                if (i > lines.Length)
                {
                    newLine += "\"";
                }
                else
                    i--;
                
                newLines.Add(newLine);
            }
        }

        return newLines.ToArray();
    }

    /// <summary>
    /// Open a CSV from a stream.
    /// </summary>
    /// <param name="inputStream"></param>
    /// <param name="delimeter"></param>
    public CSV(Stream inputStream, Delimeter delimeter = default)
    {
        Heading = "";
        _data = new List<Row>();
        _allKeys = new List<string>();
        using StreamReader reader = new StreamReader(inputStream);
        string content = reader.ReadToEnd();

        string[] lines = content.Replace("\r", "").Split('\n');

        lines = ConsolidateQuotedCsvLines(lines);

        string[] headers = lines[0].Split((char) delimeter);

        headers = Row.ConsolidateQuotedValues(headers, delimeter);

        for (int i = 0; i < headers.Length; i++)
        {

            if (headers[i].StartsWith("\"") && headers[i].EndsWith("\""))
            {
                headers[i] = headers[i].Substring(1, headers[i].Length - 2);
            }
        }

        for(int l = 1; l < lines.Length; l++)
        {
            Row row = new Row(lines[l], headers, delimeter);
            /*string line = lines[l];
            string[] values = line.Split((char) delimeter);
            for (int i = 0; i < headers.Length && i < values.Length; i++)
            {
                if (values[i].StartsWith("\"") && values[i].EndsWith("\""))
                {
                    values[i] = values[i].Substring(1, values[i].Length - 2);
                }

                while (row.ContainsKey(headers[i])) headers[i] = $"{headers[i]} ";

                row.Add(headers[i], values[i]);
            }*/

            _data.Add(row);
        }
    }

    /// <summary>
    /// Add a row to this CSV.
    /// Resets the AllKeys field.
    /// </summary>
    /// <param name="row"></param>
    public void Add(Row row)
    {
        _data.Add(row);
        _allKeys = new List<string>();
    }


    /// <summary>
    /// Check to see if a row is in this CSV.
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    public bool Contains(Row row)
    {
        for (int i = 0; i < _data.Count; i++)
        {
            bool match = true;
            foreach (string key in row.Keys)
            {
                if (!_data[i].ContainsKey(key))
                {
                    match = false;
                    break;
                }

                if (!_data[i][key].Equals(row[key]))
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Get a CSV containing all the entries of this CSV which do not correspond to entries in the Other CSV.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public CSV NotIn(CSV other)
    {
        CSV newCsv = new CSV();

        List<string> commonKeys = new List<string>();
        foreach (string key in AllKeys)
        {
            if (other.AllKeys.Contains(key))
            {
                commonKeys.Add(key);
            }
        }

        foreach (Row row in _data)
        {
            Row strippedRow = new Row();
            foreach (string key in commonKeys)
            {
                strippedRow.Add(key, row[key]);
            }

            if (!other.Contains(strippedRow))
            {
                newCsv.Add(row);
            }
        }

        return newCsv;
    }

    /// <summary>
    /// Remove a row from this CSV.
    /// </summary>
    /// <param name="row"></param>
    public void Remove(Row row)
    {
        #region Search
        int index = -1;
        bool foundMatch;
        do
        {
            foundMatch = false;
            for (int i = 0; i < _data.Count; i++)
            {
                bool match = true;
                foreach (string key in row.Keys)
                {
                    if (!_data[i].ContainsKey(key))
                    {
                        match = false;
                        break;
                    }

                    if (!_data[i][key].Equals(row[key]))
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    index = i;
                    foundMatch = true;
                    break;
                }
            }

            if (index != -1)
                _data.RemoveAt(index);
        } while (foundMatch);
        #endregion

        _allKeys = new List<string>();
    }

    [IgnoreDataMember]
    private List<string> _allKeys { get; set; }

    /// <summary>
    /// Get the list of all keys in this CSV.
    /// Not every row is guaranteed to have a value for every key.
    /// </summary>
    [IgnoreDataMember]
    public List<string> AllKeys
    {
        get
        {
            if (_allKeys == null || _allKeys.Count == 0)
            {
                _allKeys = new List<string>();

                foreach (Row row in _data)
                {
                    foreach (string key in row.Keys)
                    {
                        if (!_allKeys.Contains(key))
                        {
                            _allKeys.Add(key);
                        }
                    }
                }
            }
            return _allKeys;
        }
    }


    /// <summary>
    /// How many Columns are in this CSV?
    /// </summary>
    [IgnoreDataMember]
    public int ColCount => AllKeys.Count;

    /// <summary>
    /// How many rows are in this CSV?
    /// </summary>
    [IgnoreDataMember]
    public int RowCount => _data.Count;

    /// <summary>
    /// Save the CSV to a file.
    /// This method will delete an existing file with this name.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="delimeter"></param>
    public void Save(string fileName, Delimeter delimeter = default)
    {
        if (File.Exists(fileName)) File.Delete(fileName);
        using(FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate))
            this.Save(fs, delimeter);
    }

    private static bool ValueNeedsQuoting(string value) => value is null || value.Contains("\n") || value.Contains(",") || value.Contains("\"");
    
    /// <summary>
    /// Save the CSV to a stream.
    /// </summary>
    /// <param name="output"></param>
    /// <param name="delimeter"></param>
    /// <param name="leaveStreamOpen">leave the base stream open when done?</param>
    public void Save(Stream output, Delimeter delimeter = default, bool leaveStreamOpen = false)
    {
        using (StreamWriter writer = new StreamWriter(output, Encoding.UTF8, leaveOpen: leaveStreamOpen))
        {
            writer.AutoFlush = true;

            if (AllKeys.Count <= 0)
            {
                return;
            }

            writer.Write(ValueNeedsQuoting(AllKeys[0]) ? $"\"{AllKeys[0]}\"" : AllKeys[0]);
            for (int i = 1; i < AllKeys.Count; i++)
            {
                writer.Write("{0}{1}", (char) delimeter, ValueNeedsQuoting(AllKeys[i]) ? $"\"{AllKeys[i]}\"" : AllKeys[i]);
            }

            writer.WriteLine();

            foreach (Row row in _data)
            {
                if (row.ContainsKey(AllKeys[0]))
                    writer.Write(ValueNeedsQuoting(row[AllKeys[0]]) ? $"\"{row[AllKeys[0]]}\"" : row[AllKeys[0]]);
                for (int i = 1; i < AllKeys.Count; i++)
                {
                    writer.Write((char) delimeter);
                    if (row.ContainsKey(AllKeys[i]))
                    {
                        writer.Write(ValueNeedsQuoting(row[AllKeys[i]]) ? $"\"{row[AllKeys[i]]}\"" : row[AllKeys[i]]);
                    }
                }

                writer.WriteLine();
            }
        }
    }

    /// <summary>
    /// Bulk add data from another CSV object.
    /// </summary>
    /// <param name="other"></param>
    public void Add(CSV other)
    {
        foreach (Row row in other._data)
        {
            this.Add(row);
        }
    }

    /// <summary>
    /// Get all of the data in the requested column of the table.
    /// if an invalid header is given, throws an ArgumentOutOfRangeException.
    /// </summary>
    /// <param name="header">Column Header from this table.</param>
    /// <returns></returns>
    public List<string> GetColumn(string header)
    {
        if (!AllKeys.Contains(header))
        {
            header = string.Format("\"{0}\"", header);
            if(!AllKeys.Contains(header))
                throw new ArgumentOutOfRangeException("Invalid Header Name");
        }
        List<string> column = new List<string>();
        foreach (Row row in _data)
        {
            if (!row.ContainsKey(header)) column.Add("");
            else column.Add(row[header]);
        }

        return column;
    }

    /// <summary>
    /// Get the first row matching the given headr, key pair.
    /// if no row under the given header contains the given key, 
    /// throws an ArgumentOutOfRangeException.
    /// 
    /// </summary>
    /// <param name="header"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public Row GetRow(string header, string key)
    {
        foreach (Row row in _data)
        {
            if (!row.ContainsKey(header)) continue;

            if (row[header].Equals(key))
            {
                return row;
            }
        }

        throw new ArgumentOutOfRangeException($"{key} was not found under the {header} header.");
    }
}
