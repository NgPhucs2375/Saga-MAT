SELECT o."Id", o."OrderCode", o."Status", o."IsReserved",
  (SELECT string_agg(h."EventType"||':'||h."Status", ' -> ' ORDER BY h."HistoryId")
     FROM "OrderHistory" h WHERE h."OrderId" = o."OrderId") AS history
FROM "Orders" o ORDER BY o."CreatedAt" DESC LIMIT 6;

SELECT "OrderId"::text AS oid, "CurrentState", "Status" FROM "OrderState" ORDER BY "CreatedAt" DESC LIMIT 6;
