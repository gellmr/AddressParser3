# AddressParser3
Class library to create .dll with SQL UDF functions, for SQL Server

This project contains User Defined Functions that can be called from SQL.

You can use the functions like this:

```sql
-- Insert parsed Shipping addresses into the temporary table using the CLR functions.
-- We use the scalar UDFs to call the C# regex logic for each address component.
INSERT INTO #ParsedAddresses (OrderID, IsShipping, Line1, Line2, Line3, City, State, Zip)
SELECT
    O.ID as OrderID,
    1 AS IsShipping,
    dbo.fn_ParseAddressLine1(O.ShippingAddress),
    dbo.fn_ParseAddressLine2(O.ShippingAddress),
    dbo.fn_ParseAddressLine3(O.ShippingAddress),
    dbo.fn_ParseAddressCity(O.ShippingAddress),
    dbo.fn_ParseAddressState(O.ShippingAddress),
    dbo.fn_ParseAddressZip(O.ShippingAddress)
FROM Orders AS O
WHERE dbo.fn_ParseAddressLine1(O.ShippingAddress) IS NOT NULL;
```

Build the project in Release mode. Copy the dll from bin/Release to (dev machine) `C:\db-backups\AddressParser.dll`
Then use powershell to create a hash value of the dll...
`(Get-FileHash -Path "./AddressParser.dll" -Algorithm SHA512).Hash | Out-File "OutputFile.txt"`

Open the file and grab the hash value. Eg `F11EB805CEE2435...20EC5B770659B`
Open SSMS and execute the following SQL, to tell SQL Server to trust this assembly.

```sql
  EXEC sp_add_trusted_assembly
  @hash = 0xF11EB805CEE2435...............20EC5B770659B,
  @description = N'AddressParser';
```

Note you must have 0x before the hash value.
This will allow us to invoke the methods below from SQL.

ALSO you must enable CLR in SQL Server if not already done.

```sql
  EXEC sp_configure 'clr enabled', 1;
  RECONFIGURE;
```

This allows us to call UDF functions from within SQL, in SQL Server.

Now you have the .dll enabled, you may want to use it in a migration script like this:

```c#
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace ReactWithASP.Server.Migrations
{
  public partial class OrderHasAddress : Migration
  {
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      string sql = @"
        -- SQL to create the UDFs.

        -- Drop existing functions and assembly if they exist
        IF OBJECT_ID('dbo.fn_ParseAddressZip') IS NOT NULL DROP FUNCTION dbo.fn_ParseAddressZip;
        IF OBJECT_ID('dbo.fn_ParseAddressState') IS NOT NULL DROP FUNCTION dbo.fn_ParseAddressState;
        IF OBJECT_ID('dbo.fn_ParseAddressCity') IS NOT NULL DROP FUNCTION dbo.fn_ParseAddressCity;
        IF OBJECT_ID('dbo.fn_ParseAddressLine3') IS NOT NULL DROP FUNCTION dbo.fn_ParseAddressLine3;
        IF OBJECT_ID('dbo.fn_ParseAddressLine2') IS NOT NULL DROP FUNCTION dbo.fn_ParseAddressLine2;
        IF OBJECT_ID('dbo.fn_ParseAddressLine1') IS NOT NULL DROP FUNCTION dbo.fn_ParseAddressLine1;
        IF EXISTS(SELECT 1 FROM sys.assemblies WHERE name = 'AddressParser') DROP ASSEMBLY AddressParser;
        GO

        -- Create the new assembly from the compiled C# DLL. Note, the DLL must be compiled in release mode.
        CREATE ASSEMBLY AddressParser
        FROM 'C:\db-backups\AddressParser.dll'
        WITH PERMISSION_SET = SAFE;
        GO

        -- Create the SQL User-Defined Functions for each C# method
        CREATE FUNCTION dbo.fn_ParseAddressLine1(@address NVARCHAR(MAX))
        RETURNS NVARCHAR(MAX)
        AS EXTERNAL NAME AddressParser.AddressParser.ParseLine1;
        GO

        CREATE FUNCTION dbo.fn_ParseAddressLine2(@address NVARCHAR(MAX))
        RETURNS NVARCHAR(MAX)
        AS EXTERNAL NAME AddressParser.AddressParser.ParseLine2;
        GO

        CREATE FUNCTION dbo.fn_ParseAddressLine3(@address NVARCHAR(MAX))
        RETURNS NVARCHAR(MAX)
        AS EXTERNAL NAME AddressParser.AddressParser.ParseLine3;
        GO

        CREATE FUNCTION dbo.fn_ParseAddressCity(@address NVARCHAR(MAX))
        RETURNS NVARCHAR(MAX)
        AS EXTERNAL NAME AddressParser.AddressParser.ParseCity;
        GO

        CREATE FUNCTION dbo.fn_ParseAddressState(@address NVARCHAR(MAX))
        RETURNS NVARCHAR(MAX)
        AS EXTERNAL NAME AddressParser.AddressParser.ParseState;
        GO

        CREATE FUNCTION dbo.fn_ParseAddressZip(@address NVARCHAR(MAX))
        RETURNS NVARCHAR(MAX)
        AS EXTERNAL NAME AddressParser.AddressParser.ParseZip;
        GO

        -- Create a temporary table to hold parsed addresses before inserting them into the Addresses table.
        CREATE TABLE #ParsedAddresses (
            OrderID INT,
            Line1 NVARCHAR(MAX),
            Line2 NVARCHAR(MAX),
            Line3 NVARCHAR(MAX),
            City NVARCHAR(MAX),
            State NVARCHAR(MAX),
            Zip NVARCHAR(MAX)
        );

        -- Insert parsed Shipping addresses into the temporary table using the CLR functions.
        -- We use the scalar UDFs to call the C# regex logic for each address component.
        INSERT INTO #ParsedAddresses (OrderID, Line1, Line2, Line3, City, State, Zip)
        SELECT
            O.ID as OrderID,
            dbo.fn_ParseAddressLine1(O.ShippingAddress),
            dbo.fn_ParseAddressLine2(O.ShippingAddress),
            dbo.fn_ParseAddressLine3(O.ShippingAddress),
            dbo.fn_ParseAddressCity(O.ShippingAddress),
            dbo.fn_ParseAddressState(O.ShippingAddress),
            dbo.fn_ParseAddressZip(O.ShippingAddress)
        FROM Orders AS O
        WHERE dbo.fn_ParseAddressLine1(O.ShippingAddress) IS NOT NULL;
        ";
      migrationBuilder.Sql(sql);
    }
  }
}
```
