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

                /// <summary>
        /// Перевіряє, що GetAllActiveMembers повертає лише активних учасників.
        /// </summary>
        [Fact]
        public void GetAllActiveMembers_ShouldReturnOnlyActiveMembers()
        {
            // Arrange
            var activeMember = new Member { Id = 1, Name = "Active User", IsActive = true };
            var inactiveMember = new Member { Id = 2, Name = "Inactive User", IsActive = false };
            var allMembers = new List<Member> { activeMember, inactiveMember };

            _repo.Setup(r => r.GetAll()).Returns(allMembers);
            var service = new MemberService(_repo.Object);

            // Act
            var result = service.GetAllActiveMembers();

            // Assert
            Assert.NotEmpty(result); 
            Assert.Contains(activeMember, result); 
            Assert.DoesNotContain(inactiveMember, result); 
            Assert.NotEqual(allMembers.Count, result.Count()); 
        }

        /// <summary>
        /// Перевіряє, що GetAllActiveMembers повертає порожню колекцію,
        /// якщо активних учасників немає.
        /// </summary>
        [Fact]
        public void GetAllActiveMembers_ShouldReturnEmpty_WhenNoActiveMembers()
        {
            // Arrange
            var inactiveMember1 = new Member { Id = 1, IsActive = false };
            var inactiveMember2 = new Member { Id = 2, IsActive = false };
            var allMembers = new List<Member> { inactiveMember1, inactiveMember2 };

            _repo.Setup(r => r.GetAll()).Returns(allMembers);
            var service = new MemberService(_repo.Object);

            // Act
            var result = service.GetAllActiveMembers();

            // Assert
            Assert.Empty(result); 
        }
    }
}