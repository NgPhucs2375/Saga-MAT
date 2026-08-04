using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Domain.Enums;
using Onion.CleanArchitecture.Domain.Events;

namespace OrderCompleteService
{
    /// <summary>
    /// Consumer xử lý CompleteOrderCommand từ Saga để hoàn tất đơn & trừ kho
    /// </summary>
    public class OrderCompleteConsumer : IConsumer<CompleteOrderCommand>
    {
        // === Tiêm các Repository cần thiết === //
        private readonly IProductRepositoryAsync _productRepository;
        private readonly IOrderHistoryRepositoryAsync _orderHistoryRepository;
        private readonly ILogger<OrderCompleteConsumer> _logger;
        private readonly IOrderRepositoryAsync _orderRepository;
        private readonly IOrderTimerRepositoryAsync _orderTimerRepository;
        private readonly IConfiguration _configuration;


        // === Constructor === //
        public OrderCompleteConsumer(
            IProductRepositoryAsync productRepository,
            IOrderHistoryRepositoryAsync orderHistoryRepository,
            ILogger<OrderCompleteConsumer> logger,
            IOrderRepositoryAsync orderRepository,
            IOrderTimerRepositoryAsync orderTimerRepository,
            IConfiguration configuration)
        {
            _productRepository = productRepository;
            _orderHistoryRepository = orderHistoryRepository;
            _logger = logger;
            _orderRepository = orderRepository;
            _orderTimerRepository = orderTimerRepository;
            _configuration = configuration;
        }

        // === Hàm Tiêu thụ CompleteOrderCommand === //
        public async Task Consume(ConsumeContext<CompleteOrderCommand> context)
        {
            // message = CompleteOrderCommand
            var message = context.Message;
            var order = await _orderRepository.GetByIdAsync(message.OrderId);
            // Log cho ra string Nhận được command với ID
            _logger.LogInformation("Nhận CompleteOrderCommand OrderId={OrderId}", message.OrderId);

            // check chống trùng (idempotency) - nếu đã xử lý rồi (retry/duplicate)
            if (order.Status != OrderStatus.Accepted)
            {
                _logger.LogError("OrderId={OrderId} không ở trạng thái Accepted.", message.OrderId);
                return;
            }

            // === Re-validate và chuẩn bị các entity để update === //
            var errors = new List<string>();
            var productsToUpdate = new Dictionary<Guid, Product>();
            foreach (var item in message.Items) // Lặp qua all Items trong CompleteOrderCommand
            {
                var product = await _productRepository.GetProductByIdAsync(item.ProductId);
                if (product == null || !product.IsActive)
                {
                    errors.Add($"Sản phẩm {item.ProductId} không tồn tại hoặc đã bị vô hiệu hóa.");
                }
                else if (product.SLTKho < item.Quantity)
                {
                    errors.Add($"Sản phẩm \"{product.Name}\" chỉ còn {product.SLTKho} trong kho, yêu cầu {item.Quantity}.");
                }
                else
                {
                    // Nếu hợp lệ, thêm vào Dictionary để trừ kho sau
                    if (!productsToUpdate.ContainsKey(product.ProductId))
                    {
                        productsToUpdate.Add(product.ProductId, product);
                    }
                }
            }

            // Biến check xem có lỗi hay không
            var isSuccess = errors.Count == 0;

            if (!isSuccess)
            {
                var errorReason = string.Join("; ", errors);

                await RecordHistoryAsync(message.OrderId, HistoryStatus.Failed, "CompleteOrderCommand", string.Join("; ", errors));
                await context.Publish(new OrderCompleteFailedEvent(
                    NewId.NextGuid(), message.OrderId, message.CustomerId, errorReason, DateTime.UtcNow));
                _logger.LogWarning("Complete thất bại OrderId={OrderId}: {Errors}", message.OrderId, string.Join("; ", errors));
                return;
            }

            try
            {
                // === XỬ LÝ KHI THÀNH CÔNG (TRONG CÙNG 1 TRANSACTION) ===

                // 1. Trừ tồn kho (trên các entity đã được EF Core theo dõi)
                foreach (var item in message.Items)
                {
                    if (productsToUpdate.TryGetValue(item.ProductId, out var product))
                    {
                        product.SLTKho -= item.Quantity;
                    }
                }

                // 2. Cập nhật trạng thái đơn hàng -> Completed
                order.Status = OrderStatus.Completed;
                order.CompletedAt = DateTime.UtcNow;

                // 3. Vô hiệu hóa timer
                var pendingTimer = await _orderTimerRepository.GetPendingByOrderIdAsync(message.OrderId);
                if (pendingTimer != null)
                {
                    pendingTimer.TimerStatus = TimerStatus.Cancelled;
                }

                // 4. Lưu tất cả thay đổi vào DB trong CÙNG 1 LẦN
                // UpdateAsync sẽ tự động set LastModifiedAt (tức UpdatedAt) do cấu hình AuditableBaseEntity
                await _orderRepository.UpdateAsync(order);

                // 5. Ghi lịch sử và publish event cho Saga sau khi DB đã được cập nhật thành công
                await RecordHistoryAsync(message.OrderId, HistoryStatus.Success, "CompleteOrderCommand", "Đơn hàng hoàn tất, đã trừ kho và vô hiệu hóa timer.");

                await context.Publish(new OrderCompletedEvent(
                    NewId.NextGuid(), message.OrderId, message.CustomerId, DateTime.UtcNow));
                _logger.LogInformation("Complete thành công OrderId={OrderId}. Đã trừ kho và vô hiệu hóa timer.", message.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không mong muốn khi hoàn tất OrderId={OrderId}. Kích hoạt bồi hoàn.", message.OrderId);
                var errorReason = $"Lỗi hệ thống khi hoàn tất: {ex.Message}";
                await RecordHistoryAsync(message.OrderId, HistoryStatus.Failed, "CompleteOrderCommand", errorReason);
                await context.Publish(new OrderCompleteFailedEvent(
                    NewId.NextGuid(), message.OrderId, message.CustomerId, errorReason, DateTime.UtcNow));
            }
        }

        // === Hàm ghi OrderHistory === //
        private async Task RecordHistoryAsync(Guid orderId, HistoryStatus status, string eventType, string message)
        {
            await _orderHistoryRepository.AddAsync(new OrderHistory
            {
                HistoryId = NewId.NextGuid(),
                OrderId = orderId,
                ConsumerName = "OrderCompleteConsumer",
                Status = status,
                EventType = eventType,
                Message = message
            });
        }
    }
}
