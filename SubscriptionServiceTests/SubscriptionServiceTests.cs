using Moq;
using Subscription_Service.Models;
using Subscription_Service.Services;
using Subscription_Service.Services.Interfaces;
using Xunit;

namespace SubscriptionServiceTests
{
    public class SubscriptionServiceTests
    {
        private readonly Mock<IMemberRepository> _repo = new();
        private readonly Mock<IPaymentService> _payment = new();
        private readonly Mock<INotificationService> _notify = new();

        /// <summary>
        /// Перевіряє успішне поновлення підписки,
        /// якщо учасник існує і платіж валідний.
        /// </summary>
        [Fact]
        public void RenewSubscription_ShouldReturnTrue_WhenPaymentIsVerified()
        {
            // Arrange
            var member = new Member { Id = 1, IsActive = false };
            
            // Налаштовуємо моки
            _repo.Setup(r => r.GetById(1)).Returns(member);
            _payment.Setup(p => p.VerifyPayment(1, 100)).Returns(true);
            
            var service = new SubscriptionService(_repo.Object, _payment.Object, _notify.Object);

            // Act
            var result = service.RenewSubscription(1, 100, 30);

            // Assert
            Assert.True(result); // Ми вже використовували, але це логічна перевірка
            Assert.True(member.IsActive); // Перевіряємо, що статус змінився
            Assert.NotNull(member.SubscriptionEnd); // Перевіряємо, що дата оновилась

            // Перевіряємо, що ключові методи були викликані
            _repo.Verify(r => r.Update(member), Times.Once()); 
            _notify.Verify(n => n.SendNotification(It.IsAny<string>(), 1), Times.Once()); 
        }

        /// <summary>
        /// Перевіряє, що метод повертає false,
        /// якщо платіж не пройшов верифікацію.
        /// </summary>
        [Fact]
        public void RenewSubscription_ShouldReturnFalse_WhenPaymentFails()
        {
            // Arrange
            var member = new Member { Id = 1, IsActive = false };

            _repo.Setup(r => r.GetById(1)).Returns(member);
            _payment.Setup(p => p.VerifyPayment(1, 100)).Returns(false); // Імітуємо неуспішний платіж
            
            var service = new SubscriptionService(_repo.Object, _payment.Object, _notify.Object);

            // Act
            var result = service.RenewSubscription(1, 100, 30);

            // Assert
            Assert.False(result); // Перевіряємо, що метод повернув false

            // Перевіряємо, що дані НЕ були оновлені і сповіщення НЕ було надіслано
            _repo.Verify(r => r.Update(It.IsAny<Member>()), Times.Never()); 
            _notify.Verify(n => n.SendNotification(It.IsAny<string>(), It.IsAny<int>()), Times.Never()); 
        }

        /// <summary>
        /// Перевіряє, що метод кидає виняток ArgumentException,
        /// якщо учасник з вказаним ID не знайдений.
        /// </summary>
        [Fact]
        public void RenewSubscription_ShouldThrowException_WhenMemberNotFound()
        {
            // Arrange
            _repo.Setup(r => r.GetById(99)).Returns((Member?)null); // Імітуємо, що учасник не знайдений
            
            var service = new SubscriptionService(_repo.Object, _payment.Object, _notify.Object);

            // Act & Assert
            // Ми перевіряємо, що при виклику сервісу буде кинуто саме ArgumentException
            Assert.Throws<ArgumentException>(() => service.RenewSubscription(99, 100, 30)); 
        }
    }
}