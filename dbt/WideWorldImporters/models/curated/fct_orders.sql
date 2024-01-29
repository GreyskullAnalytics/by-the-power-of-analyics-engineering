{{ config(tags=["fact"]) }}

WITH
orderLines AS (SELECT * FROM {{ source('wideworldimporters', 'Sales_OrderLines')}}),

orders AS (SELECT * FROM {{ source('wideworldimporters', 'Sales_Orders')}}),

dim_customer AS (SELECT * FROM {{ ref('dim_customer')}}),

dim_product AS (SELECT * FROM {{ ref('dim_product')}}),

final AS (
SELECT
    dc.CustomerKey,
    dp.ProductKey,
    {{ smart_date_key('o.OrderDate') }} AS DateKey,
    ol.Quantity AS Quantity,
    ol.UnitPrice * ol.Quantity AS SalesAmount
FROM
    orderLines ol
    LEFT OUTER JOIN orders o
        ON ol.OrderID = o.OrderID
    LEFT OUTER JOIN dim_customer dc
        ON o.CustomerID = dc.CustomerID
    LEFT OUTER JOIN dim_product dp
        ON ol.StockItemID = ol.StockItemID
)

SELECT * FROM final

