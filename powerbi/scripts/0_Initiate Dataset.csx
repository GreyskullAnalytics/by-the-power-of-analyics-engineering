#r "Microsoft.VisualBasic"
using Microsoft.VisualBasic;

//input box for Dataset name
string strModelName;
do
{
    strModelName = Interaction.InputBox("Please enter the Dataset name", "Dataset Name", Model.Name);
    if (strModelName == "") return;
    if (strModelName == "Model")
    {
        Interaction.MsgBox("Please change the Dataset name from default of 'Model'", MsgBoxStyle.Critical, "Dataset name required");
    }
} while (strModelName == "Model");
//set dataset name
Model.Name = strModelName;

//input box for DbxServer
string strDbxServer;
do
{
    strDbxServer = Interaction.InputBox(@"Please enter the target Databricks Host Server (e.g. adb-12345.6.azuredatabricks.net) 

This should be the server being used for development work", "Databricks Server", "-");
    if (strDbxServer == "") return;
    if (strDbxServer == "-")
    {
        Interaction.MsgBox("Databricks Server is Required", MsgBoxStyle.Critical, "Databricks Server is Required");
    }
} while (strDbxServer == "-");

//add DbxServer parameter
if (!Model.Expressions.Contains("_DbxServer"))
{
    var dbxServer = Model.AddExpression("_DbxServer");
    dbxServer.Kind = ExpressionKind.M;
}
Model.Expressions["_DbxServer"].SetExtendedProperty("DbxServer", strDbxServer, 0);
Model.Expressions["_DbxServer"].Expression = "\"" + strDbxServer + "\" meta [IsParameterQuery=true, Type=\"Text\", IsParameterQueryRequired=true]";

//input box for DbxEndpoint
string strDbxEndpoint;
do
{
    strDbxEndpoint = Interaction.InputBox(@"Please enter the target Databricks SQL Endpoint (e.g. /sql/1.0/warehouses/abcde)
    
This should be the SQL endpoint being used for development work", "Databricks SQL Endpoint", "-");
    if (strDbxServer == "") return;
    if (strDbxEndpoint == "-")
    {
        Interaction.MsgBox("Databricks SQL Endpoint is Required", MsgBoxStyle.Critical, "Databricks SQL Endpoint is Required");

    }
} while (strDbxEndpoint == "-");

//add DbxEndpoint parameter
if (!Model.Expressions.Contains("_DbxEndpoint"))
{
    var dbxEndpoint = Model.AddExpression("_DbxEndpoint");
    dbxEndpoint.Kind = ExpressionKind.M;
}
Model.Expressions["_DbxEndpoint"].Expression = "\"" + strDbxEndpoint + "\" meta [IsParameterQuery=true, Type=\"Text\", IsParameterQueryRequired=true]";
Model.Expressions["_DbxEndpoint"].SetExtendedProperty("DbxEndpoint", strDbxEndpoint, 0);

//input box for DbxCatalog
string strDbxCatalog;
do
{
    strDbxCatalog = Interaction.InputBox(@"Please enter the target Databricks Catalog
    
This should be the catalog being used for development work.

hive_metastore can be used as a default, but for Unity Catalog an environment specific catalog needs to be provided.", "Databricks Catalog", "-");
    if (strDbxCatalog == "") return;
    if (strDbxCatalog == "-")
    {
        Interaction.MsgBox("Databricks Catalog is Required", MsgBoxStyle.Critical, "Databricks Catalog is Required");
    }
} while (strDbxCatalog == "-");

//add DbxCatalog parameter
if (!Model.Expressions.Contains("_DbxCatalog"))
{
    var dbxCatalog = Model.AddExpression("_DbxCatalog");
    dbxCatalog.Kind = ExpressionKind.M;
}
Model.Expressions["_DbxCatalog"].SetExtendedProperty("DbxCatalog", strDbxCatalog, 0);
Model.Expressions["_DbxCatalog"].Expression = "\"" + strDbxCatalog + "\" meta [IsParameterQuery=true, Type=\"Text\", IsParameterQueryRequired=true]";

//add BuildNumber parameter - no input needed, defaults to 0
if (!Model.Expressions.Contains("_BuildNumber"))
{
    var buildNumber = Model.AddExpression("_BuildNumber");
    buildNumber.Kind = ExpressionKind.M;
    buildNumber.Expression = "\"0\" meta [IsParameterQuery=true, Type=\"Text\", IsParameterQueryRequired=true]";
}

//add Enviroment parameter - no input needed, defaults to Development
if (!Model.Expressions.Contains("_Environment"))
{
    var environment = Model.AddExpression("_Environment");
    environment.Kind = ExpressionKind.M;
    environment.Expression = "\"Development\" meta [IsParameterQuery=true, Type=\"Text\", IsParameterQueryRequired=true]";
}

//add GetDataFromDBX function
if (!Model.Expressions.Contains("_fn_GetDataFromDBX"))
{
    var getDataFunction = Model.AddExpression("_fn_GetDataFromDBX");
    getDataFunction.Kind = ExpressionKind.M;
    getDataFunction.Expression = "let \n" +
    "    func = (_DbxDatabase as text, _DbxTable ) => let \n" +
    "        Source = Databricks.Catalogs(_DbxServer, _DbxEndpoint, []),\n" +
    "        catalog_Database = Source{[Name=_DbxCatalog,Kind=\"Database\"]}[Data],\n" +
    "        default_Schema = catalog_Database{[Name=_DbxDatabase,Kind=\"Schema\"]}[Data],\n" +
    "        Table = default_Schema{[Name=_DbxTable,Kind=\"Table\"]}[Data]\n" +
    "    in\n" +
    "        Table\n" +
    "in\n" +
    "    func";
}

//add Dataset Metadata table
if (!Model.Tables.Contains("@Dataset Metadata"))
{
    var newDatasetMetadata = Model.AddTable("@Dataset Metadata");
    var mPartitionDatasetMetadata = Model.Tables["@Dataset Metadata"].AddMPartition("temp");
    Model.Tables["@Dataset Metadata"].Partitions["temp"].Expression = "let\n " +
    "Refresh = Table.TransformColumnTypes(Table.FromList(List.DateTimes(DateTime.From(DateTimeZone.UtcNow()), 1, #duration(0, 0, 0, 0)), Splitter.SplitByNothing(), {\"Last Data Refresh (UTC)\"}, null, ExtraValues.Error), {{\"Last Data Refresh (UTC)\", type datetime}}),\n" +
    "#\"Added Environment\" = Table.AddColumn(Refresh, \"Environment\", each _Environment),\n" +
    "#\"Added BuildNumber\" = Table.AddColumn(#\"Added Environment\", \"Build Number\", each _BuildNumber)\n" +
    "\n in\n" +
    "#\"Added BuildNumber\"";

    //add table description
    Model.Tables["@Dataset Metadata"].Description = "Table holding metadata fields for the Dataset";

    //deletes default partition and associated data sources
    Model.Tables["@Dataset Metadata"].Partitions["@Dataset Metadata"].Delete();
    Model.DataSources["New Provider Data Source"].Delete();

    //rename temp partition to match table name
    Model.Tables["@Dataset Metadata"].Partitions["temp"].Name = "@Dataset Metadata";

    //set import mode
    Model.Tables["@Dataset Metadata"].Partitions["@Dataset Metadata"].Mode = ModeType.Import;

    //add columns
    var dmColumn1 = Model.Tables["@Dataset Metadata"].AddDataColumn("Build Number");
    dmColumn1.SourceColumn = "Build Number";
    dmColumn1.Description = "The Dataset build number from Azure Dev Ops";
    dmColumn1.DataType = DataType.String;

    var dmColumn2 = Model.Tables["@Dataset Metadata"].AddDataColumn("Environment");
    dmColumn2.SourceColumn = "Environment";
    dmColumn2.Description = "The lifecycle development stage for the Dataset";
    dmColumn2.DataType = DataType.String;

    var dmColumn3 = Model.Tables["@Dataset Metadata"].AddDataColumn("Last Data Refresh (UTC)");
    dmColumn3.SourceColumn = "Last Data Refresh (UTC)";
    dmColumn3.Description = "The date and time of the last Dataset refresh in UTC timezone format";
    dmColumn3.DataType = DataType.DateTime;
    dmColumn3.FormatString = "yyyy-mm-dd hh:nn:ss";
}

//add Measures table
if (!Model.Tables.Contains("@Measures"))
{
    var newMeasures = Model.AddTable("@Measures");
    Model.Tables["@Measures"].Partitions["@Measures"].Expression = "let\n " +
    "Source = Table.FromRows(Json.Document(Binary.Decompress(Binary.FromText(\"i44FAA==\", BinaryEncoding.Base64), Compression.Deflate)), let _t = ((type nullable text) meta [Serialized.Text = true]) in type table [Column1 = _t])\n" +
    "\n in\n" +
    "Source";
    Model.Tables["@Measures"].Description = "Table holding the Dataset measures";
}