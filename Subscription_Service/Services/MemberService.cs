using Subscription_Service.Models;
using Subscription_Service.Services.Interfaces;

namespace Subscription_Service.Services
{
    public class MemberService
    {
        private readonly IMemberRepository _repo;

        public MemberService(IMemberRepository repo)
        {
            _repo = repo;
        }

        public Member? GetMember(int id) => _repo.GetById(id);

        public bool IsActive(int id)
        {
            var member = _repo.GetById(id);
            return member?.IsActive ?? false;
        }

        public IEnumerable<Member> GetAllActiveMembers()
        {
            return _repo.GetAll().Where(m => m.IsActive);
        }

    }
}