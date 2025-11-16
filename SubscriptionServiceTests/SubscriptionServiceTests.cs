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

        /// <summary>
        /// Перевіряє, що учасники з простроченою підпискою
        /// деактивовані та сповіщені.
        /// </summary>
        [Fact]
        public void DeactivateExpiredMembers_ShouldDeactivateAndUpdate_WhenSubscriptionIsExpired()
        {
            // Arrange
            var expiredMember = new Member 
            { 
                Id = 1, IsActive = true, SubscriptionEnd = DateTime.Now.AddDays(-1) // Підписка закінчилась вчора
            };
            var activeMember = new Member 
            { 
                Id = 2, IsActive = true, SubscriptionEnd = DateTime.Now.AddDays(30) // Ще активна
            };
            var allMembers = new List<Member> { expiredMember, activeMember };

            _repo.Setup(r => r.GetAll()).Returns(allMembers);
            
            var service = new SubscriptionService(_repo.Object, _payment.Object, _notify.Object);

            // Act
            service.DeactivateExpiredMembers();

            // Assert
            Assert.False(expiredMember.IsActive); // Переконуємось, що статус змінився
            Assert.True(activeMember.IsActive);  // Переконуємось, що статус не змінився

            // Використовуємо It.Is(predicate) для перевірки, що Update викликали саме для деактивованого учасника
            _repo.Verify(r => r.Update(
                It.Is<Member>(m => m.Id == expiredMember.Id && m.IsActive == false)), 
                Times.Once());
            
            // Перевіряємо, що сповіщення було надіслано
            _notify.Verify(n => n.SendNotification("Membership expired", expiredMember.Id), Times.Once());
            
            // Перевіряємо, що "активного" учасника не оновлювали і не сповіщали
            _repo.Verify(r => r.Update(activeMember), Times.Never());
            _notify.Verify(n => n.SendNotification(It.IsAny<string>(), activeMember.Id), Times.Never());
        }

        /// <summary>
        /// Перевіряє, що нічого не відбувається,
        /// якщо немає учасників з простроченою підпискою.
        /// </summary>
        [Fact]
        public void DeactivateExpiredMembers_ShouldDoNothing_WhenNoMembersAreExpired()
        {
            // Arrange
            var activeMember = new Member { Id = 1, IsActive = true, SubscriptionEnd = DateTime.Now.AddDays(30) };
            var allMembers = new List<Member> { activeMember };

            _repo.Setup(r => r.GetAll()).Returns(allMembers);
            
            var service = new SubscriptionService(_repo.Object, _payment.Object, _notify.Object);

            // Act
            service.DeactivateExpiredMembers();

            // Assert
            // Перевіряємо, що жодних оновлень чи сповіщень не було
            _repo.Verify(r => r.Update(It.IsAny<Member>()), Times.Never());
            _notify.Verify(n => n.SendNotification(It.IsAny<string>(), It.IsAny<int>()), Times.Never());
        }

        /// <summary>
        /// Перевіряє, що деактивовано ВСІХ учасників з простроченою підпискою,
        /// якщо їх декілька.
        /// </summary>
        [Fact]
        public void DeactivateExpiredMembers_ShouldDeactivateAllExpiredMembers_WhenMultipleExist()
        {
            // Arrange
            var expiredMember1 = new Member { Id = 1, IsActive = true, SubscriptionEnd = DateTime.Now.AddDays(-1) };
            var expiredMember2 = new Member { Id = 2, IsActive = true, SubscriptionEnd = DateTime.Now.AddDays(-5) };
            var activeMember = new Member { Id = 3, IsActive = true, SubscriptionEnd = DateTime.Now.AddDays(30) };
            
            var allMembers = new List<Member> { expiredMember1, expiredMember2, activeMember };

            _repo.Setup(r => r.GetAll()).Returns(allMembers);
            
            var service = new SubscriptionService(_repo.Object, _payment.Object, _notify.Object);

            // Act
            service.DeactivateExpiredMembers();

            // Assert
            // Перевіряємо, що обидва прострочені учасники деактивовані
            Assert.False(expiredMember1.IsActive);
            Assert.False(expiredMember2.IsActive);
            // ...а активний учасник залишився активним
            Assert.True(activeMember.IsActive);

            // Перевіряємо, що Update був викликаний рівно 2 рази
            _repo.Verify(r => r.Update(It.Is<Member>(m => m.IsActive == false)), Times.Exactly(2));

            // Перевіряємо, що сповіщення було надіслано рівно 2 рази
            _notify.Verify(n => n.SendNotification("Membership expired", It.IsAny<int>()), Times.Exactly(2));
        }
    }

}