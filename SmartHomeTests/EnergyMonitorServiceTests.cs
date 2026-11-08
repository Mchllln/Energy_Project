using Energy_Project.Models;
using Energy_Project.Services;
using Energy_Project.Services.Interfaces;
using Moq;

namespace SmartHomeTests
{
    public class EnergyMonitorServiceTests
    {
        private readonly Mock<IDeviceRepository> _deviceRepo = new();
        private readonly Mock<IEnergyPlanRepository> _planRepo = new();
        private readonly Mock<INotificationService> _notify = new();

        private EnergyMonitorService CreateService() =>
            new(_deviceRepo.Object, _planRepo.Object, _notify.Object);

        /// <summary>
        /// Якщо всі пристрої вимкнені, поточне споживання дорівнює нулю.
        /// </summary>
        [Fact]
        public void CalculateCurrentUsageKwh_AllDevicesOff_ReturnsZero()
        {
            var device1 = new Device { PowerUsageWatts = 1000, IsOn = false };
            var device2 = new Device { PowerUsageWatts = 500, IsOn = false };
            _deviceRepo.Setup(r => r.GetAll()).Returns(new List<Device> { device1, device2 });
            var service = CreateService();

            var actual = service.CalculateCurrentUsageKwh();

            Assert.Equal(0.0, actual);
        }

        /// <summary>
        /// Споживання рахується лише з увімкнених пристроїв і переводиться з Вт у кВт.
        /// </summary>
        [Theory]
        [InlineData(1000.0, 500.0, 1.5)]
        [InlineData(2000.0, 3000.0, 5.0)]
        [InlineData(0.0, 0.0, 0.0)]
        public void CalculateCurrentUsageKwh_ActiveDevices_ReturnsSumInKwh(double watts1, double watts2, double expected)
        {
            var device1 = new Device { PowerUsageWatts = watts1, IsOn = true };
            var device2 = new Device { PowerUsageWatts = watts2, IsOn = true };
            var device3 = new Device { PowerUsageWatts = 9999, IsOn = false };
            _deviceRepo.Setup(r => r.GetAll()).Returns(new List<Device> { device1, device2, device3 });
            var service = CreateService();

            var actual = service.CalculateCurrentUsageKwh();

            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Якщо споживання більше за денний ліміт, сповіщення про перевантаження надсилається рівно один раз.
        /// </summary>
        [Fact]
        public void CheckForOverload_UsageAboveLimit_SendsAlertOnce()
        {
            var device1 = new Device { PowerUsageWatts = 3000, IsOn = true };
            _deviceRepo.Setup(r => r.GetAll()).Returns(new List<Device> { device1 });
            _planRepo.Setup(r => r.GetCurrentPlan()).Returns(new EnergyPlan { DailyLimitKwh = 2 });
            var service = CreateService();

            service.CheckForOverload();

            _notify.Verify(n => n.SendAlert(It.IsAny<string>()), Times.Exactly(1));
        }

        /// <summary>
        /// Якщо споживання дорівнює ліміту, сповіщення не надсилається, бо перевантаженням вважається лише перевищення.
        /// </summary>
        [Fact]
        public void CheckForOverload_UsageEqualsLimit_DoesNotSendAlert()
        {
            var device1 = new Device { PowerUsageWatts = 2000, IsOn = true };
            _deviceRepo.Setup(r => r.GetAll()).Returns(new List<Device> { device1 });
            _planRepo.Setup(r => r.GetCurrentPlan()).Returns(new EnergyPlan { DailyLimitKwh = 2 });
            var service = CreateService();

            service.CheckForOverload();

            _notify.Verify(n => n.SendAlert(It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Якщо споживання менше за ліміт, сповіщення не надсилається.
        /// </summary>
        [Fact]
        public void CheckForOverload_UsageBelowLimit_DoesNotSendAlert()
        {
            var device1 = new Device { PowerUsageWatts = 1000, IsOn = true };
            _deviceRepo.Setup(r => r.GetAll()).Returns(new List<Device> { device1 });
            _planRepo.Setup(r => r.GetCurrentPlan()).Returns(new EnergyPlan { DailyLimitKwh = 5 });
            var service = CreateService();

            service.CheckForOverload();

            _notify.Verify(n => n.SendAlert(It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Після виклику UpdateEnergyLimit репозиторій зберігає план саме з новим лімітом.
        /// </summary>
        [Fact]
        public void UpdateEnergyLimit_NewLimit_UpdatesPlanWithNewLimit()
        {
            const double NEW_LIMIT = 10;
            var plan = new EnergyPlan { DailyLimitKwh = 5 };
            _planRepo.Setup(p => p.GetCurrentPlan()).Returns(plan);
            var service = CreateService();

            service.UpdateEnergyLimit(NEW_LIMIT);

            _planRepo.Verify(p => p.UpdatePlan(It.Is<EnergyPlan>(x => x.DailyLimitKwh == NEW_LIMIT)), Times.Once);
        }

        /// <summary>
        /// Після виклику UpdateEnergyLimit значення ліміту в плані відрізняється від старого.
        /// </summary>
        [Fact]
        public void UpdateEnergyLimit_NewLimit_ChangesLimitValue()
        {
            const double NEW_LIMIT = 10;
            const double OLD_LIMIT = 5;
            var plan = new EnergyPlan { DailyLimitKwh = OLD_LIMIT };
            _planRepo.Setup(p => p.GetCurrentPlan()).Returns(plan);
            var service = CreateService();

            service.UpdateEnergyLimit(NEW_LIMIT);

            Assert.NotEqual(OLD_LIMIT, plan.DailyLimitKwh);
        }
    }
}