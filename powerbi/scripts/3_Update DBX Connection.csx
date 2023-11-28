#r "Microsoft.VisualBasic"
using Microsoft.VisualBasic;

//input box for DbxServer
string strDbxServer = Model.Expressions["_DbxServer"].GetExtendedProperty("DbxServer");

strDbxServer = Interaction.InputBox("Please enter the target Databricks Host Server (e.g. adb-12345.6.azuredatabricks.net). This should be the server being used for development work", "Update Databricks Server", strDbxServer);
    if (strDbxServer == "") return;

//update DbxServer parameter
Model.Expressions["_DbxServer"].SetExtendedProperty("DbxServer", strDbxServer, 0);
Model.Expressions["_DbxServer"].Expression = "\"" + strDbxServer + "\" meta [IsParameterQuery=true, Type=\"Text\", IsParameterQueryRequired=true]";

//input box for DbxEndpoint
string strDbxEndpoint = Model.Expressions["_DbxEndpoint"].GetExtendedProperty("DbxEndpoint");

strDbxEndpoint = Interaction.InputBox("Please enter the target Databricks SQL Endpoint (e.g. /sql/1.0/warehouses/abcde). This should be the SQL endpoint being used for development work", "Update Databricks SQL Endpoint", strDbxEndpoint);
    if (strDbxServer == "") return;

//update Endpoint parameter
Model.Expressions["_DbxEndpoint"].Expression = "\"" + strDbxEndpoint + "\" meta [IsParameterQuery=true, Type=\"Text\", IsParameterQueryRequired=true]";
Model.Expressions["_DbxEndpoint"].SetExtendedProperty("DbxEndpoint", strDbxEndpoint, 0);

//input box for DbxCatalog
string strDbxCatalog = Model.Expressions["_DbxCatalog"].GetExtendedProperty("DbxCatalog");

 strDbxCatalog = Interaction.InputBox("Please enter the target Databricks Catalog. This should be the catalog being used for development work. hive_metastore can be used as a default, but for Unity Catalog an environment specific catalog needs to be provided.", "Databricks Catalog", "-");
    if (strDbxCatalog == "") return;

//update Catalog parameter
Model.Expressions["_DbxCatalog"].Expression = "\"" + strDbxCatalog + "\" meta [IsParameterQuery=true, Type=\"Text\", IsParameterQueryRequired=true]";
Model.Expressions["_DbxCatalog"].SetExtendedProperty("DbxCatalog", strDbxCatalog, 0);


