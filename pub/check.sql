SELECT "ProductId", "Name", "SLTKho", "IsActive" FROM "Product" ORDER BY "ProductId";
SELECT "Id"::text AS orderid, "OrderCode", "Status", "IsReserved", "TotalAmount" FROM "Orders" ORDER BY "CreatedAt" DESC LIMIT 5;
