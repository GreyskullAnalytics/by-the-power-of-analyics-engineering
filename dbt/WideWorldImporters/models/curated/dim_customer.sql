{{ config(tags=["dimension"]) }}

WITH
customers AS ( SELECT * FROM {{ source('wideworldimporters', 'Sales_Customers')}}),

customerCategories AS ( SELECT * FROM {{ source('wideworldimporters', 'Sales_CustomerCategories')}}),

buyingGroups AS ( SELECT * FROM {{ source('wideworldimporters', 'Sales_BuyingGroups')}}),

final AS (
SELECT 
    ROW_NUMBER() OVER (ORDER BY c.CustomerID) AS CustomerKey,
    c.CustomerID,
    c.CustomerName,
    c.WebsiteURL,
    cc.CustomerCategoryName,
    bg.BuyingGroupName
FROM
    customers c
    LEFT OUTER JOIN customerCategories cc
        ON c.CustomerCategoryID = cc.CustomerCategoryID
    LEFT OUTER JOIN buyingGroups bg
        ON c.BuyingGroupID = bg.BuyingGroupID
)

SELECT * FROM final