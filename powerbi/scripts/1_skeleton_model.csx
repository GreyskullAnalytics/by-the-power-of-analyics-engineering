class p {

public static void ConvertCase(dynamic obj)
{
    var oldName = obj.Name;
    var newName = new System.Text.StringBuilder();
    for(int i = 0; i < oldName.Length; i++) {
        // First letter should always be capitalized:
        if(i == 0) newName.Append(Char.ToUpper(oldName[i]));

        // A sequence of two uppercase letters followed by a lowercase letter should have a space inserted
        // after the first letter:
        else if(i + 2 < oldName.Length && char.IsLower(oldName[i + 2]) && char.IsUpper(oldName[i + 1]) && char.IsUpper(oldName[i]))
        {
            newName.Append(oldName[i]);
            newName.Append(" ");
        }

        // All other sequences of a lowercase letter followed by an uppercase letter, should have a space
        // inserted after the first letter:
        else if(i + 1 < oldName.Length && char.IsLower(oldName[i]) && char.IsUpper(oldName[i+1]))
        {
            newName.Append(oldName[i]);
            newName.Append(" ");
        }
        else
        {
            newName.Append(oldName[i]);
        }
    }
    obj.Name = newName.ToString();
}
}

//add Measures table
//if (!Model.Tables.Contains("@Measures"))
//{
//    var newMeasures = Model.AddTable("@Measures");
//    Model.Tables["@Measures"].Partitions["@Measures"].Expression = "let\n " +
//    "Source = Table.FromRows(Json.Document(Binary.Decompress(Binary.FromText(\"i44FAA==\", BinaryEncoding.Base64), Compression.Deflate)), let _t = ((type nullable text) meta [Serialized.Text = true]) in type table [Column1 = _t])\n" +
//    "\n in\n" +
//    "Source";
//    Model.Tables["@Measures"].Description = "Table holding the Dataset measures";
//}


foreach(var t in Model.Tables) {
//rename tables
    t.SetExtendedProperty("SourceTable", t.Name, 0);
    t.Name = t.Name.Replace("fct_", "").ToString();
    t.Name = t.Name.Replace("dim_", "").ToString();
    //convert colummns
    p.ConvertCase(t);
//
foreach(var c in t.Columns) {
    p.ConvertCase(c);
    c.SummarizeBy = AggregateFunction.None;
    if (c.DataType == DataType.DateTime)
    {c.FormatString = "yyyy-mm-dd";}
    }
}

//create relationships
foreach(var t in Model.Tables) {
    var keySuffix = " Key";
    string sourceTable = t.GetExtendedProperty("SourceTable");
        if ( sourceTable.Substring(0, 3) == "fct")
    {
    foreach(var factColumn in t.Columns.Where(c => c.Name.EndsWith(keySuffix)))
        {
        var dim = Model.Tables.FirstOrDefault(tbl => factColumn.Name.EndsWith(tbl.Name + keySuffix));
        if(dim != null)
        {
            // Find the key column on the dimension table:
            var dimColumn = dim.Columns.FirstOrDefault(c => factColumn.Name.EndsWith(c.Name));
            if(dimColumn != null)
            {
                // Check whether a relationship already exists between the two columns:
                if(!Model.Relationships.Any(r => r.FromColumn == factColumn && r.ToColumn == dimColumn))
                {
                    // If relationships already exists between the two tables, new relationships will be created as inactive:
                    var makeInactive = Model.Relationships.Any(r => r.FromTable == t && r.ToTable == dim);

                    // Add the new relationship:
                    var rel = Model.AddRelationship();
                    rel.FromColumn = factColumn;
                    rel.ToColumn = dimColumn;
                    factColumn.IsHidden = true;
                    factColumn.IsAvailableInMDX = false;
                    dimColumn.IsHidden = true;
                    dimColumn.IsAvailableInMDX = false;
                    dimColumn.IsKey = true;
                    if(makeInactive) rel.IsActive = false;
                }
            }
          
        }
        
        } 
    foreach(var c in t.Columns)
        {
          if(c.IsHidden == false)
            {
            c.IsHidden = true;
            c.IsAvailableInMDX = false;
            var dt = c.DataType;
            var measurePrefix = "# ";

            if (dt == DataType.Double || dt == DataType.Decimal)
                    { measurePrefix = "£ ";
                    c.DataType = DataType.Decimal; } 
            var measureName = measurePrefix + c.Name;
            var newMeasure = Model.Tables[t.Name].AddMeasure(measureName);
            //newMeasure.DisplayFolder = t.Name;
            newMeasure.Expression = "SUM(" + c.DaxObjectFullName + ")";
            newMeasure.Description = "Total " + c.Name;
                if ( measurePrefix =="£ ")
                    {
                    newMeasure.FormatString = "$ #,##0";
                }
                else { newMeasure.FormatString = "#,##0";} 
            }
            
           }
    }

}
Model.Tables["Date"].DataCategory = "Time";
Model.Tables["Date"].Columns["Date"].IsKey = true;