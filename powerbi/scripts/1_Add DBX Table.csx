#r "Microsoft.VisualBasic"
using Microsoft.VisualBasic;
using System.Data.Odbc;
using sysData = System.Data;

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

//set variables from extended properties
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
var continueMsg = MsgBoxResult.Yes;
do
{
    //prompt for DBX database/schema name
    string dbxDatabase;
    do
    {
        dbxDatabase = Interaction.InputBox("Please enter the Databricks Database/Schema name", "Databricks Database", "-");
        if (dbxDatabase == "") return;
        if (dbxDatabase == "-")
        {
            Error("Database name required");
        }
    } while (dbxDatabase == "-");

    //prompt for DBX source table name
    string dbxTable;
    do
    {
        dbxTable = Interaction.InputBox("Please enter Databricks source table name", "Databricks Source Table", "-");
        if (dbxTable == "") return;
        if (dbxTable == "-")
        {
            Error("Source table name required");
        }
    } while (dbxTable == "-");

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

    //prompt for dataset target table name
    string dsTable;
    do
    {
        dsTable = Interaction.InputBox("Please enter Dataset target table name  (i.e. what you would like the table to be called in your model)", "Dataset Target Table", "-");
        if (dsTable == "") return;
        if (dsTable == "-")
        {
            Interaction.MsgBox("Target table name required", MsgBoxStyle.Critical, "Target table name required");
        }
    } while (dsTable == "-");

    //check if that table name already exists
    if (Model.Tables.Contains(dsTable))
    {
        do
        {
            dsTable = Interaction.InputBox(@"Target table name already exists
            
    Please select a different table name", "Dataset Target Table", dsTable);
            if (dsTable == "") return;
            if (dsTable == "-")
            {
                Interaction.MsgBox("Target table name required", MsgBoxStyle.Critical, "Target table name required");
            }
        } while (Model.Tables.Contains(dsTable));
    }

    //prompt for table mode i.e. Import, Direct Query or Dual
    string mode;
    do
    {
        mode = Interaction.InputBox(@"Please choose table mode

    Import is the preferred option. For Direct Query datasets, you should only set Fact tables to DirectQuery. Dimension tables should be set to Dual

    Delete as appropriate:", "Table Mode", "Import / DirectQuery / Dual");
        if (mode == "") return;
        var newTable = Model.AddTable(dsTable);
        //assign mode       
        if (mode == "Import")
        {
            newTable.Partitions[dsTable].Mode = ModeType.Import;
        }
        else if (mode == "DirectQuery")
        {
            newTable.Partitions[dsTable].Mode = ModeType.DirectQuery;
        }
        else if (mode == "Dual")
        {
            newTable.Partitions[dsTable].Mode = ModeType.Dual;
        }
        else
        {
            Interaction.MsgBox(@"Table Mode must be one of:

        - Import

        - DirectQuery

        - Dual", MsgBoxStyle.Critical, "Invalid Table Mode");
            Model.Tables[dsTable].Delete();
        }
    } while (!Model.Tables.Contains(dsTable));

    var createdTable = Model.Tables[dsTable];
    createdTable.Partitions[dsTable].Expression = "let DatabricksTable = _fn_GetDataFromDBX(\"" + dbxDatabase + "\", \"" + dbxTable + "\") in DatabricksTable";
    createdTable.SetExtendedProperty("Source", "Databricks", 0);
    createdTable.SetExtendedProperty("SourceDatabase", dbxDatabase, 0);
    createdTable.SetExtendedProperty("SourceTable", dbxTable, 0);

    //add columns to table
    foreach (sysData.DataRow row in dbxColumns.Rows)
    {
        string sourceColumn = row["col_name"].ToString();
        var newColumn = Model.Tables[dsTable].AddDataColumn(sourceColumn);
        newColumn.SourceColumn = sourceColumn;
        newColumn.Description = row["comment"].ToString();
        newColumn.SummarizeBy = AggregateFunction.None;
        newColumn.SetExtendedProperty("SourceColumn", sourceColumn, 0);

    // set data types  
        var dt = row["data_type"].ToString();
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
    continueMsg = Interaction.MsgBox("Do you want to add another table?", MsgBoxStyle.YesNo, "Add another table?");
} while (continueMsg == MsgBoxResult.Yes);

conn.Close();