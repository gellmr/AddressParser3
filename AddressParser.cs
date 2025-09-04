using Microsoft.SqlServer.Server;
using System;
using System.Data.SqlTypes;
using System.Text.RegularExpressions;
using System.Data;
using System.Linq;

public class MyAddressDto
{
  public string Line1 { get; set; } = string.Empty;
  public string Line2 { get; set; } = string.Empty;
  public string Line3 { get; set; } = string.Empty;
  public string City { get; set; } = string.Empty;
  public string State { get; set; } = string.Empty;
  public string Country { get; set; } = "Australia";
  public string Zip { get; set; } = string.Empty;
}

/*
Note
Build this project in Release mode. Copy the dll from bin/Release to (dev machine) C:\db-backups\AddressParser.dll
Then use powershell to create a hash value of the dll...
(Get-FileHash -Path "./AddressParser.dll" -Algorithm SHA512).Hash | Out-File "OutputFile.txt"
Open the file and grab the hash value. Eg F11EB805CEE2435...20EC5B770659B
Open SSMS and execute the following SQL, to tell SQL Server to trust this assembly.
  EXEC sp_add_trusted_assembly
  @hash = 0xF11EB805CEE2435...............20EC5B770659B,
  @description = N'AddressParser';
Note you must have 0x before the hash value.
This will allow us to invoke the methods below from SQL.
ALSO you must enable CLR in SQL Server if not already done.
  EXEC sp_configure 'clr enabled', 1;
  RECONFIGURE;
This allows us to call UDF functions (like the C# functions below) from SQL Server.
*/
public static class AddressParser
{
  public static MyAddressDto ParseAddress(string address)
  {
    var stateRegex = new Regex(@"\b(WA|SA|NSW|VIC|QLD|NT|TAS|ACT)\b", RegexOptions.IgnoreCase);
    var zipRegex = new Regex(@"\b\d{4}\b");

    var dto = new MyAddressDto();

    // Extract Zip (4 digits at the end) and State
    var matches = zipRegex.Matches(address);
    var zipMatch = matches[matches.Count - 1];
    if (zipMatch != null)
    {
      dto.Zip = zipMatch.Value;
    }

    matches = stateRegex.Matches(address);
    var stateMatch = matches[matches.Count - 1];
    if (stateMatch.Success)
    {
      dto.State = stateMatch.Value.ToUpper();
    }

    // Remove Zip and State from the string
    var cleanAddress = address;

    if (dto.Zip != null)
    {
      int lastIndex = cleanAddress.LastIndexOf(dto.Zip);
      if (lastIndex >= 0)
      {
        cleanAddress = cleanAddress.Remove(lastIndex, dto.Zip.Length).Trim();
      }
    }

    if (dto.State != null)
    {
      int lastIndex = cleanAddress.LastIndexOf(dto.State, StringComparison.OrdinalIgnoreCase);
      if (lastIndex >= 0)
      {
        cleanAddress = cleanAddress.Remove(lastIndex, dto.State.Length).Trim();
      }
    }

    // Split the remaining string by commas and spaces to find city and lines
    var parts = cleanAddress.Split(new[] { ',', '.' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(p => p.Trim())
                              .Where(p => !string.IsNullOrEmpty(p))
                              .ToList();

    if (parts.Any())
    {
      // The last part is the City
      dto.City = parts.Last();
      parts.RemoveAt(parts.Count - 1);
    }

    // Assign Lines
    if (parts.Count > 0)
    {
      dto.Line1 = parts[0];
    }
    if (parts.Count > 1)
    {
      dto.Line2 = parts[1];
    }
    if (parts.Count > 2)
    {
      dto.Line3 = string.Join(", ", parts.Skip(2));
    }
    return dto;
  }

  [SqlFunction(IsDeterministic = true, DataAccess = DataAccessKind.None)]
  public static SqlString ParseLine1(SqlString address)
  {
    if (address.IsNull) return SqlString.Null;
    try
    {
      string input = address.Value.Trim();
      MyAddressDto dto = ParseAddress(input);
      return dto.Line1.Trim();
    }
    catch (Exception ex)
    {
      return SqlString.Null;
    }
  }

  [SqlFunction(IsDeterministic = true, DataAccess = DataAccessKind.None)]
  public static SqlString ParseLine2(SqlString address)
  {
    if (address.IsNull) return SqlString.Null;
    try
    {
      string input = address.Value.Trim();
      MyAddressDto dto = ParseAddress(input);
      return dto.Line2.Trim();
    }
    catch (Exception ex)
    {
      return SqlString.Null;
    }
  }

  [SqlFunction(IsDeterministic = true, DataAccess = DataAccessKind.None)]
  public static SqlString ParseLine3(SqlString address)
  {
    if (address.IsNull) return SqlString.Null;
    try
    {
      string input = address.Value.Trim();
      MyAddressDto dto = ParseAddress(input);
      return dto.Line3.Trim();
    }
    catch (Exception ex)
    {
      return SqlString.Null;
    }
  }

  [SqlFunction(IsDeterministic = true, DataAccess = DataAccessKind.None)]
  public static SqlString ParseCity(SqlString address)
  {
    if (address.IsNull) return SqlString.Null;
    try
    {
      string input = address.Value.Trim();
      MyAddressDto dto = ParseAddress(input);
      return dto.City.Trim();
    }
    catch (Exception ex)
    {
      return SqlString.Null;
    }
  }

  [SqlFunction(IsDeterministic = true, DataAccess = DataAccessKind.None)]
  public static SqlString ParseState(SqlString address)
  {
    if (address.IsNull) return SqlString.Null;
    try
    {
      string input = address.Value.Trim();
      MyAddressDto dto = ParseAddress(input);
      return dto.State.Trim();
    }
    catch (Exception ex)
    {
      return SqlString.Null;
    }
  }

  [SqlFunction(IsDeterministic = true, DataAccess = DataAccessKind.None)]
  public static SqlString ParseZip(SqlString address)
  {
    if (address.IsNull) return SqlString.Null;
    try
    {
      string input = address.Value.Trim();
      MyAddressDto dto = ParseAddress(input);
      return dto.Zip.Trim();
    }
    catch (Exception ex)
    {
      return SqlString.Null;
    }
  }
}
