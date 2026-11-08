using Energy_Project.Models;
using Energy_Project.Services;
using Energy_Project.Services.Interfaces;
using Moq;

namespace SmartHomeTests
{
    public class DeviceServiceTests
    {
        private readonly Mock<IDeviceRepository> _deviceRepo = new();

        private DeviceService CreateService() => new(_deviceRepo.Object);

        /// <summary>
        /// Якщо пристрій знайдено і просять увімкнути, метод повертає true.
        /// </summary>
        [Fact]
        public void ToggleDevice_DeviceExists_TurnOn_ReturnsTrue()
        {
            var device = new Device { Id = 1, IsOn = false };
            _deviceRepo.Setup(r => r.GetById(1)).Returns(device);
            var service = CreateService();

            var result = service.ToggleDevice(1, true);

            Assert.True(result);
        }

        /// <summary>
        /// Якщо пристрій знайдено і просять вимкнути, метод повертає false.
        /// </summary>
        [Fact]
        public void ToggleDevice_DeviceExists_TurnOff_ReturnsFalse()
        {
            var device = new Device { Id = 1, IsOn = true };
            _deviceRepo.Setup(r => r.GetById(1)).Returns(device);
            var service = CreateService();

            var result = service.ToggleDevice(1, false);

            Assert.False(result);
        }

        /// <summary>
        /// Якщо пристрою немає, метод Update репозиторію не викликається.
        /// </summary>
        [Fact]
        public void ToggleDevice_DeviceNotFound_NeverCallsUpdate()
        {
            _deviceRepo.Setup(r => r.GetById(It.IsAny<int>())).Returns((Device?)null);
            var service = CreateService();

            try
            {
                service.ToggleDevice(1, true);
            }
            catch (ArgumentException)
            {
            }

            _deviceRepo.Verify(r => r.Update(It.IsAny<Device>()), Times.Never);
        }

        /// <summary>
        /// Якщо пристрій знайдено, метод Update репозиторію викликається один раз.
        /// </summary>
        [Fact]
        public void ToggleDevice_DeviceFound_CallsUpdate()
        {
            var device = new Device { Id = 1, IsOn = false };
            _deviceRepo.Setup(r => r.GetById(1)).Returns(device);
            var service = CreateService();

            service.ToggleDevice(1, true);

            _deviceRepo.Verify(r => r.Update(It.Is<Device>(d => d.Id == 1)), Times.Once);
        }

        /// <summary>
        /// Якщо пристрою немає, метод кидає ArgumentException.
        /// </summary>
        [Fact]
        public void ToggleDevice_DeviceNotFound_ThrowsArgumentException()
        {
            const int MISSING_ID = 99;
            _deviceRepo.Setup(r => r.GetById(MISSING_ID)).Returns((Device?)null);
            var service = CreateService();

            Action actual = () => service.ToggleDevice(MISSING_ID, true);

            Assert.Throws<ArgumentException>(actual);
        }

        /// <summary>
        /// Серед увімкнених і вимкнених пристроїв результат містить увімкнений.
        /// </summary>
        [Fact]
        public void GetActiveDevices_MixedDevices_ContainsActiveDevice()
        {
            var device1 = new Device { Id = 1, IsOn = true };
            var device2 = new Device { Id = 2, IsOn = false };
            _deviceRepo.Setup(r => r.GetAll()).Returns(new List<Device> { device1, device2 });
            var service = CreateService();

            var actual = service.GetActiveDevices().ToList();

            Assert.Contains(device1, actual);
        }

        /// <summary>
        /// Якщо всі пристрої вимкнені, список активних пристроїв порожній.
        /// </summary>
        [Fact]
        public void GetActiveDevices_AllDevicesOff_ReturnsEmpty()
        {
            var device1 = new Device { Id = 1, IsOn = false };
            var device2 = new Device { Id = 2, IsOn = false };
            _deviceRepo.Setup(r => r.GetAll()).Returns(new List<Device> { device1, device2 });
            var service = CreateService();

            var actual = service.GetActiveDevices().ToList();

            Assert.Empty(actual);
        }
    }
}