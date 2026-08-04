SELECT column_name FROM information_schema.columns WHERE table_name='OrderHistories';
SELECT '---OLD-HISTORY---';
SELECT h."EventType", h."Status", left(h."Message",60) AS msg
FROM "OrderHistories" h JOIN "Orders" o ON h."OrderId"=o."OrderId"
WHERE o."OrderCode" IN ('ORD-20260804035927-88e3c9')
ORDER BY h."Id";
SELECT '---OLD-STATE---';
SELECT "CorrelationId"::text AS oid, "CurrentState" FROM "OrderState" ORDER BY "CreatedAt" DESC LIMIT 6;
SELECT '---OLD-TIMERS---';
SELECT t."OrderId"::text, t."Status", t."TimerStatus", t."Timeout" FROM "OrderTimers" t ORDER BY t."CreatedAt" DESC LIMIT 6;
