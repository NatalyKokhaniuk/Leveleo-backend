-- PostgreSQL: повне очищення замовлень і оплат + відновлення складу (ніби замовлень не було).
-- Перевірте ім'я БД та виконайте вручну (pgAdmin, DBeaver, psql). Краще в транзакції.
--
-- Логіка складу в проєкті:
--   Pending: тримання через InventoryReservations (StockQuantity не зменшується).
--   Після оплати (Processing/Shipped/Completed): ConfirmReservation зменшує Products.StockQuantity.
-- Тому перед видаленням замовлень повертаємо кількість на склад для статусів 1,2,3 (Processing, Shipped, Completed).
-- Статуси enum OrderStatus: Pending=0, Processing=1, Shipped=2, Completed=3, Cancelled=4, PaymentFailed=5.
--
-- Якщо є Cancelled після оплати (рідко), склад могли не повернути в коді — за потреби скоригуйте вручну.

BEGIN;

-- 1) Повернути на склад товар, уже списаний після успішної оплати
UPDATE "Products" p
SET "StockQuantity" = p."StockQuantity" + agg.qty,
    "UpdatedAt" = NOW()
FROM (
    SELECT oi."ProductId", SUM(oi."Quantity")::int AS qty
    FROM "OrderItems" oi
    INNER JOIN "Orders" o ON o."Id" = oi."OrderId"
    WHERE o."Status" IN (1, 2, 3)
    GROUP BY oi."ProductId"
) agg
WHERE p."Id" = agg."ProductId";

-- 2) Резерви під неоплачені замовлення (Pending тощо) — прибираємо повністю
DELETE FROM "InventoryReservations";

-- 3) Завдання адмінки, прив’язані до замовлень (опційно, щоб не лишались «сірий» зв’язок)
DELETE FROM "AdminTasks"
WHERE "RelatedEntityType" = 'Order';

-- 4) Видалити всі замовлення; оплати та рядки залежних таблиць зазвичай зникають каскадом (Payment.OrderId → Order).
--    Якщо в БД є зовнішній ключ Order.PaymentId → Payment, спочатку обнуляємо його.
UPDATE "Orders" SET "PaymentId" = NULL;

DELETE FROM "Orders";

COMMIT;

-- Після виконання: таблиці "Orders", "OrderItems", "Payments", "Deliveries" порожні (якщо інших даних не було).
-- Кошики ("ShoppingCarts") не чіпаються — за потреби очистіть окремо.
