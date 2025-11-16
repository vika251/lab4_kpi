using Moq;
using Subscription_Service.Models;
using Subscription_Service.Services;
using Subscription_Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SubscriptionServiceTests
{
    public class MemberServiceTests
    {
        private readonly Mock<IMemberRepository> _repo = new();
        private readonly Mock<IPaymentService> _payment = new();
        private readonly Mock<INotificationService> _notify = new(); 

        /// <summary>
        /// Перевіряє метод IsActive з різними вхідними даними.
        /// Має повертати 'expectedResult' (true/false) залежно від статусу 'isActive'.
        /// </summary>
        [Theory] 
        [InlineData(1, true, true)]  // Сценарій 1: Учасник 1 активний, очікуємо true
        [InlineData(2, false, false)] // Сценарій 2: Учасник 2 неактивний, очікуємо false
        [InlineData(99, null, false)] // Сценарій 3: Учасник 99 не існує, очікуємо false
        public void IsActive_ShouldReturnCorrectStatus_BasedOnMemberState(int memberId, bool? isActive, bool expectedResult)
        {
            // Arrange
            Member? member = isActive.HasValue ? new Member { Id = memberId, IsActive = isActive.Value } : null;

            _repo.Setup(r => r.GetById(memberId)).Returns(member);
            
            var service = new MemberService(_repo.Object);

            // Act
            var actualResult = service.IsActive(memberId);

            // Assert
            Assert.Equal(expectedResult, actualResult);
        }

        /// <summary>
        /// Перевіряє, що GetMember повертає правильного учасника,
        /// якщо він існує.
        /// </summary>
        [Fact]
        public void GetMember_ShouldReturnMember_WhenMemberExists()
        {
            // Arrange
            var expectedMember = new Member { Id = 1, Name = "Test User" };
            _repo.Setup(r => r.GetById(1)).Returns(expectedMember);

            var service = new MemberService(_repo.Object);

            // Act
            var actualMember = service.GetMember(1);

            // Assert
            Assert.NotNull(actualMember);  
            Assert.Equal(expectedMember.Id, actualMember.Id); 
            Assert.Equal(expectedMember.Name, actualMember.Name);
        }

        /// <summary>
        /// Перевіряє, що GetMember повертає null,
        /// якщо учасник не знайдений.
        /// </summary>
        [Fact]
        public void GetMember_ShouldReturnNull_WhenMemberDoesNotExist()
        {
            // Arrange
            _repo.Setup(r => r.GetById(99)).Returns((Member?)null);

            var service = new MemberService(_repo.Object);

            // Act
            var result = service.GetMember(99);

            // Assert
            Assert.Null(result);
        }
    }
}