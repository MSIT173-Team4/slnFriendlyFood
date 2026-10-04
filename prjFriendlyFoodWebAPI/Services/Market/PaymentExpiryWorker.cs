using Microsoft.Extensions.Options;

namespace prjFriendlyFoodWebAPI.Services.Market
{
    // 背景排程：每分鐘取消一次逾期未付款的批次（由 Market:EnableExpiryWorker 決定是否啟用）
    public class PaymentExpiryWorker : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PaymentExpiryWorker> _logger;

        public PaymentExpiryWorker(IServiceScopeFactory scopeFactory, ILogger<PaymentExpiryWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("逾期訂單背景排程已啟動，每 {Minutes} 分鐘執行一次", Interval.TotalMinutes);

            using var timer = new PeriodicTimer(Interval);
            do
            {
                try
                {
                    // BackgroundService 整個程式只有一個實例（Singleton），
                    // DbContext 卻是每個請求一個（Scoped），所以每次執行都要自己建立一個 scope
                    using var scope = _scopeFactory.CreateScope();
                    var cancellation = scope.ServiceProvider.GetRequiredService<IOrderCancellationService>();
                    await cancellation.CancelExpiredBatchesAsync();
                }
                catch (Exception ex)
                {
                    // 這次失敗只記錄，下一輪照常執行，不讓整個排程停掉
                    _logger.LogError(ex, "逾期訂單背景排程執行失敗");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}