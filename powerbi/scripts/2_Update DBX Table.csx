#r "Microsoft.VisualBasic"
using Microsoft.VisualBasic;
using System.Data.Odbc;
using sysData = System.Data;

//check that user has a table selected
if (Selected.Tables.Count == 0)
{
    Interaction.MsgBox("Select one or more tables", MsgBoxStyle.Critical, "Table Required");
    return;
}

//prompt for personal access token
string dbxPAT;
do
{
    dbxPAT = Interaction.InputBox("Please enter your Databricks Personal Access Token (needed to connect to the SQL Endpoint) ", "Personal Access Token", "-");
    if (dbxPAT == "") return;
    if (dbxPAT == "-")
    {
        Interaction.MsgBox("Personal Access Token required", MsgBoxStyle.Critical, "Personal Access Token required");
    }
} while (dbxPAT == "-");

//set connection variables from extended properties
string dbxServer = Model.Expressions["_DbxServer"].GetExtendedProperty("DbxServer");
string dbxEndpoint = Model.Expressions["_DbxEndpoint"].GetExtendedProperty("DbxEndpoint");

//set DBX connection string
var odbcConnStr = @"DSN=Simba Spark;driver=C:\Program Files (x86)\Simba Spark ODBC Driver;host=" +
        dbxServer +
        ";port=443;httppath=" +
        dbxEndpoint +
        ";thrifttransport=2;ssl=1;authmech=3;uid=token;pwd=" +
        dbxPAT;

//test connection
OdbcConnection conn = new OdbcConnection(odbcConnStr);
try
{
    conn.Open();
}
catch
{
    Interaction.MsgBox(@"Connection failed

Please check the following prequisites:
    
- you must have the Simba Spark 32-Bit ODBC Driver installed 
(download from https://www.databricks.com/spark/odbc-drivers-download)

- the ODBC driver must be installed in the path C:\Program Files (x86)\Simba Spark ODBC Driver

- check that the Databricks server name " + dbxServer +
@" is correct

- check that the Datacricks SQL endpoint / HTTP Path " + dbxEndpoint +
@" is correct

- check that you have used a valid Personal Access Token",
    MsgBoxStyle.Critical,
    "Connection Error");
    return;
}

//loop through each selected table
foreach (var t in Selected.Tables)
{
    string tableName = t.Name;
    string dbxSource = Model.Tables[tableName].GetExtendedProperty("Source");
    if (dbxSource == "Databricks") {
    string dbxDatabase = Model.Tables[tableName].GetExtendedProperty("SourceDatabase");
    string dbxTable = Model.Tables[tableName].GetExtendedProperty("SourceTable");

    //get table data from DBX
    var query = @"DESCRIBE " + dbxDatabase + "." + dbxTable;

    OdbcDataAdapter da = new OdbcDataAdapter(query, conn);
    var dbxColumns = new sysData.DataTable();

    try
    {
        da.Fill(dbxColumns);
    }
    catch
    {
        Interaction.MsgBox(@"Connection failed

Either: 
    - the table " + dbxDatabase + "." + dbxTable + " does not exist" +
        @"
    
    - you do not have permissions to query this table
    
    - the connection timed out. Please check that the SQL Endpoint cluster is running",
        MsgBoxStyle.Critical,
        "Connection Error");
        return;
    }

    //update Existing Columns
    foreach (sysData.DataRow row in dbxColumns.Rows)
    {
        string sourceColumn = row["col_name"].ToString();
        var dt = row["data_type"].ToString();
        int counter = 0;
        foreach (var c in t.Columns)
        {
            string tableColumn = c.GetExtendedProperty("SourceColumn");
            if (tableColumn == sourceColumn)
            {
                counter = counter + 1;
                c.Description = row["comment"].ToString();
                c.SummarizeBy = AggregateFunction.None;
                // set data types  
                if (dt == "date")
                {
                    if (c.DataType != DataType.DateTime)
                    {
                        c.DataType = DataType.DateTime;
                        c.FormatString = "yyyy-mm-dd";
                    }
                }
                if (dt == "timestamp")
                {
                    if (c.DataType != DataType.DateTime)
                    {
                        c.DataType = DataType.DateTime;
                        c.FormatString = "hh:nn:ss";
                    }
                }
                if (dt == "string")
                {
                    c.DataType = DataType.String;
                }
                if (dt == "int" || dt == "tinyint" || dt == "smallint" || dt == "bigint")
                {
                    c.DataType = DataType.Int64;
                }
                if (dt == "boolean")
                {
                    c.DataType = DataType.Boolean;
                }
                if (dt.StartsWith("decimal"))
                {
                    c.DataType = DataType.Decimal;
                }
                if (dt == "double" || dt == "float")
                {
                    c.DataType = DataType.Double;
                }
            }
        }
        // add new columns
        if (counter == 0)
        {
            var newColumn = t.AddDataColumn(sourceColumn);
            newColumn.SourceColumn = sourceColumn;
            newColumn.Description = row["comment"].ToString();
            newColumn.SummarizeBy = AggregateFunction.None;
            newColumn.SetExtendedProperty("SourceColumn", sourceColumn, 0);

            // set data types  
            if (dt == "date")
            {
                newColumn.DataType = DataType.DateTime;
                newColumn.FormatString = "yyyy-mm-dd";
            }
            if (dt == "timestamp")
            {
                newColumn.DataType = DataType.DateTime;
                newColumn.FormatString = "hh:nn:ss";
            }
            if (dt == "string")
            {
                newColumn.DataType = DataType.String;
            }
            if (dt == "int" || dt == "tinyint" || dt == "smallint" || dt == "bigint")
            {
                newColumn.DataType = DataType.Int64;
            }
            if (dt == "boolean")
            {
                newColumn.DataType = DataType.Boolean;
            }
            if (dt.StartsWith("decimal"))
            {
                newColumn.DataType = DataType.Decimal;
            }
            if (dt == "double" || dt == "float")
            {
                newColumn.DataType = DataType.Double;
            }
        }
    }

    // delete columns that no longer exist at source
    string[] deleteCols = { };
    foreach (var c in t.Columns)
    {
        int counter = 0;
        string tableColumn = c.GetExtendedProperty("SourceColumn");
        foreach (sysData.DataRow row in dbxColumns.Rows)
        {
            string sourceColumn = row["col_name"].ToString();
            if (tableColumn == sourceColumn)
            {
                counter = counter + 1;
            }
        }
        if (counter == 0)
        {
            deleteCols = deleteCols.Concat(new[] { c.Name }).ToArray();
        }
    }
    foreach (string c in deleteCols)
    {
        t.Columns[c].Delete();
    }
}
}
conn.Close();