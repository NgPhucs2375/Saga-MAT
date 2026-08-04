DELETE FROM transport.message_delivery WHERE queue_id IN (SELECT id FROM transport.queue WHERE name = 'order-state');
DELETE FROM transport.message WHERE transport_message_id NOT IN (SELECT DISTINCT transport_message_id FROM transport.message_delivery);
DELETE FROM "OrderState";
SELECT (SELECT count(*) FROM transport.message_delivery d JOIN transport.queue q ON q.id=d.queue_id WHERE q.name='order-state') AS stuck_msgs,
       (SELECT count(*) FROM "OrderState") AS saga_rows;
