WITH 
stockItems AS ( SELECT * FROM {{ source('wideworldimporters', 'Warehouse_StockItems')}}),

colors AS ( SELECT * FROM {{ source('wideworldimporters', 'Warehouse_Colors')}}),

suppliers AS ( SELECT * FROM {{ source('wideworldimporters', 'Purchasing_Suppliers')}}),

supplierCategories AS ( SELECT * FROM {{ source('wideworldimporters', 'Purchasing_SupplierCategories')}}),

final AS (
SELECT 
    si.StockItemID,
    si.StockItemName,
    si.Brand,
    c.ColorName,
    s.SupplierName,
    s.SupplierReference,
    sc.SupplierCategoryName
FROM 
    stockItems si
    LEFT OUTER JOIN colors c
        ON si.ColorID = c.ColorID
    LEFT OUTER JOIN suppliers s
        ON si.SupplierID = s.SupplierID 
    LEFT OUTER JOIN supplierCategories sc
        ON s.SupplierCategoryID = sc.SupplierCategoryID
)

SELECT * FROM final